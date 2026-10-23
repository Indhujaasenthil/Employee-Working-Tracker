import {
  Component,
  inject,
  OnInit,
  signal
} from '@angular/core';

import {
  HttpClient,
  HttpErrorResponse
} from '@angular/common/http';

import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

import { forkJoin } from 'rxjs';
import { environment } from '../../../environments/environment';

interface Employee {
  id: number;
  employeeCode: string;
  fullName: string;
  isActive: boolean;
}

interface Project {
  id: number;
  projectCode: string;
  projectName: string;
  isActive: boolean;
}

interface WorkTask {
  id: number;
  taskCode: string;
  taskTitle: string;
  projectId: number;
  projectName?: string;
  assignedEmployeeId: number | null;
  assignedEmployeeName?: string;
  status: string;
  isActive: boolean;
}

type WorkLogStatus =
  | 'Draft'
  | 'Submitted'
  | 'Approved'
  | 'Rejected';

interface DailyWorkLog {
  id: number;

  employeeId: number;
  employeeCode?: string;
  employeeName: string;

  projectId: number;
  projectCode?: string;
  projectName: string;

  workTaskId: number | null;
  taskCode?: string | null;
  taskTitle: string | null;

  workDate: string;
  hoursWorked: number;
  progressPercentage: number;

  status: WorkLogStatus;

  workDescription: string;
  blockers: string | null;

  createdAt: string;
}

interface DailyWorkLogRequest {
  employeeId: number;
  projectId: number;

  /*
   * IMPORTANT:
   * The backend expects WorkTaskId.
   * ASP.NET JSON converts it to workTaskId.
   */
  workTaskId: number | null;

  workDate: string;
  hoursWorked: number;
  progressPercentage: number;

  status: WorkLogStatus;

  workDescription: string;
  blockers: string | null;
}

