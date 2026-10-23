
import {
  Component,
  inject,
  OnInit,
  signal
} from '@angular/core';

import {
  DatePipe,
  DecimalPipe
} from '@angular/common';

import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

import {
  HttpClient,
  HttpErrorResponse
} from '@angular/common/http';

import {
  ReportFilter,
  WorkReport,
  WorkLogStatus
} from '../../models/report';

import {
  ReportService
} from '../../services/report.service';

import {
  environment
} from '../../../environments/environment';

interface EmployeeOption {
  id: number;
  employeeCode: string;
  fullName: string;
  isActive: boolean;
}

interface ProjectOption {
  id: number;
  projectCode: string;
  projectName: string;
  isActive: boolean;
}

@Component({
  selector: 'app-report',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    DecimalPipe
  ],
  templateUrl: './report.html',
  styleUrl: './report.scss'
})
export class ReportComponent implements OnInit {

  private readonly reportService =
    inject(ReportService);

  private readonly http =
    inject(HttpClient);

  private readonly formBuilder =
    inject(FormBuilder);

  // Role-secured employee options endpoint
  private readonly employeeOptionsUrl =
    `${environment.apiUrl}/reports/employee-options`;

  private readonly projectsUrl =
    `${environment.apiUrl}/projects`;

  readonly report =
    signal<WorkReport | null>(null);

  readonly employees =
    signal<EmployeeOption[]>([]);

  readonly projects =
    signal<ProjectOption[]>([]);

  readonly loading =
    signal(false);

  readonly errorMessage =
    signal('');

  readonly reportForm =
    this.formBuilder.group({
      fromDate: [
        this.getFirstDayOfCurrentMonth(),
        [
          Validators.required
        ]
      ],

      toDate: [
        this.getToday(),
        [
          Validators.required
        ]
      ],

      employeeId: [
        null as number | null
      ],

      projectId: [
        null as number | null
      ]
    });

  ngOnInit(): void {
    this.loadFilterOptions();
    this.loadReport();
  }

  loadReport(): void {
    this.errorMessage.set('');

    if (this.reportForm.invalid) {
      this.reportForm.markAllAsTouched();
      return;
    }

    const fromDate =
      this.reportForm.controls
        .fromDate.value;

    const toDate =
      this.reportForm.controls
        .toDate.value;

    if (!fromDate || !toDate) {
      return;
    }

    if (toDate < fromDate) {
      this.errorMessage.set(
        'To date cannot be earlier than from date.'
      );
      return;
    }

    const filter: ReportFilter = {
      fromDate,
      toDate,

      employeeId:
        this.reportForm.controls
          .employeeId.value,

      projectId:
        this.reportForm.controls
          .projectId.value
    };

    this.loading.set(true);

    this.reportService
      .getWorkSummary(filter)
      .subscribe({
        next: report => {
          this.report.set(report);
          this.loading.set(false);
        },

        error: (
          error: HttpErrorResponse
        ) => {
          console.error(
            'Report loading error:',
            error
          );

          this.report.set(null);

          if (error.status === 403) {
            this.errorMessage.set(
              'You do not have permission to view this employee report.'
            );
          } else if (error.status === 401) {
            this.errorMessage.set(
              error.error?.message ??
                'Your login is not authorized to view this report.'
            );
          } else {
            this.errorMessage.set(
              error.error?.message ??
                'Unable to load the report.'
            );
          }

          this.loading.set(false);
        }
      });
  }

  resetFilters(): void {
    this.reportForm.reset({
      fromDate:
        this.getFirstDayOfCurrentMonth(),

      toDate:
        this.getToday(),

      employeeId: null,
      projectId: null
    });

    this.loadReport();
  }

  exportCsv(): void {
    this.errorMessage.set('');

    const report =
      this.report();

    if (
      !report ||
      report.workLogs.length === 0
    ) {
      this.errorMessage.set(
        'There are no work-log records to export.'
      );
      return;
    }

    const headers = [
      'Work Date',
      'Employee Code',
      'Employee Name',
      'Project Code',
      'Project Name',
      'Task Code',
      'Task Title',
      'Hours',
      'Progress',
      'Status',
      'Description',
      'Blockers'
    ];

    const rows =
      report.workLogs.map(
        workLog => [
          workLog.workDate,
          workLog.employeeCode,
          workLog.employeeName,
          workLog.projectCode,
          workLog.projectName,
          workLog.taskCode ?? '',
          workLog.taskTitle ?? '',
          workLog.hoursWorked.toString(),
          `${workLog.progressPercentage}%`,
          workLog.status,
          workLog.workDescription,
          workLog.blockers ?? ''
        ]
      );

    const csvContent = [
      headers,
      ...rows
    ]
      .map(row =>
        row
          .map(value =>
            this.escapeCsvValue(value)
          )
          .join(',')
      )
      .join('\r\n');

    const blob =
      new Blob(
        [csvContent],
        {
          type:
            'text/csv;charset=utf-8;'
        }
      );

    const downloadUrl =
      URL.createObjectURL(blob);

    const anchor =
      document.createElement('a');

    anchor.href =
      downloadUrl;

    anchor.download =
      `work-report-${report.fromDate.slice(0, 10)}` +
      `-to-${report.toDate.slice(0, 10)}.csv`;

    anchor.click();

    URL.revokeObjectURL(
      downloadUrl
    );
  }

  getStatusClass(
    status: WorkLogStatus
  ): string {
    return status.toLowerCase();
  }

  private loadFilterOptions(): void {

    // Employees are filtered by backend role
    this.http
      .get<EmployeeOption[]>(
        this.employeeOptionsUrl
      )
      .subscribe({
        next: employees => {
          const activeEmployees =
            employees.filter(
              employee => employee.isActive
            );

          this.employees.set(
            activeEmployees
          );

          if (activeEmployees.length === 1) {
            this.reportForm.controls
              .employeeId
              .setValue(
                activeEmployees[0].id
              );

            this.loadReport();
          }
        },

        error: (
          error: HttpErrorResponse
        ) => {
          console.error(
            'Employee report option error:',
            error
          );

          this.employees.set([]);

          if (error.status === 403) {
            this.errorMessage.set(
              'You do not have permission to load report employees.'
            );
          } else {
            this.errorMessage.set(
              error.error?.message ??
                'Unable to load report employees.'
            );
          }
        }
      });

    // Load project filter options
    this.http
      .get<ProjectOption[]>(
        this.projectsUrl
      )
      .subscribe({
        next: projects => {
          this.projects.set(
            projects.filter(
              project => project.isActive
            )
          );
        },

        error: (
          error: HttpErrorResponse
        ) => {
          console.error(
            'Project loading error:',
            error
          );

          this.projects.set([]);
        }
      });
  }

  private getToday(): string {
    return this.formatInputDate(
      new Date()
    );
  }

  private getFirstDayOfCurrentMonth():
    string {
    const today =
      new Date();

    return this.formatInputDate(
      new Date(
        today.getFullYear(),
        today.getMonth(),
        1
      )
    );
  }

  private formatInputDate(
    date: Date
  ): string {
    const year =
      date.getFullYear();

    const month =
      String(
        date.getMonth() + 1
      ).padStart(
        2,
        '0'
      );

    const day =
      String(
        date.getDate()
      ).padStart(
        2,
        '0'
      );

    return `${year}-${month}-${day}`;
  }

  private escapeCsvValue(
    value: string
  ): string {
    const escaped =
      value.replace(
        /"/g,
        '""'
      );

    return `"${escaped}"`;
  }
}
