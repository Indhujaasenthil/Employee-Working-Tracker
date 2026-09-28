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
  Department,
  DepartmentRequest
} from '../../models/department';
import { DepartmentService } from '../../services/department.service';

@Component({
  selector: 'app-department',
  imports: [ReactiveFormsModule],
  templateUrl: './department.html',
  styleUrl: './department.scss'
})
export class DepartmentComponent implements OnInit {
  private readonly departmentService =
    inject(DepartmentService);

  private readonly formBuilder = inject(FormBuilder);

  readonly departments = signal<Department[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly successMessage = signal('');
  readonly errorMessage = signal('');

  readonly departmentForm =
    this.formBuilder.nonNullable.group({
      departmentCode: [
        '',
        [
          Validators.required,
          Validators.maxLength(20)
        ]
      ],
      departmentName: [
        '',
        [
          Validators.required,
          Validators.maxLength(100)
        ]
      ],
      description: [
        '',
        [
          Validators.maxLength(250)
        ]
      ],
      isActive: [true]
    });

  ngOnInit(): void {
    this.loadDepartments();
  }

  loadDepartments(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.departmentService.getDepartments().subscribe({
      next: (departments) => {
        this.departments.set(departments);
        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        console.error(
          'Department loading error:',
          error
        );

        this.errorMessage.set(
          'Unable to load departments.'
        );

        this.loading.set(false);
      }
    });
  }

  saveDepartment(): void {
    this.successMessage.set('');
    this.errorMessage.set('');

    if (this.departmentForm.invalid) {
      this.departmentForm.markAllAsTouched();
      return;
    }

    const formValue =
      this.departmentForm.getRawValue();

    const request: DepartmentRequest = {
      departmentCode:
        formValue.departmentCode.trim().toUpperCase(),
      departmentName:
        formValue.departmentName.trim(),
      description:
        formValue.description.trim() || null,
      isActive: formValue.isActive
    };

    this.saving.set(true);

    const id = this.editingId();

    if (id === null) {
      this.createDepartment(request);
      return;
    }

    this.updateDepartment(id, request);
  }

  private createDepartment(
    request: DepartmentRequest
  ): void {
    this.departmentService
      .createDepartment(request)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Department added successfully.'
          );

          this.saving.set(false);
          this.resetForm();
          this.loadDepartments();
        },
        error: (error: HttpErrorResponse) => {
          this.handleSaveError(error);
        }
      });
  }

  private updateDepartment(
    id: number,
    request: DepartmentRequest
  ): void {
    this.departmentService
      .updateDepartment(id, request)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Department updated successfully.'
          );

          this.saving.set(false);
          this.resetForm();
          this.loadDepartments();
        },
        error: (error: HttpErrorResponse) => {
          this.handleSaveError(error);
        }
      });
  }

  editDepartment(department: Department): void {
    this.editingId.set(department.id);
    this.successMessage.set('');
    this.errorMessage.set('');

    this.departmentForm.patchValue({
      departmentCode: department.departmentCode,
      departmentName: department.departmentName,
      description: department.description ?? '',
      isActive: department.isActive
    });

    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  }

  deleteDepartment(department: Department): void {
    const shouldDelete = window.confirm(
      `Delete department "${department.departmentName}"?`
    );

    if (!shouldDelete) {
      return;
    }

    this.successMessage.set('');
    this.errorMessage.set('');

    this.departmentService
      .deleteDepartment(department.id)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Department deleted successfully.'
          );

          if (this.editingId() === department.id) {
            this.resetForm();
          }

          this.loadDepartments();
        },
        error: (error: HttpErrorResponse) => {
          console.error(
            'Department deletion error:',
            error
          );

          this.errorMessage.set(
            error.error?.message ??
            'Unable to delete department.'
          );
        }
      });
  }

  resetForm(): void {
    this.editingId.set(null);

    this.departmentForm.reset({
      departmentCode: '',
      departmentName: '',
      description: '',
      isActive: true
    });
  }

  isInvalid(
    controlName:
      | 'departmentCode'
      | 'departmentName'
      | 'description'
  ): boolean {
    const control =
      this.departmentForm.controls[controlName];

    return control.invalid &&
      (control.touched || control.dirty);
  }

  private handleSaveError(
    error: HttpErrorResponse
  ): void {
    console.error(
      'Department saving error:',
      error
    );

    this.errorMessage.set(
      error.error?.message ??
      'Unable to save department.'
    );

    this.saving.set(false);
  }
}