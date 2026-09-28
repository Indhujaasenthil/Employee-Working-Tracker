import {
  Component,
  inject,
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
import { Router } from '@angular/router';

import {
  AuthService
} from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent {
  private readonly formBuilder =
    inject(FormBuilder);

  private readonly authService =
    inject(AuthService);

  private readonly router =
    inject(Router);

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  readonly loginForm =
    this.formBuilder.nonNullable.group({
      userName: [
        '',
        [
          Validators.required,
          Validators.maxLength(100)
        ]
      ],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(6)
        ]
      ]
    });

  constructor() {
    if (this.authService.isAuthenticated()) {
      void this.router.navigate(['/dashboard']);
    }
  }

  login(): void {
    this.errorMessage.set('');

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);

    const request = {
      userName:
        this.loginForm.controls.userName.value.trim(),

      password:
        this.loginForm.controls.password.value
    };

    this.authService.login(request).subscribe({
      next: () => {
        this.loading.set(false);

        void this.router.navigate(['/dashboard']);
      },

      error: (error: HttpErrorResponse) => {
        console.error('Login error:', error);

        this.errorMessage.set(
          error.status === 401
            ? 'Invalid username or password.'
            : error.error?.message ??
              'Unable to sign in. Check that the backend is running.'
        );

        this.loading.set(false);
      }
    });
  }

  isInvalid(
    controlName: 'userName' | 'password'
  ): boolean {
    const control =
      this.loginForm.controls[controlName];

    return control.invalid &&
      (control.touched || control.dirty);
  }
}