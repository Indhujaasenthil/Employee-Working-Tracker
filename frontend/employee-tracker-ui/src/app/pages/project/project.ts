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
import { HttpErrorResponse } from '@angular/common/http';

import {
  Project,
  ProjectRequest,
  ProjectStatus
} from '../../models/project';
import { ProjectService } from '../../services/project.service';

@Component({
  selector: 'app-project',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './project.html',
  styleUrl: './project.scss'
})
export class ProjectComponent implements OnInit {
  private readonly projectService =
    inject(ProjectService);

  private readonly formBuilder =
    inject(FormBuilder);

  readonly projects = signal<Project[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  readonly projectStatuses: ProjectStatus[] = [
    'Not Started',
    'In Progress',
    'On Hold',
    'Completed',
    'Cancelled'
  ];

  readonly projectForm =
    this.formBuilder.nonNullable.group({
      projectCode: [
        '',
        [
          Validators.required,
          Validators.maxLength(20)
        ]
      ],
      projectName: [
        '',
        [
          Validators.required,
          Validators.maxLength(150)
        ]
      ],
      clientName: [
        '',
        [
          Validators.required,
          Validators.maxLength(150)
        ]
      ],
      startDate: [
        '',
        [
          Validators.required
        ]
      ],
      deadline: [''],
      status: [
        'Not Started' as ProjectStatus,
        [
          Validators.required
        ]
      ],
      description: [
        '',
        [
          Validators.maxLength(500)
        ]
      ],
      isActive: [true]
    });

  ngOnInit(): void {
    this.loadProjects();
  }

  loadProjects(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.projectService
      .getProjects()
      .subscribe({
        next: (projects) => {
          this.projects.set(projects);
          this.loading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          console.error(
            'Project loading error:',
            error
          );

          this.errorMessage.set(
            'Unable to load projects.'
          );

          this.loading.set(false);
        }
      });
  }

  saveProject(): void {
    this.successMessage.set('');
    this.errorMessage.set('');

    if (this.projectForm.invalid) {
      this.projectForm.markAllAsTouched();
      return;
    }

    const startDate =
      this.projectForm.controls.startDate.value;

    const deadline =
      this.projectForm.controls.deadline.value;

    if (
      deadline &&
      new Date(deadline) < new Date(startDate)
    ) {
      this.errorMessage.set(
        'Deadline cannot be earlier than the start date.'
      );

      return;
    }

    this.saving.set(true);

    const description =
      this.projectForm.controls.description
        .value.trim();

    const request: ProjectRequest = {
      projectCode:
        this.projectForm.controls
          .projectCode.value.trim(),

      projectName:
        this.projectForm.controls
          .projectName.value.trim(),

      clientName:
        this.projectForm.controls
          .clientName.value.trim(),

      startDate,

      deadline:
        deadline.length > 0
          ? deadline
          : null,

      status:
        this.projectForm.controls.status.value,

      description:
        description.length > 0
          ? description
          : null,

      isActive:
        this.projectForm.controls
          .isActive.value
    };

    const id = this.editingId();

    if (id === null) {
      this.createProject(request);
      return;
    }

    this.updateProject(id, request);
  }

  private createProject(
    request: ProjectRequest
  ): void {
    this.projectService
      .createProject(request)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Project added successfully.'
          );

          this.saving.set(false);
          this.resetForm();
          this.loadProjects();
        },
        error: (error: HttpErrorResponse) => {
          this.handleSaveError(error);
        }
      });
  }

  private updateProject(
    id: number,
    request: ProjectRequest
  ): void {
    this.projectService
      .updateProject(id, request)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Project updated successfully.'
          );

          this.saving.set(false);
          this.resetForm();
          this.loadProjects();
        },
        error: (error: HttpErrorResponse) => {
          this.handleSaveError(error);
        }
      });
  }

  editProject(project: Project): void {
    this.editingId.set(project.id);
    this.successMessage.set('');
    this.errorMessage.set('');

    this.projectForm.patchValue({
      projectCode: project.projectCode,
      projectName: project.projectName,
      clientName: project.clientName,
      startDate: this.formatDate(project.startDate),
      deadline: project.deadline
        ? this.formatDate(project.deadline)
        : '',
      status: project.status,
      description: project.description ?? '',
      isActive: project.isActive
    });

    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  }

  deleteProject(project: Project): void {
    const shouldDelete = window.confirm(
      `Delete project "${project.projectName}"?`
    );

    if (!shouldDelete) {
      return;
    }

    this.successMessage.set('');
    this.errorMessage.set('');

    this.projectService
      .deleteProject(project.id)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Project deleted successfully.'
          );

          if (this.editingId() === project.id) {
            this.resetForm();
          }

          this.loadProjects();
        },
        error: (error: HttpErrorResponse) => {
          console.error(
            'Project deletion error:',
            error
          );

          this.errorMessage.set(
            error.error?.message ??
              'Unable to delete project.'
          );
        }
      });
  }

  resetForm(): void {
    this.editingId.set(null);

    this.projectForm.reset({
      projectCode: '',
      projectName: '',
      clientName: '',
      startDate: '',
      deadline: '',
      status: 'Not Started',
      description: '',
      isActive: true
    });
  }

  isInvalid(
    controlName:
      | 'projectCode'
      | 'projectName'
      | 'clientName'
      | 'startDate'
      | 'status'
      | 'description'
  ): boolean {
    const control =
      this.projectForm.controls[controlName];

    return (
      control.invalid &&
      (control.touched || control.dirty)
    );
  }

  private formatDate(date: string): string {
    return date.substring(0, 10);
  }

  private handleSaveError(
    error: HttpErrorResponse
  ): void {
    console.error(
      'Project saving error:',
      error
    );

    this.errorMessage.set(
      error.error?.message ??
        'Unable to save project.'
    );

    this.saving.set(false);
  }
}