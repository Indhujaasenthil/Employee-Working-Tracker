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
  HttpErrorResponse
} from '@angular/common/http';

import {
  forkJoin
} from 'rxjs';

import {
  Employee,
  EmployeeRequest
} from '../../models/employee';

import {
  Department
} from '../../models/department';

import {
  Designation
} from '../../models/designation';

import {
  EmployeeService
} from '../../services/employee.service';

import {
  DepartmentService
} from '../../services/department.service';

import {
  DesignationService
} from '../../services/designation.service';

import {
  AuthService
} from '../../services/auth.service';

@Component({
  selector: 'app-employee',
  standalone: true,

  imports: [
    ReactiveFormsModule
  ],

  templateUrl: './employee.html',
  styleUrl: './employee.scss'
})
export class EmployeeComponent
  implements OnInit {

  private readonly employeeService =
    inject(EmployeeService);

  private readonly departmentService =
    inject(DepartmentService);

  private readonly designationService =
    inject(DesignationService);

  private readonly authService =
    inject(AuthService);

  private readonly formBuilder =
    inject(FormBuilder);

  // =====================================================
  // ROLE PERMISSIONS
  // =====================================================

  readonly isAdministrator =
    this.authService.hasRole('Administrator');

  readonly isManager =
    this.authService.hasRole('Manager');

  // =====================================================
  // SIGNALS
  // =====================================================

  readonly employees =
    signal<Employee[]>([]);

  readonly departments =
    signal<Department[]>([]);

  readonly designations =
    signal<Designation[]>([]);

  readonly loading =
    signal(false);

  readonly saving =
    signal(false);

  readonly editingId =
    signal<number | null>(null);

  readonly errorMessage =
    signal('');

  readonly successMessage =
    signal('');

  // =====================================================
  // FORM
  // =====================================================

  readonly employeeForm =
    this.formBuilder.group({

      employeeCode: [
        '',
        [
          Validators.required,
          Validators.maxLength(20)
        ]
      ],

      fullName: [
        '',
        [
          Validators.required,
          Validators.maxLength(100)
        ]
      ],

      email: [
        '',
        [
          Validators.required,
          Validators.email,
          Validators.maxLength(150)
        ]
      ],

      department: [
        '',
        [
          Validators.required
        ]
      ],

      designation: [
        '',
        [
          Validators.required
        ]
      ],

      managerId: [
        null as number | null
      ],

      isActive: [
        true
      ]
    });

  // =====================================================
  // INIT
  // =====================================================

  ngOnInit(): void {

    /*
     * Administrator needs departments/designations
     * because Administrator can add/edit employees.
     *
     * Manager only needs the employee list because
     * Manager has read-only access to this page.
     */
    if (this.isAdministrator) {
      this.loadMasterData();
    }

    this.loadEmployees();
  }

  // =====================================================
  // MASTER DATA
  // =====================================================

  loadMasterData(): void {

    if (!this.isAdministrator) {
      return;
    }

    forkJoin({

      departments:
        this.departmentService
          .getDepartments(),

      designations:
        this.designationService
          .getDesignations()

    })
      .subscribe({

        next: ({
          departments,
          designations
        }) => {

          this.departments.set(
            departments.filter(
              department =>
                department.isActive
            )
          );

          this.designations.set(
            designations.filter(
              designation =>
                designation.isActive
            )
          );
        },

        error: (
          error: HttpErrorResponse
        ) => {

          console.error(
            'Master data loading error:',
            error
          );

          this.errorMessage.set(
            error.error?.message ??
              'Unable to load departments or designations.'
          );
        }
      });
  }

  // =====================================================
  // LOAD EMPLOYEES
  // =====================================================

  loadEmployees(): void {

    this.loading.set(true);

    this.errorMessage.set('');

    this.employeeService
      .getEmployees()
      .subscribe({

        next: employees => {

          this.employees.set(
            employees
          );

          this.loading.set(false);
        },

        error: (
          error: HttpErrorResponse
        ) => {

          console.error(
            'Employee loading error:',
            error
          );

          this.errorMessage.set(
            error.error?.message ??
              'Unable to load employees.'
          );

          this.loading.set(false);
        }
      });
  }

  // =====================================================
  // SAVE
  // =====================================================

  saveEmployee(): void {

    this.successMessage.set('');
    this.errorMessage.set('');

    // Only Administrator can create/update employees.
    if (!this.isAdministrator) {

      this.errorMessage.set(
        'Only an Administrator can add or update employees.'
      );

      return;
    }

    if (this.employeeForm.invalid) {

      this.employeeForm
        .markAllAsTouched();

      return;
    }

    const formValue =
      this.employeeForm.getRawValue();

    const request: EmployeeRequest = {

      employeeCode:
        formValue.employeeCode
          ?.trim() ?? '',

      fullName:
        formValue.fullName
          ?.trim() ?? '',

      email:
        formValue.email
          ?.trim() ?? '',

      department:
        formValue.department ?? '',

      designation:
        formValue.designation ?? '',

      managerId:
        formValue.managerId ?? null,

      isActive:
        formValue.isActive ?? true
    };

    const id =
      this.editingId();

    // Prevent employee from selecting
    // themselves as manager.
    if (
      id !== null &&
      request.managerId === id
    ) {

      this.errorMessage.set(
        'An employee cannot be their own manager.'
      );

      return;
    }

    this.saving.set(true);

    if (id === null) {

      this.createEmployee(
        request
      );

      return;
    }

    this.updateEmployee(
      id,
      request
    );
  }

  // =====================================================
  // CREATE
  // =====================================================

  private createEmployee(
    request: EmployeeRequest
  ): void {

    if (!this.isAdministrator) {

      this.saving.set(false);

      this.errorMessage.set(
        'Only an Administrator can add employees.'
      );

      return;
    }

    this.employeeService
      .createEmployee(request)
      .subscribe({

        next: () => {

          this.successMessage.set(
            'Employee added successfully.'
          );

          this.saving.set(false);

          this.resetForm();

          this.loadEmployees();
        },

        error: (
          error: HttpErrorResponse
        ) => {

          this.handleSaveError(
            error
          );
        }
      });
  }

  // =====================================================
  // UPDATE
  // =====================================================

  private updateEmployee(
    id: number,
    request: EmployeeRequest
  ): void {

    if (!this.isAdministrator) {

      this.saving.set(false);

      this.errorMessage.set(
        'Only an Administrator can update employees.'
      );

      return;
    }

    this.employeeService
      .updateEmployee(
        id,
        request
      )
      .subscribe({

        next: () => {

          this.successMessage.set(
            'Employee updated successfully.'
          );

          this.saving.set(false);

          this.resetForm();

          this.loadEmployees();
        },

        error: (
          error: HttpErrorResponse
        ) => {

          this.handleSaveError(
            error
          );
        }
      });
  }

  // =====================================================
  // EDIT
  // =====================================================

  editEmployee(
    employee: Employee
  ): void {

    if (!this.isAdministrator) {

      this.errorMessage.set(
        'Only an Administrator can edit employees.'
      );

      return;
    }

    this.editingId.set(
      employee.id
    );

    this.successMessage.set('');
    this.errorMessage.set('');

    this.employeeForm.patchValue({

      employeeCode:
        employee.employeeCode,

      fullName:
        employee.fullName,

      email:
        employee.email,

      department:
        employee.department,

      designation:
        employee.designation,

      managerId:
        employee.managerId,

      isActive:
        employee.isActive
    });

    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  }

  // =====================================================
  // DELETE
  // =====================================================

  deleteEmployee(
    employee: Employee
  ): void {

    if (!this.isAdministrator) {

      this.errorMessage.set(
        'Only an Administrator can delete employees.'
      );

      return;
    }

    const shouldDelete =
      window.confirm(
        `Delete employee "${employee.fullName}"?`
      );

    if (!shouldDelete) {
      return;
    }

    this.successMessage.set('');
    this.errorMessage.set('');

    this.employeeService
      .deleteEmployee(
        employee.id
      )
      .subscribe({

        next: () => {

          this.successMessage.set(
            'Employee deleted successfully.'
          );

          if (
            this.editingId() ===
            employee.id
          ) {

            this.resetForm();
          }

          this.loadEmployees();
        },

        error: (
          error: HttpErrorResponse
        ) => {

          console.error(
            'Employee deletion error:',
            error
          );

          this.errorMessage.set(
            error.error?.message ??
              'Unable to delete employee.'
          );
        }
      });
  }

  // =====================================================
  // RESET
  // =====================================================

  resetForm(): void {

    this.editingId.set(null);

    this.employeeForm.reset({

      employeeCode: '',

      fullName: '',

      email: '',

      department: '',

      designation: '',

      managerId: null,

      isActive: true
    });
  }

  // =====================================================
  // MANAGER OPTIONS
  // =====================================================

  getManagerOptions():
    Employee[] {

    const currentEmployeeId =
      this.editingId();

    return this.employees()
      .filter(employee =>
        employee.isActive &&
        employee.id !==
          currentEmployeeId
      );
  }

  // =====================================================
  // VALIDATION
  // =====================================================

  isInvalid(
    controlName:
      | 'employeeCode'
      | 'fullName'
      | 'email'
      | 'department'
      | 'designation'
  ): boolean {

    const control =
      this.employeeForm
        .controls[controlName];

    return (
      control.invalid &&
      (
        control.touched ||
        control.dirty
      )
    );
  }

  // =====================================================
  // ERROR
  // =====================================================

  private handleSaveError(
    error: HttpErrorResponse
  ): void {

    console.error(
      'Employee saving error:',
      error
    );

    this.errorMessage.set(
      error.error?.message ??
        'Unable to save employee.'
    );

    this.saving.set(false);
  }
}