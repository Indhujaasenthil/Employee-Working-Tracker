import {
  Component,
  inject,
  signal
} from '@angular/core';

import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators
} from '@angular/forms';

import {
  HttpErrorResponse
} from '@angular/common/http';

import {
  AuthService
} from '../../services/auth.service';

import {
  ChangePasswordRequest
} from '../../models/change-password';

const matchingPasswordsValidator:
  ValidatorFn = (
    control: AbstractControl
  ): ValidationErrors | null => {
    const newPassword =
      control.get('newPassword')?.value;

    const confirmPassword =
      control.get('confirmPassword')?.value;

    if (
      !newPassword ||
      !confirmPassword
    ) {
      return null;
    }

    return newPassword === confirmPassword
      ? null
      : { passwordMismatch: true };
  };

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [
    ReactiveFormsModule
  ],
  templateUrl: './change-password.html',
  styleUrl: './change-password.scss'
})
export class ChangePasswordComponent {
  private readonly formBuilder =
    inject(FormBuilder);

  private readonly authService =
    inject(AuthService);

  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  readonly passwordForm =
    this.formBuilder.nonNullable.group(
      {
        currentPassword: [
          '',
          [
            Validators.required
          ]
        ],

        newPassword: [
          '',
          [
            Validators.required,
            Validators.minLength(6),
            Validators.maxLength(100)
          ]
        ],

        confirmPassword: [
          '',
          [
            Validators.required
          ]
        ]
      },
      {
        validators:
          matchingPasswordsValidator
      }
    );

  changePassword(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    const request: ChangePasswordRequest = {
      currentPassword:
        this.passwordForm.controls
          .currentPassword.value,

      newPassword:
        this.passwordForm.controls
          .newPassword.value,

      confirmPassword:
        this.passwordForm.controls
          .confirmPassword.value
    };

    this.authService
      .changePassword(request)
      .subscribe({
        next: response => {
          this.successMessage.set(
            response.message
          );

          this.saving.set(false);

          this.passwordForm.reset({
            currentPassword: '',
            newPassword: '',
            confirmPassword: ''
          });
        },

        error: (error: HttpErrorResponse) => {
          console.error(
            'Change-password error:',
            error
          );

          this.errorMessage.set(
            error.error?.message ??
              'Unable to change password.'
          );

          this.saving.set(false);
        }
      });
  }

  resetForm(): void {
    this.passwordForm.reset({
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    });

    this.errorMessage.set('');
    this.successMessage.set('');
  }

  isInvalid(
    controlName:
      | 'currentPassword'
      | 'newPassword'
      | 'confirmPassword'
  ): boolean {
    const control =
      this.passwordForm.controls[
        controlName
      ];

    return control.invalid &&
      (control.touched || control.dirty);
  }

  passwordsDoNotMatch(): boolean {
    const confirmPassword =
      this.passwordForm.controls
        .confirmPassword;

    return (
      this.passwordForm.hasError(
        'passwordMismatch'
      ) &&
      (
        confirmPassword.touched ||
        confirmPassword.dirty
      )
    );
  }
}
