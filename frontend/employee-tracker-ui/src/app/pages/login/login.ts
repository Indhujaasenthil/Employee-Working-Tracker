
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

import {
  ActivatedRoute,
  Router
} from '@angular/router';

import {
  finalize
} from 'rxjs';

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

  private readonly activatedRoute =
    inject(ActivatedRoute);

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
      this.navigateAfterLogin();
    }
  }

  login(): void {
    if (this.loading()) {
      return;
    }

    this.errorMessage.set('');

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    const userName =
      this.loginForm.controls.userName.value.trim();

    if (!userName) {
      this.loginForm.controls.userName.setErrors({
        required: true
      });

      this.loginForm.controls.userName.markAsTouched();
      return;
    }

    this.loading.set(true);

    const request = {
      userName,
      password:
        this.loginForm.controls.password.value
    };

    this.authService.login(request)
      .pipe(
        finalize(() => {
          this.loading.set(false);
        })
      )
      .subscribe({
        next: () => {
          this.navigateAfterLogin();
        },

        error: (error: HttpErrorResponse) => {
          if (error.status === 401) {
            this.errorMessage.set(
              'Invalid username or password.'
            );
          } else if (error.status === 0) {
            this.errorMessage.set(
              'Unable to connect to the server. ' +
              'Please check that the backend is running.'
            );
          } else {
            this.errorMessage.set(
              'Unable to sign in. Please try again.'
            );
          }
        }
      });
  }

  private navigateAfterLogin(): void {
    const returnUrl =
      this.activatedRoute.snapshot.queryParamMap
        .get('returnUrl');

    // Only allow internal application paths.
    const safeReturnUrl =
      returnUrl &&
      returnUrl.startsWith('/') &&
      !returnUrl.startsWith('//') &&
      !returnUrl.startsWith('/\\') &&
      !returnUrl.includes('\\') &&
      !returnUrl.includes('://') &&
      !returnUrl.startsWith('/login') &&
      !returnUrl.startsWith('/%')
        ? returnUrl
        : '/dashboard';

    void this.router.navigateByUrl(safeReturnUrl);
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
