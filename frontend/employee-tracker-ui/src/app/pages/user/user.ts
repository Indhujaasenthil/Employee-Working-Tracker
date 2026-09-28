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
  HttpClient,
  HttpErrorResponse
} from '@angular/common/http';

import {
  AppUser,
  UserRequest,
  UserRole
} from '../../models/user';

import {
  UserService
} from '../../services/user.service';

interface EmployeeOption {
  id: number;
  employeeCode: string;
  fullName: string;
  isActive: boolean;
}

@Component({
  selector: 'app-user',
  standalone: true,
  imports: [
    ReactiveFormsModule
  ],
  templateUrl: './user.html',
  styleUrl: './user.scss'
})
export class UserComponent implements OnInit {
  private readonly userService =
    inject(UserService);

  private readonly http =
    inject(HttpClient);

  private readonly formBuilder =
    inject(FormBuilder);

  private readonly employeeApiUrl =
    'http://localhost:5027/api/employees';

  readonly users = signal<AppUser[]>([]);
  readonly employees =
    signal<EmployeeOption[]>([]);

  readonly loading = signal(false);
  readonly saving = signal(false);

  readonly editingId =
    signal<number | null>(null);

  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  readonly roles: UserRole[] = [
    'Administrator',
    'Manager',
    'Employee'
  ];

  readonly userForm =
    this.formBuilder.group({
      userName: [
        '',
        [
          Validators.required,
          Validators.minLength(3),
          Validators.maxLength(100)
        ]
      ],

      password: [
        '',
        [
          Validators.minLength(6),
          Validators.maxLength(100)
        ]
      ],

      role: [
        'Employee' as UserRole,
        [
          Validators.required
        ]
      ],

      employeeId: [
        null as number | null,
        [
          Validators.required
        ]
      ]
    });

  ngOnInit(): void {
    this.loadUsers();
    this.loadEmployees();

    this.userForm.controls.role
      .valueChanges
      .subscribe(role => {
        this.updateEmployeeValidation(role);
      });
  }

  loadUsers(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.userService.getUsers().subscribe({
      next: users => {
        this.users.set(users);
        this.loading.set(false);
      },

      error: (error: HttpErrorResponse) => {
        console.error(
          'User loading error:',
          error
        );

        this.users.set([]);

        this.errorMessage.set(
          error.error?.message ??
            'Unable to load users.'
        );

        this.loading.set(false);
      }
    });
  }

  loadEmployees(): void {
    this.http
      .get<EmployeeOption[]>(
        this.employeeApiUrl
      )
      .subscribe({
        next: employees => {
          const activeEmployees =
            employees.filter(employee =>
              employee.isActive
            );

          this.employees.set(
            activeEmployees
          );
        },

        error: (error: HttpErrorResponse) => {
          console.error(
            'Employee loading error:',
            error
          );

          this.employees.set([]);

          this.errorMessage.set(
            'Unable to load employees.'
          );
        }
      });
  }

  saveUser(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    this.applyPasswordValidation();

    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      return;
    }

    const role =
      this.userForm.controls.role.value;

    if (!role) {
      return;
    }

    const password =
      this.userForm.controls
        .password.value?.trim() ?? '';

    const request: UserRequest = {
      userName:
        this.userForm.controls
          .userName.value?.trim() ?? '',

      password:
        password.length > 0
          ? password
          : null,

      role,

      employeeId:
        role === 'Employee'
          ? this.userForm.controls
              .employeeId.value
          : null
    };

    this.saving.set(true);

    const id = this.editingId();

    if (id === null) {
      this.createUser(request);
      return;
    }

    this.updateUser(id, request);
  }

  editUser(user: AppUser): void {
    this.editingId.set(user.id);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.userForm.patchValue({
      userName: user.userName,
      password: '',
      role: user.role,
      employeeId: user.employeeId
    });

    this.updateEmployeeValidation(
      user.role,
      false
    );

    this.applyPasswordValidation();

    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  }

  deleteUser(user: AppUser): void {
    const confirmed = window.confirm(
      `Delete user "${user.userName}"?`
    );

    if (!confirmed) {
      return;
    }

    this.errorMessage.set('');
    this.successMessage.set('');

    this.userService
      .deleteUser(user.id)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'User deleted successfully.'
          );

          if (this.editingId() === user.id) {
            this.resetForm();
          }

          this.loadUsers();
        },

        error: (error: HttpErrorResponse) => {
          console.error(
            'User deletion error:',
            error
          );

          this.errorMessage.set(
            error.error?.message ??
              'Unable to delete user.'
          );
        }
      });
  }

  resetForm(): void {
    this.editingId.set(null);
    this.errorMessage.set('');

    this.userForm.reset({
      userName: '',
      password: '',
      role: 'Employee',
      employeeId: null
    });

    this.updateEmployeeValidation(
      'Employee',
      false
    );

    this.applyPasswordValidation();
  }

  isInvalid(
    controlName:
      | 'userName'
      | 'password'
      | 'role'
      | 'employeeId'
  ): boolean {
    const control =
      this.userForm.controls[controlName];

    return control.invalid &&
      (control.touched || control.dirty);
  }

  private createUser(
    request: UserRequest
  ): void {
    this.userService
      .createUser(request)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'User created successfully.'
          );

          this.saving.set(false);
          this.resetForm();
          this.loadUsers();
        },

        error: (error: HttpErrorResponse) => {
          this.handleSaveError(error);
        }
      });
  }

  private updateUser(
    id: number,
    request: UserRequest
  ): void {
    this.userService
      .updateUser(id, request)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'User updated successfully.'
          );

          this.saving.set(false);
          this.resetForm();
          this.loadUsers();
        },

        error: (error: HttpErrorResponse) => {
          this.handleSaveError(error);
        }
      });
  }

  private updateEmployeeValidation(
    role: UserRole | null,
    clearValue = true
  ): void {
    const employeeControl =
      this.userForm.controls.employeeId;

    if (role === 'Employee') {
      employeeControl.setValidators([
        Validators.required
      ]);
    } else {
      employeeControl.clearValidators();

      if (clearValue) {
        employeeControl.setValue(null);
      }
    }

    employeeControl.updateValueAndValidity({
      emitEvent: false
    });
  }

  private applyPasswordValidation(): void {
    const passwordControl =
      this.userForm.controls.password;

    const validators = [
      Validators.minLength(6),
      Validators.maxLength(100)
    ];

    if (this.editingId() === null) {
      passwordControl.setValidators([
        Validators.required,
        ...validators
      ]);
    } else {
      passwordControl.setValidators(
        validators
      );
    }

    passwordControl.updateValueAndValidity({
      emitEvent: false
    });
  }

  private handleSaveError(
    error: HttpErrorResponse
  ): void {
    console.error(
      'User saving error:',
      error
    );

    this.errorMessage.set(
      error.error?.message ??
        'Unable to save user.'
    );

    this.saving.set(false);
  }
}
