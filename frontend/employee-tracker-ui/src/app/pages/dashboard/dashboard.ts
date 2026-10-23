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
import { environment } from '../../../environments/environment';

interface RecentWorkLog {
  id: number;
  workDate: string;

  employeeCode: string;
  employeeName: string;

  projectCode: string;
  projectName: string;

  taskCode: string | null;
  taskTitle: string | null;

  hoursWorked: number;
  progressPercentage: number;
  status: string;
  workDescription: string;
}

interface DashboardData {
  role: string;

  totalEmployees: number;
  activeEmployees: number;

  totalProjects: number;
  activeProjects: number;

  totalTasks: number;
  pendingTasks: number;
  completedTasks: number;

  totalHours: number;
  totalWorkHours: number;
  totalHoursWorked: number;

  totalWorkLogs: number;
  draftWorkLogs: number;
  submittedWorkLogs: number;
  approvedWorkLogs: number;
  rejectedWorkLogs: number;

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

  private readonly http =
    inject(HttpClient);

  private readonly apiUrl =
  `${environment.apiUrl}/dashboard`;

  readonly dashboard =
    signal<DashboardData | null>(null);

  readonly loading =
    signal(false);

  readonly errorMessage =
    signal('');

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);

    this.errorMessage.set('');

    this.http
      .get<DashboardData>(
        this.apiUrl
      )
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

  getDashboardTitle(
    role: string
  ): string {
    if (role === 'Administrator') {
      return 'Administrator Dashboard';
    }

    if (role === 'Manager') {
      return 'Manager Dashboard';
    }

    if (role === 'Employee') {
      return 'My Dashboard';
    }

    return 'Dashboard';
  }

  getEmployeeCardTitle(
    role: string
  ): string {
    if (role === 'Manager') {
      return 'Team Employees';
    }

    if (role === 'Employee') {
      return 'My Profile';
    }

    return 'Total Employees';
  }

  getProjectCardTitle(
    role: string
  ): string {
    if (role === 'Manager') {
      return 'Team Projects';
    }

    if (role === 'Employee') {
      return 'My Projects';
    }

    return 'Total Projects';
  }

  getTaskCardTitle(
    role: string
  ): string {
    if (role === 'Manager') {
      return 'Team Tasks';
    }

    if (role === 'Employee') {
      return 'My Tasks';
    }

    return 'Total Tasks';
  }

  getRecentTitle(
    role: string
  ): string {
    if (role === 'Manager') {
      return 'Recent Team Work Logs';
    }

    if (role === 'Employee') {
      return 'My Recent Work Logs';
    }

    return 'Recent Work Logs';
  }
}