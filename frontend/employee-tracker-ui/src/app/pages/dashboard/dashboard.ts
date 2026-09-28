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
import { DatePipe } from '@angular/common';

interface RecentWorkLog {
  id: number;
  workDate: string;
  employeeName: string;
  projectName: string;
  taskTitle: string;
  hoursWorked: number;
  progressPercentage: number;
  status: string;
  workDescription: string;
}

interface DashboardData {
  totalEmployees: number;
  activeEmployees: number;

  totalProjects: number;
  activeProjects: number;

  totalTasks: number;
  pendingTasks: number;
  completedTasks: number;

  totalWorkLogs: number;
  draftWorkLogs: number;
  submittedWorkLogs: number;
  approvedWorkLogs: number;
  rejectedWorkLogs: number;

  totalHoursWorked: number;

  recentWorkLogs: RecentWorkLog[];
}

@Component({
  selector: 'app-dashboard',
  imports: [
    DatePipe
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent
  implements OnInit {

  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5027/api/dashboard';

  readonly dashboard =
    signal<DashboardData | null>(null);

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.http
      .get<DashboardData>(this.apiUrl)
      .subscribe({
        next: (
          dashboardData: DashboardData
        ) => {
          this.dashboard.set(
            dashboardData
          );

          this.loading.set(false);
        },
        error: (
          error: HttpErrorResponse
        ) => {
          console.error(
            'Dashboard loading error:',
            error
          );

          if (error.status === 0) {
            this.errorMessage.set(
              'Unable to connect to the backend. ' +
              'Confirm that dotnet run is active.'
            );
          } else {
            this.errorMessage.set(
              error.error?.message ??
                'Unable to load dashboard information.'
            );
          }

          this.loading.set(false);
        }
      });
  }
}