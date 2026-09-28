import {
  Component,
  inject,
  OnInit,
  signal
} from '@angular/core';

import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

import {
  HttpErrorResponse
} from '@angular/common/http';

import {
  forkJoin
} from 'rxjs';

import {
  Employee
} from '../../models/employee';

import {
  Project
} from '../../models/project';

import {
  WorkTask,
  WorkTaskPriority,
  WorkTaskRequest,
  WorkTaskStatus
} from '../../models/work-task';

import {
  TaskStatus
} from '../../models/task-status';

import {
  AuthService
} from '../../services/auth.service';

import {
  EmployeeService
} from '../../services/employee.service';

import {
  ProjectService
} from '../../services/project.service';

import {
  WorkTaskService
} from '../../services/work-task.service';

import {
  TaskStatusService
} from '../../services/task-status.service';

@Component({
  selector: 'app-work-task',
  standalone: true,
  imports: [
    ReactiveFormsModule
  ],
  templateUrl: './work-task.html',
  styleUrl: './work-task.scss'
})
export class WorkTaskComponent implements OnInit {
  private readonly authService =
    inject(AuthService);

  private readonly employeeService =
    inject(EmployeeService);

  private readonly projectService =
    inject(ProjectService);

  private readonly workTaskService =
    inject(WorkTaskService);

  private readonly taskStatusService =
    inject(TaskStatusService);

  private readonly formBuilder =
    inject(FormBuilder);

  readonly projects = signal<Project[]>([]);
  readonly employees = signal<Employee[]>([]);
  readonly workTasks = signal<WorkTask[]>([]);

  readonly loading = signal(false);
  readonly saving = signal(false);

  readonly updatingStatusId =
    signal<number | null>(null);

  readonly editingId =
    signal<number | null>(null);

  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  readonly priorities: WorkTaskPriority[] = [
    'Low',
    'Medium',
    'High',
    'Critical'
  ];

  readonly statuses: WorkTaskStatus[] = [
    'Not Started',
    'In Progress',
    'On Hold',
    'Completed',
    'Cancelled'
  ];

  readonly employeeStatuses: TaskStatus[] = [
    'Not Started',
    'In Progress',
    'On Hold',
    'Completed'
  ];

  readonly workTaskForm =
    this.formBuilder.nonNullable.group({
      taskCode: [
        '',
        [
          Validators.required,
          Validators.maxLength(20)
        ]
      ],

      taskTitle: [
        '',
        [
          Validators.required,
          Validators.maxLength(150)
        ]
      ],

      projectId: [
        0,
        [
          Validators.required,
          Validators.min(1)
        ]
      ],

      assignedEmployeeId: [0],

      startDate: [
        this.getTodayDate(),
        [
          Validators.required
        ]
      ],

      dueDate: [''],

      priority: [
        'Medium' as WorkTaskPriority,
        [
          Validators.required
        ]
      ],

      status: [
        'Not Started' as WorkTaskStatus,
        [
          Validators.required
        ]
      ],

      estimatedHours: [
        0,
        [
          Validators.required,
          Validators.min(0)
        ]
      ],

      description: [
        '',
        [
          Validators.maxLength(1000)
        ]
      ],

      isActive: [true]
    });

  ngOnInit(): void {
    this.loadPageData();
  }

  canManageTasks(): boolean {
    const role =
      this.authService.currentUserRole();

    return role === 'Administrator' ||
      role === 'Manager';
  }

  isEmployee(): boolean {
    return this.authService
      .currentUserRole() === 'Employee';
  }

  loadPageData(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    if (this.canManageTasks()) {
      this.loadManagementData();
      return;
    }

    this.loadEmployeeTasks();
  }

  private loadManagementData(): void {
    forkJoin({
      projects:
        this.projectService.getProjects(),

      employees:
        this.employeeService.getEmployees(),

      tasks:
        this.workTaskService.getWorkTasks()
    }).subscribe({
      next: ({
        projects,
        employees,
        tasks
      }) => {
        this.projects.set(
          projects.filter(project =>
            project.isActive
          )
        );

        this.employees.set(
          employees.filter(employee =>
            employee.isActive
          )
        );

        this.workTasks.set(tasks);
        this.loading.set(false);
      },

      error: (error: HttpErrorResponse) => {
        this.handleLoadingError(error);
      }
    });
  }

  private loadEmployeeTasks(): void {
    this.workTaskService
      .getWorkTasks()
      .subscribe({
        next: tasks => {
          this.projects.set([]);
          this.employees.set([]);
          this.workTasks.set(tasks);
          this.loading.set(false);
        },

        error: (error: HttpErrorResponse) => {
          this.handleLoadingError(error);
        }
      });
  }

  updateTaskStatus(
    task: WorkTask,
    statusValue: string
  ): void {
    const status =
      statusValue as TaskStatus;

    if (!this.employeeStatuses.includes(status)) {
      this.errorMessage.set(
        'Select a valid task status.'
      );

      return;
    }

    if (task.status === status) {
      return;
    }

    this.updatingStatusId.set(task.id);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.taskStatusService
      .updateStatus(task.id, status)
      .subscribe({
        next: response => {
          this.workTasks.update(tasks =>
            tasks.map(existingTask =>
              existingTask.id === task.id
                ? {
                    ...existingTask,
                    status: response.status
                  }
                : existingTask
            )
          );

          this.successMessage.set(
            response.message
          );

          this.updatingStatusId.set(null);
        },

        error: (error: HttpErrorResponse) => {
          console.error(
            'Task status update error:',
            error
          );

          this.errorMessage.set(
            error.error?.message ??
              'Unable to update task status.'
          );

          this.updatingStatusId.set(null);

          // Restore the actual value from
          // the backend after a failed update.
          this.loadPageData();
        }
      });
  }

