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
import { environment } from '../../../environments/environment';

interface WorkLogApproval {
  id: number;
  employeeId: number;
  employeeName: string;
  projectId: number;
  projectName: string;
  taskId: number;
  taskTitle: string;
  workDate: string;
  hoursWorked: number;
  progressPercentage: number;
  status: string;
  workDescription: string;
  blockers: string | null;
  createdAt: string;
}

type ApprovalStatus =
  | 'Approved'
  | 'Rejected';

@Component({
  selector: 'app-work-log-approval',
  imports: [],
  templateUrl: './work-log-approval.html',
  styleUrl: './work-log-approval.scss'
})
export class WorkLogApprovalComponent
  implements OnInit {

  private readonly http = inject(HttpClient);

  private readonly apiUrl =
  `${environment.apiUrl}/daily-work-logs`

  readonly workLogs =
    signal<WorkLogApproval[]>([]);

  readonly loading = signal(false);

  readonly processingId =
    signal<number | null>(null);

  readonly errorMessage = signal('');

  readonly successMessage = signal('');

  ngOnInit(): void {
    this.loadWorkLogs();
  }

  loadWorkLogs(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.http
      .get<WorkLogApproval[]>(this.apiUrl)
      .subscribe({
        next: (
          workLogs: WorkLogApproval[]
        ) => {
          const submittedWorkLogs =
            workLogs.filter(
              (workLog: WorkLogApproval) =>
                workLog.status
                  .toLowerCase() ===
                'submitted'
            );

          this.workLogs.set(
            submittedWorkLogs
          );

          this.loading.set(false);
        },
        error: (
          error: HttpErrorResponse
        ) => {
          console.error(
            'Approval loading error:',
            error
          );

          this.workLogs.set([]);

          this.errorMessage.set(
            error.error?.message ??
              'Unable to load submitted work logs.'
          );

          this.loading.set(false);
        }
      });
  }

  approve(
    workLog: WorkLogApproval
  ): void {
    this.changeStatus(
      workLog,
      'Approved'
    );
  }

  reject(
    workLog: WorkLogApproval
  ): void {
    this.changeStatus(
      workLog,
      'Rejected'
    );
  }

  private changeStatus(
    workLog: WorkLogApproval,
    status: ApprovalStatus
  ): void {
    const confirmed =
      window.confirm(
        `${status} the work log of ` +
        `"${workLog.employeeName}"?`
      );

    if (!confirmed) {
      return;
    }

    this.processingId.set(workLog.id);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.http
      .patch<void>(
        `${this.apiUrl}/${workLog.id}/status`,
        { status }
      )
      .subscribe({
        next: () => {
          this.successMessage.set(
            `Work log ` +
            `${status.toLowerCase()} ` +
            `successfully.`
          );

          this.processingId.set(null);

          this.removeProcessedWorkLog(
            workLog.id
          );
        },
        error: (
          error: HttpErrorResponse
        ) => {
          console.error(
            'Approval status error:',
            error
          );

          this.errorMessage.set(
            error.error?.message ??
              `Unable to ` +
              `${status.toLowerCase()} ` +
              `the work log.`
          );

          this.processingId.set(null);
        }
      });
  }

  private removeProcessedWorkLog(
    id: number
  ): void {
    this.workLogs.update(
      (workLogs: WorkLogApproval[]) =>
        workLogs.filter(
          (workLog: WorkLogApproval) =>
            workLog.id !== id
        )
    );
  }
}