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
  Designation,
  DesignationRequest
} from '../../models/designation';
import { DesignationService } from '../../services/designation.service';

@Component({
  selector: 'app-designation',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './designation.html',
  styleUrl: './designation.scss'
})
export class DesignationComponent implements OnInit {
  private readonly designationService =
    inject(DesignationService);

  private readonly formBuilder = inject(FormBuilder);

  readonly designations = signal<Designation[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  readonly designationForm =
    this.formBuilder.nonNullable.group({
      designationCode: [
        '',
        [
          Validators.required,
          Validators.maxLength(20)
        ]
      ],
      designationName: [
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
    this.loadDesignations();
  }

  loadDesignations(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.designationService
      .getDesignations()
      .subscribe({
        next: (designations) => {
          this.designations.set(designations);
          this.loading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          console.error(
            'Designation loading error:',
            error
          );

          this.errorMessage.set(
            'Unable to load designations.'
          );

          this.loading.set(false);
        }
      });
  }

  saveDesignation(): void {
    this.successMessage.set('');
    this.errorMessage.set('');

    if (this.designationForm.invalid) {
      this.designationForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    const description =
      this.designationForm.controls.description
        .value.trim();

    const request: DesignationRequest = {
      designationCode:
        this.designationForm.controls
          .designationCode.value.trim(),

      designationName:
        this.designationForm.controls
          .designationName.value.trim(),

      description:
        description.length > 0
          ? description
          : null,

      isActive:
        this.designationForm.controls
          .isActive.value
    };

    const id = this.editingId();

    if (id === null) {
      this.createDesignation(request);
      return;
    }

    this.updateDesignation(id, request);
  }

  private createDesignation(
    request: DesignationRequest
  ): void {
    this.designationService
      .createDesignation(request)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Designation added successfully.'
          );

          this.saving.set(false);
          this.resetForm();
          this.loadDesignations();
        },
        error: (error: HttpErrorResponse) => {
          this.handleSaveError(error);
        }
      });
  }

  private updateDesignation(
    id: number,
    request: DesignationRequest
  ): void {
    this.designationService
      .updateDesignation(id, request)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Designation updated successfully.'
          );

          this.saving.set(false);
          this.resetForm();
          this.loadDesignations();
        },
        error: (error: HttpErrorResponse) => {
          this.handleSaveError(error);
        }
      });
  }

  editDesignation(
    designation: Designation
  ): void {
    this.editingId.set(designation.id);
    this.successMessage.set('');
    this.errorMessage.set('');

    this.designationForm.patchValue({
      designationCode:
        designation.designationCode,
      designationName:
        designation.designationName,
      description:
        designation.description ?? '',
      isActive:
        designation.isActive
    });

    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  }

  deleteDesignation(
    designation: Designation
  ): void {
    const shouldDelete = window.confirm(
      `Delete designation "${designation.designationName}"?`
    );

    if (!shouldDelete) {
      return;
    }

    this.successMessage.set('');
    this.errorMessage.set('');

    this.designationService
      .deleteDesignation(designation.id)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Designation deleted successfully.'
          );

          if (
            this.editingId() === designation.id
          ) {
            this.resetForm();
          }

          this.loadDesignations();
        },
        error: (error: HttpErrorResponse) => {
          console.error(
            'Designation deletion error:',
            error
          );

          this.errorMessage.set(
            error.error?.message ??
              'Unable to delete designation.'
          );
        }
      });
  }

  resetForm(): void {
    this.editingId.set(null);

    this.designationForm.reset({
      designationCode: '',
      designationName: '',
      description: '',
      isActive: true
    });
  }

  isInvalid(
    controlName:
      | 'designationCode'
      | 'designationName'
      | 'description'
  ): boolean {
    const control =
      this.designationForm.controls[controlName];

    return (
      control.invalid &&
      (control.touched || control.dirty)
    );
  }

  private handleSaveError(
    error: HttpErrorResponse
  ): void {
    console.error(
      'Designation saving error:',
      error
    );

    this.errorMessage.set(
      error.error?.message ??
        'Unable to save designation.'
    );

    this.saving.set(false);
  }
}