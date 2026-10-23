import { Component, inject, OnInit, signal } from '@angular/core';

import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { HttpErrorResponse } from '@angular/common/http';

import { forkJoin, of } from 'rxjs';

import { Employee } from '../../models/employee';

import { Project } from '../../models/project';

import { ProjectMember, ProjectMemberRequest } from '../../models/project-member';

import { ProjectService } from '../../services/project.service';

import { ProjectMemberService } from '../../services/project-member.service';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-project-member',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './project-member.html',
  styleUrl: './project-member.scss',
})
export class ProjectMemberComponent implements OnInit {
  private readonly projectService = inject(ProjectService);

  private readonly projectMemberService = inject(ProjectMemberService);

  private readonly authService = inject(AuthService);

  private readonly formBuilder = inject(FormBuilder);

  readonly projects = signal<Project[]>([]);

  readonly employees = signal<Employee[]>([]);

  readonly projectMembers = signal<ProjectMember[]>([]);

  readonly loading = signal(false);

  readonly saving = signal(false);

  readonly editingId = signal<number | null>(null);

  readonly errorMessage = signal('');

  readonly successMessage = signal('');

  readonly canManage =
    this.authService.hasRole('Administrator') || this.authService.hasRole('Manager');

  readonly isEmployee = this.authService.hasRole('Employee');

  readonly projectMemberForm = this.formBuilder.nonNullable.group({
    projectId: [0, [Validators.required, Validators.min(1)]],

    employeeId: [0, [Validators.required, Validators.min(1)]],

    projectRole: ['Team Member', [Validators.required, Validators.maxLength(100)]],

    assignedDate: [this.getTodayDate(), [Validators.required]],

    isActive: [true],
  });

  ngOnInit(): void {
    this.loadPageData();
  }

  loadPageData(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    const employeesRequest = this.canManage
      ? this.projectMemberService.getAvailableEmployees()
      : of([] as Employee[]);

    forkJoin({
      projects: this.projectService.getProjects(),

      employees: employeesRequest,

      projectMembers: this.projectMemberService.getProjectMembers(),
    }).subscribe({
      next: ({ projects, employees, projectMembers }) => {
        this.projects.set(projects.filter((project) => project.isActive));

        this.employees.set(employees.filter((employee) => employee.isActive));

        this.projectMembers.set(projectMembers);

        this.loading.set(false);
      },

      error: (error: HttpErrorResponse) => {
        console.error('Project team loading error:', error);

        this.projects.set([]);
        this.employees.set([]);
        this.projectMembers.set([]);

        this.errorMessage.set(error.error?.message ?? 'Unable to load project team information.');

        this.loading.set(false);
      },
    });
  }

  saveProjectMember(): void {
    if (!this.canManage) {
      return;
    }

    this.successMessage.set('');
    this.errorMessage.set('');

    if (this.projectMemberForm.invalid) {
      this.projectMemberForm.markAllAsTouched();

      return;
    }

    this.saving.set(true);

    const request: ProjectMemberRequest = {
      projectId: this.projectMemberForm.controls.projectId.value,

      employeeId: this.projectMemberForm.controls.employeeId.value,

      projectRole: this.projectMemberForm.controls.projectRole.value.trim(),

      assignedDate: this.projectMemberForm.controls.assignedDate.value,

      isActive: this.projectMemberForm.controls.isActive.value,
    };

    const id = this.editingId();

    if (id === null) {
      this.createProjectMember(request);

      return;
    }

    this.updateProjectMember(id, request);
  }

  private createProjectMember(request: ProjectMemberRequest): void {
    this.projectMemberService.createProjectMember(request).subscribe({
      next: () => {
        this.successMessage.set('Employee assigned to project successfully.');

        this.saving.set(false);

        this.resetForm();

        this.loadPageData();
      },

      error: (error: HttpErrorResponse) => {
        this.handleSaveError(error);
      },
    });
  }

  private updateProjectMember(id: number, request: ProjectMemberRequest): void {
    this.projectMemberService.updateProjectMember(id, request).subscribe({
      next: () => {
        this.successMessage.set('Project team member updated successfully.');

        this.saving.set(false);

        this.resetForm();

        this.loadPageData();
      },

      error: (error: HttpErrorResponse) => {
        this.handleSaveError(error);
      },
    });
  }

  editProjectMember(member: ProjectMember): void {
    if (!this.canManage) {
      return;
    }

    this.editingId.set(member.id);

    this.successMessage.set('');
    this.errorMessage.set('');

    this.projectMemberForm.patchValue({
      projectId: member.projectId,

      employeeId: member.employeeId,

      projectRole: member.projectRole,

      assignedDate: this.formatDate(member.assignedDate),

      isActive: member.isActive,
    });

    window.scrollTo({
      top: 0,
      behavior: 'smooth',
    });
  }

  deleteProjectMember(member: ProjectMember): void {
    if (!this.canManage) {
      return;
    }

    const shouldDelete = window.confirm(
      `Remove "${member.employeeName}" from "${member.projectName}"?`,
    );

    if (!shouldDelete) {
      return;
    }

    this.successMessage.set('');
    this.errorMessage.set('');

    this.projectMemberService.deleteProjectMember(member.id).subscribe({
      next: () => {
        this.successMessage.set('Employee removed from project successfully.');

        if (this.editingId() === member.id) {
          this.resetForm();
        }

        this.loadPageData();
      },

      error: (error: HttpErrorResponse) => {
        console.error('Project team deletion error:', error);

        this.errorMessage.set(error.error?.message ?? 'Unable to remove employee from project.');
      },
    });
  }

  resetForm(): void {
    this.editingId.set(null);

    this.projectMemberForm.reset({
      projectId: 0,
      employeeId: 0,
      projectRole: 'Team Member',
      assignedDate: this.getTodayDate(),
      isActive: true,
    });
  }

  isInvalid(controlName: 'projectId' | 'employeeId' | 'projectRole' | 'assignedDate'): boolean {
    const control = this.projectMemberForm.controls[controlName];

    return control.invalid && (control.touched || control.dirty);
  }

  authServiceRoleIsManager(): boolean {
    return this.authService.hasRole('Manager');
  }

  private getTodayDate(): string {
    const today = new Date();

    const timezoneOffset = today.getTimezoneOffset() * 60000;

    return new Date(today.getTime() - timezoneOffset).toISOString().substring(0, 10);
  }

  private formatDate(date: string): string {
    return date.substring(0, 10);
  }

  private handleSaveError(error: HttpErrorResponse): void {
    console.error('Project team saving error:', error);

    this.errorMessage.set(error.error?.message ?? 'Unable to save project team member.');

    this.saving.set(false);
  }
}
