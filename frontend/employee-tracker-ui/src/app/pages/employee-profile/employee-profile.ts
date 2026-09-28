import {
  Component,
  inject,
  OnInit,
  signal
} from '@angular/core';

import {
  DatePipe
} from '@angular/common';

import {
  HttpErrorResponse
} from '@angular/common/http';

import {
  EmployeeProfile
} from '../../models/employee-profile';

import {
  EmployeeProfileService
} from '../../services/employee-profile.service';

@Component({
  selector: 'app-employee-profile',
  standalone: true,
  imports: [
    DatePipe
  ],
  templateUrl: './employee-profile.html',
  styleUrl: './employee-profile.scss'
})
export class EmployeeProfileComponent
  implements OnInit {

  private readonly employeeProfileService =
    inject(EmployeeProfileService);

  readonly profile =
    signal<EmployeeProfile | null>(null);

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.employeeProfileService
      .getProfile()
      .subscribe({
        next: (
          profile: EmployeeProfile
        ) => {
          this.profile.set(profile);
          this.loading.set(false);
        },

        error: (
          error: HttpErrorResponse
        ) => {
          console.error(
            'Employee profile loading error:',
            error
          );

          this.profile.set(null);

          this.errorMessage.set(
            error.error?.message ??
              'Unable to load your profile.'
          );

          this.loading.set(false);
        }
      });
  }
}