  saveWorkTask(): void {
    this.successMessage.set('');
    this.errorMessage.set('');

    if (!this.canManageTasks()) {
      this.errorMessage.set(
        'You do not have permission to create or edit tasks.'
      );

      return;
    }

    if (this.workTaskForm.invalid) {
      this.workTaskForm.markAllAsTouched();
      return;
    }

    const startDate =
      this.workTaskForm.controls
        .startDate.value;

    const dueDate =
      this.workTaskForm.controls
        .dueDate.value;

    if (dueDate &&
        new Date(dueDate) <
        new Date(startDate)) {
      this.errorMessage.set(
        'Due date cannot be earlier than the start date.'
      );

      return;
    }

    this.saving.set(true);

    const employeeId =
      this.workTaskForm.controls
        .assignedEmployeeId.value;

    const description =
      this.workTaskForm.controls
        .description.value.trim();

    const request: WorkTaskRequest = {
      taskCode:
        this.workTaskForm.controls
          .taskCode.value.trim(),

      taskTitle:
        this.workTaskForm.controls
          .taskTitle.value.trim(),

      projectId:
        this.workTaskForm.controls
          .projectId.value,

      assignedEmployeeId:
        employeeId > 0
          ? employeeId
          : null,

      startDate,

      dueDate:
        dueDate.length > 0
          ? dueDate
          : null,

      priority:
        this.workTaskForm.controls
          .priority.value,

      status:
        this.workTaskForm.controls
          .status.value,

      estimatedHours:
        this.workTaskForm.controls
          .estimatedHours.value,

      description:
        description.length > 0
          ? description
          : null,

      isActive:
        this.workTaskForm.controls
          .isActive.value
    };

    const id = this.editingId();

    if (id === null) {
      this.createWorkTask(request);
    } else {
      this.updateWorkTask(id, request);
    }
  }

  private createWorkTask(
    request: WorkTaskRequest
  ): void {
    this.workTaskService
      .createWorkTask(request)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Task added successfully.'
          );

          this.saving.set(false);
          this.resetForm();
          this.loadPageData();
        },

        error: (error: HttpErrorResponse) =>
          this.handleSaveError(error)
      });
  }

  private updateWorkTask(
    id: number,
    request: WorkTaskRequest
  ): void {
    this.workTaskService
      .updateWorkTask(id, request)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Task updated successfully.'
          );

          this.saving.set(false);
          this.resetForm();
          this.loadPageData();
        },

        error: (error: HttpErrorResponse) =>
          this.handleSaveError(error)
      });
  }

  editWorkTask(task: WorkTask): void {
    if (!this.canManageTasks()) {
      return;
    }

    this.editingId.set(task.id);
    this.successMessage.set('');
    this.errorMessage.set('');

    this.workTaskForm.patchValue({
      taskCode: task.taskCode,
      taskTitle: task.taskTitle,
      projectId: task.projectId,

      assignedEmployeeId:
        task.assignedEmployeeId ?? 0,

      startDate:
        task.startDate.substring(0, 10),

      dueDate:
        task.dueDate?.substring(0, 10) ?? '',

      priority: task.priority,
      status: task.status,

      estimatedHours:
        task.estimatedHours,

      description:
        task.description ?? '',

      isActive: task.isActive
    });

    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  }

  deleteWorkTask(task: WorkTask): void {
    if (!this.canManageTasks()) {
      return;
    }

    const shouldDelete = window.confirm(
      `Delete task "${task.taskTitle}"?`
    );

    if (!shouldDelete) {
      return;
    }

    this.workTaskService
      .deleteWorkTask(task.id)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Task deleted successfully.'
          );

          this.loadPageData();
        },

        error: (error: HttpErrorResponse) => {
          this.errorMessage.set(
            error.error?.message ??
              'Unable to delete task.'
          );
        }
      });
  }

  resetForm(): void {
    this.editingId.set(null);

    this.workTaskForm.reset({
      taskCode: '',
      taskTitle: '',
      projectId: 0,
      assignedEmployeeId: 0,
      startDate: this.getTodayDate(),
      dueDate: '',
      priority: 'Medium',
      status: 'Not Started',
      estimatedHours: 0,
      description: '',
      isActive: true
    });
  }

  isInvalid(
    controlName:
      | 'taskCode'
      | 'taskTitle'
      | 'projectId'
      | 'startDate'
      | 'priority'
      | 'status'
      | 'estimatedHours'
      | 'description'
  ): boolean {
    const control =
      this.workTaskForm.controls[controlName];

    return control.invalid &&
      (control.touched || control.dirty);
  }

  private getTodayDate(): string {
    const today = new Date();

    const offset =
      today.getTimezoneOffset() * 60000;

    return new Date(
      today.getTime() - offset
    )
      .toISOString()
      .substring(0, 10);
  }

  private handleLoadingError(
    error: HttpErrorResponse
  ): void {
    console.error(
      'Task loading error:',
      error
    );

    this.errorMessage.set(
      error.error?.message ??
        'Unable to load task information.'
    );

    this.loading.set(false);
  }

  private handleSaveError(
    error: HttpErrorResponse
  ): void {
    console.error(
      'Task saving error:',
      error
    );

    this.errorMessage.set(
      error.error?.message ??
        'Unable to save task.'
    );

    this.saving.set(false);
  }
}