@Component({
  selector: 'app-work-log',
  standalone: true,
  imports: [
    ReactiveFormsModule
  ],
  templateUrl: './work-log.html',
  styleUrl: './work-log.scss'
})
export class WorkLogComponent
  implements OnInit {

  private readonly http =
    inject(HttpClient);

  private readonly formBuilder =
    inject(FormBuilder);

  private readonly apiUrl =
    environment.apiUrl;

  readonly employees =
    signal<Employee[]>([]);

  readonly projects =
    signal<Project[]>([]);

  readonly tasks =
    signal<WorkTask[]>([]);

  readonly filteredTasks =
    signal<WorkTask[]>([]);

  /*
   * Keep this alias in case another part
   * of the page uses availableTasks().
   */
  readonly availableTasks =
    this.filteredTasks;

  readonly workLogs =
    signal<DailyWorkLog[]>([]);

  readonly loading =
    signal(false);

  readonly saving =
    signal(false);

  readonly editingId =
    signal<number | null>(null);

  readonly errorMessage =
    signal('');

  readonly successMessage =
    signal('');

  readonly workLogForm =
    this.formBuilder.nonNullable.group({
      employeeId: [
        0,
        [
          Validators.required,
          Validators.min(1)
        ]
      ],

      projectId: [
        0,
        [
          Validators.required,
          Validators.min(1)
        ]
      ],

      /*
       * taskId is only the Angular
       * form-control name.
       */
      taskId: [
        0,
        [
          Validators.required,
          Validators.min(1)
        ]
      ],

      workDate: [
        this.getCurrentDate(),
        [
          Validators.required
        ]
      ],

      hoursWorked: [
        8,
        [
          Validators.required,
          Validators.min(0.25),
          Validators.max(24)
        ]
      ],

      progressPercentage: [
        0,
        [
          Validators.required,
          Validators.min(0),
          Validators.max(100)
        ]
      ],

      status: [
        'Draft' as WorkLogStatus,
        [
          Validators.required
        ]
      ],

      workDescription: [
        '',
        [
          Validators.required,
          Validators.maxLength(1000)
        ]
      ],

      blockers: [
        '',
        [
          Validators.maxLength(500)
        ]
      ]
    });

  ngOnInit(): void {
    this.registerFormChanges();
    this.loadPageData();
  }

  loadPageData(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    forkJoin({
      employees:
        this.http.get<Employee[]>(
          `${this.apiUrl}/employees`
        ),

      projects:
        this.http.get<Project[]>(
          `${this.apiUrl}/projects`
        ),

      tasks:
        this.http.get<WorkTask[]>(
          `${this.apiUrl}/worktasks`
        ),

      workLogs:
        this.http.get<DailyWorkLog[]>(
          `${this.apiUrl}/daily-work-logs`
        )
    }).subscribe({
      next: result => {
        this.employees.set(
          result.employees.filter(
            employee =>
              employee.isActive
          )
        );

        this.projects.set(
          result.projects.filter(
            project =>
              project.isActive
          )
        );

        this.tasks.set(
          result.tasks.filter(
            task =>
              task.isActive
          )
        );

        this.workLogs.set(
          result.workLogs
        );

        this.setDefaultSelections();

        this.filterTasks();

        this.loading.set(false);
      },

      error: (
        error: HttpErrorResponse
      ) => {
        console.error(
          'Work-log page loading error:',
          error
        );

        this.errorMessage.set(
          this.getErrorMessage(
            error,
            'Unable to load daily work-log information.'
          )
        );

        this.loading.set(false);
      }
    });
  }

  loadWorkLogs(): void {
    this.loadPageData();
  }

  private registerFormChanges(): void {
    this.workLogForm.controls.projectId
      .valueChanges
      .subscribe(() => {
        this.workLogForm.controls.taskId
          .setValue(
            0,
            {
              emitEvent: false
            }
          );

        this.filterTasks();
      });

    this.workLogForm.controls.employeeId
      .valueChanges
      .subscribe(() => {
        this.workLogForm.controls.taskId
          .setValue(
            0,
            {
              emitEvent: false
            }
          );

        this.filterTasks();
      });
  }

  private setDefaultSelections(): void {
    if (
      this.workLogForm.controls
        .employeeId.value === 0 &&
      this.employees().length > 0
    ) {
      this.workLogForm.controls
        .employeeId
        .setValue(
          this.employees()[0].id,
          {
            emitEvent: false
          }
        );
    }

    if (
      this.workLogForm.controls
        .projectId.value === 0 &&
      this.projects().length > 0
    ) {
      this.workLogForm.controls
        .projectId
        .setValue(
          this.projects()[0].id,
          {
            emitEvent: false
          }
        );
    }
  }
  formatDisplayDate(value: string): string {
    if (!value) {
      return '-';
    }

    const datePart = value.substring(0, 10);
    const [year, month, day] = datePart.split('-');

    if (!year || !month || !day) {
      return value;
    }

    return `${day}/${month}/${year}`;
  }
  filterTasks(): void {
    const projectId = Number(
      this.workLogForm.controls.projectId.value
    );

    if (projectId <= 0) {
      this.filteredTasks.set([]);
      return;
    }

    const matchingTasks = this.tasks().filter(
      (task: WorkTask) =>
        Number(task.projectId) === projectId &&
        task.isActive
    );

    this.filteredTasks.set(matchingTasks);
  }

  saveWorkLog(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (this.workLogForm.invalid) {
      this.workLogForm
        .markAllAsTouched();

      this.errorMessage.set(
        'Please complete all required fields.'
      );

      return;
    }

    const formValue =
      this.workLogForm.getRawValue();

    /*
     * IMPORTANT:
     *
     * Angular form:
     * taskId
     *
     * API:
     * workTaskId
     */
    const request:
      DailyWorkLogRequest = {

      employeeId:
        Number(
          formValue.employeeId
        ),

      projectId:
        Number(
          formValue.projectId
        ),

      workTaskId:
        Number(
          formValue.taskId
        ),

      workDate:
        formValue.workDate,

      hoursWorked:
        Number(
          formValue.hoursWorked
        ),

      progressPercentage:
        Number(
          formValue.progressPercentage
        ),

      status:
        formValue.status,

      workDescription:
        formValue.workDescription
          .trim(),

      blockers:
        formValue.blockers
          .trim() || null
    };

    this.saving.set(true);

    const id =
      this.editingId();

    if (id === null) {
      this.createWorkLog(
        request
      );

      return;
    }

    this.updateWorkLog(
      id,
      request
    );
  }

  private createWorkLog(
    request: DailyWorkLogRequest
  ): void {
    this.http
      .post<DailyWorkLog>(
        `${this.apiUrl}/daily-work-logs`,
        request
      )
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Daily work log added successfully.'
          );

          this.saving.set(false);

          this.resetForm();

          this.loadPageData();
        },

        error: (
          error: HttpErrorResponse
        ) => {
          this.handleSaveError(
            error
          );
        }
      });
  }

  private updateWorkLog(
    id: number,
    request: DailyWorkLogRequest
  ): void {
    this.http
      .put<void>(
        `${this.apiUrl}/daily-work-logs/${id}`,
        request
      )
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Daily work log updated successfully.'
          );

          this.saving.set(false);

          this.resetForm();

          this.loadPageData();
        },

        error: (
          error: HttpErrorResponse
        ) => {
          this.handleSaveError(
            error
          );
        }
      });
  }

  editWorkLog(
    workLog: DailyWorkLog
  ): void {
    /*
     * Approved and Submitted records
     * should normally be controlled by
     * the workflow rather than freely
     * edited.
     */
    if (
      workLog.status ===
      'Approved' ||
      workLog.status ===
      'Submitted'
    ) {
      this.errorMessage.set(
        `A ${workLog.status.toLowerCase()} ` +
        'work log cannot be edited.'
      );

      return;
    }

    this.editingId.set(
      workLog.id
    );

    this.errorMessage.set('');
    this.successMessage.set('');

    this.workLogForm.patchValue(
      {
        employeeId:
          Number(
            workLog.employeeId
          ),

        projectId:
          Number(
            workLog.projectId
          ),

        /*
         * API response uses workTaskId.
         * Angular form uses taskId.
         */
        taskId:
          Number(
            workLog.workTaskId ?? 0
          ),

        workDate:
          this.formatDateForInput(
            workLog.workDate
          ),

        hoursWorked:
          Number(
            workLog.hoursWorked
          ),

        progressPercentage:
          Number(
            workLog.progressPercentage
          ),

        /*
         * Rejected work logs can be
         * corrected and submitted again.
         */
        status:
          workLog.status ===
            'Rejected'
            ? 'Draft'
            : workLog.status,

        workDescription:
          workLog.workDescription,

        blockers:
          workLog.blockers ?? ''
      },
      {
        emitEvent: false
      }
    );

    this.filterTasks();

    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  }

  deleteWorkLog(
    workLog: DailyWorkLog
  ): void {
    if (
      workLog.status ===
      'Approved' ||
      workLog.status ===
      'Submitted'
    ) {
      this.errorMessage.set(
        `A ${workLog.status.toLowerCase()} ` +
        'work log cannot be deleted.'
      );

      return;
    }

    const confirmed =
      window.confirm(
        `Delete the work log for ` +
        `"${workLog.employeeName}"?`
      );

    if (!confirmed) {
      return;
    }

    this.errorMessage.set('');
    this.successMessage.set('');

    this.http
      .delete<void>(
        `${this.apiUrl}/daily-work-logs/${workLog.id}`
      )
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Daily work log deleted successfully.'
          );

          if (
            this.editingId() ===
            workLog.id
          ) {
            this.resetForm();
          }

          this.loadPageData();
        },

        error: (
          error: HttpErrorResponse
        ) => {
          console.error(
            'Work-log deletion error:',
            error
          );

          this.errorMessage.set(
            this.getErrorMessage(
              error,
              'Unable to delete daily work log.'
            )
          );
        }
      });
  }

  canEdit(
    workLog: DailyWorkLog
  ): boolean {
    return (
      workLog.status === 'Draft' ||
      workLog.status === 'Rejected'
    );
  }

  canDelete(
    workLog: DailyWorkLog
  ): boolean {
    return (
      workLog.status === 'Draft' ||
      workLog.status === 'Rejected'
    );
  }

  resetForm(): void {
    this.editingId.set(null);

    const employeeId =
      this.employees().length > 0
        ? this.employees()[0].id
        : 0;

    const projectId =
      this.projects().length > 0
        ? this.projects()[0].id
        : 0;

    this.workLogForm.reset({
      employeeId,
      projectId,
      taskId: 0,

      workDate:
        this.getCurrentDate(),

      hoursWorked: 8,

      progressPercentage: 0,

      status: 'Draft',

      workDescription: '',

      blockers: ''
    });

    this.filterTasks();
  }

  isInvalid(
    controlName:
      | 'employeeId'
      | 'projectId'
      | 'taskId'
      | 'workDate'
      | 'hoursWorked'
      | 'progressPercentage'
      | 'status'
      | 'workDescription'
      | 'blockers'
  ): boolean {
    const control =
      this.workLogForm
        .controls[controlName];

    return (
      control.invalid &&
      (
        control.touched ||
        control.dirty
      )
    );
  }

  private handleSaveError(
    error: HttpErrorResponse
  ): void {
    console.error(
      'Work-log saving error:',
      error
    );

    this.errorMessage.set(
      this.getErrorMessage(
        error,
        'Unable to save daily work log.'
      )
    );

    this.saving.set(false);
  }

  private getErrorMessage(
    error: HttpErrorResponse,
    defaultMessage: string
  ): string {
    if (
      typeof error.error?.message ===
      'string'
    ) {
      return error.error.message;
    }

    /*
     * ASP.NET validation errors may be
     * returned in the errors object.
     */
    if (
      error.error?.errors &&
      typeof error.error.errors ===
      'object'
    ) {
      const validationMessages =
        Object.values(
          error.error.errors
        )
          .flat()
          .filter(
            message =>
              typeof message ===
              'string'
          );

      if (
        validationMessages.length > 0
      ) {
        return validationMessages
          .join(' ');
      }
    }

    if (error.status === 401) {
      return (
        'Your login session is invalid or expired. ' +
        'Please log in again.'
      );
    }

    if (error.status === 403) {
      return (
        'You do not have permission to perform ' +
        'this work-log action.'
      );
    }

    if (error.status === 0) {
      return (
        'Unable to connect to the backend. ' +
        'Confirm that dotnet run is active.'
      );
    }

    return defaultMessage;
  }

  private getCurrentDate(): string {
    const today =
      new Date();

    const year =
      today.getFullYear();

    const month =
      String(
        today.getMonth() + 1
      ).padStart(
        2,
        '0'
      );

    const day =
      String(
        today.getDate()
      ).padStart(
        2,
        '0'
      );

    return (
      `${year}-${month}-${day}`
    );
  }

  private formatDateForInput(
    value: string
  ): string {
    if (!value) {
      return this.getCurrentDate();
    }

    return value.substring(
      0,
      10
    );
  }
}