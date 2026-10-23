
import {
  Component,
  inject,
  OnInit,
  OnDestroy,
  signal
} from '@angular/core';

import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

import { HttpErrorResponse } from '@angular/common/http';
import {
  DomSanitizer,
  SafeResourceUrl
} from '@angular/platform-browser';

import {
  Project,
  ProjectRequest,
  ProjectStatus
} from '../../models/project';

import {
  ProjectFile,
  ProjectService
} from '../../services/project.service';

@Component({
  selector: 'app-project',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './project.html',
  styleUrl: './project.scss'
})
export class ProjectComponent implements OnInit, OnDestroy {
  private readonly projectService = inject(ProjectService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly sanitizer = inject(DomSanitizer);

  readonly projects = signal<Project[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  readonly selectedProject = signal<Project | null>(null);
  readonly projectFiles = signal<ProjectFile[]>([]);
  readonly filesLoading = signal(false);
  readonly uploading = signal(false);
  readonly deletingFileId = signal<number | null>(null);
  readonly downloadingFileId = signal<number | null>(null);
  readonly selectedFile = signal<File | null>(null);
  readonly fileErrorMessage = signal('');
  readonly fileSuccessMessage = signal('');

  readonly previewLoading = signal(false);
  readonly previewFileName = signal('');
  readonly previewFileType = signal('');
  readonly previewFileUrl = signal<string | null>(null);
  readonly previewPdfUrl = signal<SafeResourceUrl | null>(null);
  readonly previewText = signal('');

  private previewRequestId = 0;

  readonly maxFileSize = 10 * 1024 * 1024;

  readonly projectStatuses: ProjectStatus[] = [
    'Not Started',
    'In Progress',
    'On Hold',
    'Completed',
    'Cancelled'
  ];

  readonly projectForm = this.formBuilder.nonNullable.group({
    projectCode: [
      '',
      [Validators.required, Validators.maxLength(20)]
    ],
    projectName: [
      '',
      [Validators.required, Validators.maxLength(150)]
    ],
    clientName: [
      '',
      [Validators.required, Validators.maxLength(150)]
    ],
    startDate: ['', [Validators.required]],
    deadline: [''],
    status: [
      'Not Started' as ProjectStatus,
      [Validators.required]
    ],
    description: ['', [Validators.maxLength(500)]],
    isActive: [true]
  });

  ngOnInit(): void {
    this.loadProjects();
  }

  ngOnDestroy(): void {
    this.closeFilePreview();
  }

  loadProjects(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.projectService.getProjects().subscribe({
      next: (projects) => {
        this.projects.set(projects);
        this.loading.set(false);

        const selected = this.selectedProject();

        if (selected) {
          const updated = projects.find(
            project => project.id === selected.id
          );

          if (updated) {
            this.selectedProject.set(updated);
          } else {
            this.closeProjectFiles();
          }
        }
      },
      error: (error: HttpErrorResponse) => {
        console.error('Project loading error:', error);
        this.errorMessage.set('Unable to load projects.');
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
      this.projectForm.controls.description.value.trim();

    const request: ProjectRequest = {
      projectCode:
        this.projectForm.controls.projectCode.value.trim(),
      projectName:
        this.projectForm.controls.projectName.value.trim(),
      clientName:
        this.projectForm.controls.clientName.value.trim(),
      startDate,
      deadline: deadline.length > 0 ? deadline : null,
      status: this.projectForm.controls.status.value,
      description:
        description.length > 0 ? description : null,
      isActive:
        this.projectForm.controls.isActive.value
    };

    const id = this.editingId();

    if (id === null) {
      this.createProject(request);
    } else {
      this.updateProject(id, request);
    }
  }

  private createProject(request: ProjectRequest): void {
    this.projectService.createProject(request).subscribe({
      next: () => {
        this.successMessage.set('Project added successfully.');
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
    this.projectService.updateProject(id, request).subscribe({
      next: () => {
        this.successMessage.set('Project updated successfully.');
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

    window.scrollTo({ top: 0, behavior: 'smooth' });
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

    this.projectService.deleteProject(project.id).subscribe({
      next: () => {
        this.successMessage.set(
          'Project deleted successfully.'
        );

        if (this.editingId() === project.id) {
          this.resetForm();
        }

        if (this.selectedProject()?.id === project.id) {
          this.closeProjectFiles();
        }

        this.loadProjects();
      },
      error: (error: HttpErrorResponse) => {
        console.error('Project deletion error:', error);
        this.errorMessage.set(
          error.error?.message ?? 'Unable to delete project.'
        );
      }
    });
  }

  openProjectFiles(project: Project): void {
    this.closeFilePreview();
    this.selectedProject.set(project);
    this.selectedFile.set(null);
    this.projectFiles.set([]);
    this.fileErrorMessage.set('');
    this.fileSuccessMessage.set('');
    this.loadProjectFiles(project.id);
  }

  closeProjectFiles(): void {
    this.closeFilePreview();
    this.selectedProject.set(null);
    this.selectedFile.set(null);
    this.projectFiles.set([]);
    this.filesLoading.set(false);
    this.fileErrorMessage.set('');
    this.fileSuccessMessage.set('');
  }

  loadProjectFiles(projectId: number): void {
    this.filesLoading.set(true);
    this.fileErrorMessage.set('');

    this.projectService.getProjectFiles(projectId).subscribe({
      next: (files) => {
        if (this.selectedProject()?.id !== projectId) {
          return;
        }

        this.projectFiles.set(files);
        this.filesLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        if (this.selectedProject()?.id !== projectId) {
          return;
        }

        console.error('File loading error:', error);
        this.fileErrorMessage.set(
          error.error?.message ?? 'Unable to load project files.'
        );
        this.filesLoading.set(false);
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    this.selectedFile.set(null);
    this.fileErrorMessage.set('');
    this.fileSuccessMessage.set('');

    if (!file) {
      return;
    }

    const extension =
      file.name.split('.').pop()?.toLowerCase() ?? '';

    const allowedExtensions = [
      'pdf', 'txt', 'png', 'jpg', 'jpeg'
    ];

    if (!allowedExtensions.includes(extension)) {
      this.fileErrorMessage.set(
        'Only PDF, TXT, PNG and JPG files are allowed.'
      );
      input.value = '';
      return;
    }

    if (file.size === 0 || file.size > this.maxFileSize) {
      this.fileErrorMessage.set(
        'File must be greater than 0 bytes and no larger than 10 MB.'
      );
      input.value = '';
      return;
    }

    this.selectedFile.set(file);
  }

  uploadProjectFile(): void {
    const project = this.selectedProject();
    const file = this.selectedFile();

    if (!project || !file || this.uploading()) {
      return;
    }

    this.uploading.set(true);
    this.fileErrorMessage.set('');
    this.fileSuccessMessage.set('');

    this.projectService
      .uploadProjectFile(project.id, file)
      .subscribe({
        next: () => {
          this.uploading.set(false);
          this.selectedFile.set(null);

          this.fileSuccessMessage.set(
            'Project file uploaded successfully.'
          );

          if (this.selectedProject()?.id === project.id) {
            this.loadProjectFiles(project.id);
          }
        },
        error: (error: HttpErrorResponse) => {
          console.error('File upload error:', error);
          this.fileErrorMessage.set(
            error.error?.message ?? 'Unable to upload file.'
          );
          this.uploading.set(false);
        }
      });
  }

  downloadProjectFile(file: ProjectFile): void {
    const project = this.selectedProject();

    if (!project) {
      return;
    }

    this.downloadingFileId.set(file.id);
    this.fileErrorMessage.set('');

    this.projectService
      .downloadProjectFile(project.id, file.id)
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');

          link.href = url;
          link.download = file.originalFileName;

          document.body.appendChild(link);
          link.click();
          link.remove();

          setTimeout(() => URL.revokeObjectURL(url), 1000);
          this.downloadingFileId.set(null);
        },
        error: (error: HttpErrorResponse) => {
          console.error('File download error:', error);
          this.fileErrorMessage.set(
            'Unable to download file.'
          );
          this.downloadingFileId.set(null);
        }
      });
  }

  previewProjectFile(file: ProjectFile): void {
    const project = this.selectedProject();

    if (!project) {
      return;
    }

    this.closeFilePreview();

    const requestId = this.previewRequestId;
    this.previewLoading.set(true);
    this.fileErrorMessage.set('');

    this.projectService
      .previewProjectFile(project.id, file.id)
      .subscribe({
        next: async (blob) => {
          if (
            requestId !== this.previewRequestId ||
            this.selectedProject()?.id !== project.id
          ) {
            return;
          }

          const allowedTypes = [
            'application/pdf',
            'image/png',
            'image/jpeg',
            'text/plain'
          ];

          if (!allowedTypes.includes(blob.type)) {
            this.fileErrorMessage.set(
              'This file type cannot be previewed.'
            );
            this.previewLoading.set(false);
            return;
          }

          this.previewFileName.set(file.originalFileName);
          this.previewFileType.set(blob.type);

          if (blob.type === 'text/plain') {
            try {
              const text = await blob.text();

              if (requestId !== this.previewRequestId) {
                return;
              }

              this.previewText.set(text);
            } catch {
              this.fileErrorMessage.set(
                'Unable to read the text file.'
              );
            }

            this.previewLoading.set(false);
            return;
          }

          const url = URL.createObjectURL(blob);
          this.previewFileUrl.set(url);

          if (blob.type === 'application/pdf') {
            // Only trust a locally created blob URL
            // from our authenticated preview API.
            this.previewPdfUrl.set(
              this.sanitizer.bypassSecurityTrustResourceUrl(url)
            );
          }

          this.previewLoading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          if (requestId !== this.previewRequestId) {
            return;
          }

          console.error('File preview error:', error);
          this.fileErrorMessage.set(
            'Unable to preview file.'
          );
          this.previewLoading.set(false);
        }
      });
  }

  closeFilePreview(): void {
    this.previewRequestId++;

    const url = this.previewFileUrl();

    this.previewPdfUrl.set(null);
    this.previewFileUrl.set(null);
    this.previewFileName.set('');
    this.previewFileType.set('');
    this.previewText.set('');
    this.previewLoading.set(false);

    if (url) {
      URL.revokeObjectURL(url);
    }
  }

  deleteProjectFile(file: ProjectFile): void {
    const project = this.selectedProject();

    if (!project) {
      return;
    }

    const shouldDelete = window.confirm(
      `Delete file "${file.originalFileName}"?`
    );

    if (!shouldDelete) {
      return;
    }

    this.deletingFileId.set(file.id);
    this.fileErrorMessage.set('');
    this.fileSuccessMessage.set('');

    this.projectService
      .deleteProjectFile(project.id, file.id)
      .subscribe({
        next: () => {
          this.deletingFileId.set(null);
          this.closeFilePreview();

          this.fileSuccessMessage.set(
            'Project file deleted successfully.'
          );

          if (this.selectedProject()?.id === project.id) {
            this.loadProjectFiles(project.id);
          }
        },
        error: (error: HttpErrorResponse) => {
          console.error('File deletion error:', error);
          this.fileErrorMessage.set(
            error.error?.message ?? 'Unable to delete file.'
          );
          this.deletingFileId.set(null);
        }
      });
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) {
      return `${bytes} B`;
    }

    if (bytes < 1024 * 1024) {
      return `${(bytes / 1024).toFixed(1)} KB`;
    }

    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
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

  private handleSaveError(error: HttpErrorResponse): void {
    console.error('Project saving error:', error);
    this.errorMessage.set(
      error.error?.message ?? 'Unable to save project.'
    );
    this.saving.set(false);
  }
}
