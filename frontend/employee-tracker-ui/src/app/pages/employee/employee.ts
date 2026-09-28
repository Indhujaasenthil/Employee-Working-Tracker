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
import { forkJoin } from 'rxjs';

import {
  Employee,
  EmployeeRequest
} from '../../models/employee';
import { Department } from '../../models/department';
import { Designation } from '../../models/designation';

import { EmployeeService } from '../../services/employee.service';
import { DepartmentService } from '../../services/department.service';
import { DesignationService } from '../../services/designation.service';

@Component({
  selector: 'app-employee',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './employee.html',
  styleUrl: './employee.scss'
})
export class EmployeeComponent implements OnInit {
  private readonly employeeService =
    inject(EmployeeService);

  private readonly departmentService =
    inject(DepartmentService);

  private readonly designationService =
    inject(DesignationService);

  private readonly formBuilder =
    inject(FormBuilder);

  readonly employees = signal<Employee[]>([]);
  readonly departments = signal<Department[]>([]);
  readonly designations = signal<Designation[]>([]);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  readonly employeeForm =
    this.formBuilder.nonNullable.group({
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
      isActive: [true]
    });

  ngOnInit(): void {
    this.loadMasterData();
    this.loadEmployees();
  }

  loadMasterData(): void {
    forkJoin({
      departments:
        this.departmentService.getDepartments(),
      designations:
        this.designationService.getDesignations()
    }).subscribe({
      next: ({ departments, designations }) => {
        this.departments.set(
          departments.filter(
            department => department.isActive
          )
        );

        this.designations.set(
          designations.filter(
            designation => designation.isActive
          )
        );
      },
      error: (error: HttpErrorResponse) => {
        console.error(
          'Master data loading error:',
          error
        );

        this.errorMessage.set(
          'Unable to load departments or designations.'
        );
      }
    });
  }

  loadEmployees(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.employeeService
      .getEmployees()
      .subscribe({
        next: (employees) => {
          this.employees.set(employees);
          this.loading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          console.error(
            'Employee loading error:',
            error
          );

          this.errorMessage.set(
            'Unable to load employees.'
          );

          this.loading.set(false);
        }
      });
  }

  saveEmployee(): void {
    this.successMessage.set('');
    this.errorMessage.set('');

    if (this.employeeForm.invalid) {
      this.employeeForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    const request: EmployeeRequest = {
      employeeCode:
        this.employeeForm.controls
          .employeeCode.value.trim(),

      fullName:
        this.employeeForm.controls
          .fullName.value.trim(),

      email:
        this.employeeForm.controls
          .email.value.trim(),

      department:
        this.employeeForm.controls
          .department.value,

      designation:
        this.employeeForm.controls
          .designation.value,

      isActive:
        this.employeeForm.controls
          .isActive.value
    };

    const id = this.editingId();

    if (id === null) {
      this.createEmployee(request);
      return;
    }

    this.updateEmployee(id, request);
  }

  private createEmployee(
    request: EmployeeRequest
  ): void {
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
        error: (error: HttpErrorResponse) => {
          this.handleSaveError(error);
        }
      });
  }

  private updateEmployee(
    id: number,
    request: EmployeeRequest
  ): void {
    this.employeeService
      .updateEmployee(id, request)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Employee updated successfully.'
          );

          this.saving.set(false);
          this.resetForm();
          this.loadEmployees();
        },
        error: (error: HttpErrorResponse) => {
          this.handleSaveError(error);
        }
      });
  }

  editEmployee(employee: Employee): void {
    this.editingId.set(employee.id);
    this.successMessage.set('');
    this.errorMessage.set('');

    this.employeeForm.patchValue({
      employeeCode: employee.employeeCode,
      fullName: employee.fullName,
      email: employee.email,
      department: employee.department,
      designation: employee.designation,
      isActive: employee.isActive
    });

    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  }

  deleteEmployee(employee: Employee): void {
    const shouldDelete = window.confirm(
      `Delete employee "${employee.fullName}"?`
    );

    if (!shouldDelete) {
      return;
    }

    this.successMessage.set('');
    this.errorMessage.set('');

    this.employeeService
      .deleteEmployee(employee.id)
      .subscribe({
        next: () => {
          this.successMessage.set(
            'Employee deleted successfully.'
          );

          if (
            this.editingId() === employee.id
          ) {
            this.resetForm();
          }

          this.loadEmployees();
        },
        error: (error: HttpErrorResponse) => {
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

  resetForm(): void {
    this.editingId.set(null);

    this.employeeForm.reset({
      employeeCode: '',
      fullName: '',
      email: '',
      department: '',
      designation: '',
      isActive: true
    });
  }

  isInvalid(
    controlName:
      | 'employeeCode'
      | 'fullName'
      | 'email'
      | 'department'
      | 'designation'
  ): boolean {
    const control =
      this.employeeForm.controls[controlName];

    return (
      control.invalid &&
      (control.touched || control.dirty)
    );
  }

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