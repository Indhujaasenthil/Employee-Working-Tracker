import {
  Component,
  inject,
  OnInit,
  signal
} from '@angular/core';

import {
  HttpErrorResponse
} from '@angular/common/http';

import {
  forkJoin,
  of
} from 'rxjs';

import {
  AvailableManager,
  EmployeeManager
} from '../../models/employee-manager';

import {
  EmployeeManagerService
} from '../../services/employee-manager.service';

import {
  AuthService
} from '../../services/auth.service';

@Component({
  selector: 'app-employee-manager',
  standalone: true,
  imports: [],
  templateUrl: './employee-manager.html',
  styleUrl: './employee-manager.scss'
})
export class EmployeeManagerComponent
  implements OnInit {

  private readonly employeeManagerService =
    inject(EmployeeManagerService);

  private readonly authService =
    inject(AuthService);

  readonly employees =
    signal<EmployeeManager[]>([]);

  readonly availableManagers =
    signal<AvailableManager[]>([]);

  readonly selectedManagerIds =
    signal<Record<number, number | null>>({});

  readonly loading = signal(false);

  readonly processingId =
    signal<number | null>(null);

  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  readonly isAdministrator =
    this.authService.hasRole('Administrator');

  readonly isManager =
    this.authService.hasRole('Manager');

  ngOnInit(): void {
    this.loadPageData();
  }

  loadPageData(): void {
    this.loading.set(true);

    this.errorMessage.set('');
    this.successMessage.set('');

    /*
     * Administrator:
     * Load employees + available managers
     * because Administrator can change
     * manager assignments.
     *
     * Manager:
     * Load employee reporting structure only.
     * Manager must not receive assignment
     * controls from this component.
     */
    forkJoin({
      employees:
        this.employeeManagerService
          .getEmployees(),

      managers:
        this.isAdministrator
          ? this.employeeManagerService
              .getAvailableManagers()
          : of<AvailableManager[]>([])
    }).subscribe({
      next: ({
        employees,
        managers
      }) => {
        this.employees.set(employees);

        this.availableManagers.set(
          managers
        );

        const selections:
          Record<number, number | null> = {};

        for (const employee of employees) {
          selections[employee.id] =
            employee.managerId;
        }

        this.selectedManagerIds.set(
          selections
        );

        this.loading.set(false);
      },

      error: (
        error: HttpErrorResponse
      ) => {
        console.error(
          'Manager assignment loading error:',
          error
        );

        this.employees.set([]);
        this.availableManagers.set([]);
        this.selectedManagerIds.set({});

        this.errorMessage.set(
          error.error?.message ??
            'Unable to load manager assignments.'
        );

        this.loading.set(false);
      }
    });
  }

  getSelectedManagerId(
    employeeId: number
  ): number | null {
    return (
      this.selectedManagerIds()[employeeId] ??
      null
    );
  }

  managerChanged(
    employeeId: number,
    value: string
  ): void {

    /*
     * Extra frontend protection.
     * Only Administrator can change
     * manager selections.
     */
    if (!this.isAdministrator) {
      return;
    }

    const managerId =
      value.length > 0
        ? Number(value)
        : null;

    this.selectedManagerIds.update(
      selections => ({
        ...selections,
        [employeeId]: managerId
      })
    );
  }

  getManagersForEmployee(
    employeeId: number
  ): AvailableManager[] {
    return this.availableManagers()
      .filter(manager =>
        manager.id !== employeeId
      );
  }

  saveManager(
    employee: EmployeeManager
  ): void {

    /*
     * Important:
     * Manager cannot call this method
     * through the UI.
     */
    if (!this.isAdministrator) {
      this.errorMessage.set(
        'Only an Administrator can change manager assignments.'
      );

      return;
    }

    const managerId =
      this.getSelectedManagerId(
        employee.id
      );

    const manager =
      this.availableManagers()
        .find(item =>
          item.id === managerId
        );

    const message =
      managerId === null
        ? `Remove the manager assigned to "${employee.fullName}"?`
        : `Assign "${manager?.fullName}" as the manager of "${employee.fullName}"?`;

    const confirmed =
      window.confirm(message);

    if (!confirmed) {
      this.selectedManagerIds.update(
        selections => ({
          ...selections,
          [employee.id]:
            employee.managerId
        })
      );

      return;
    }

    this.processingId.set(
      employee.id
    );

    this.errorMessage.set('');
    this.successMessage.set('');

    this.employeeManagerService
      .assignManager(
        employee.id,
        managerId
      )
      .subscribe({
        next: response => {
          this.successMessage.set(
            response.message
          );

          this.processingId.set(null);

          this.loadPageData();
        },

        error: (
          error: HttpErrorResponse
        ) => {
          console.error(
            'Manager assignment error:',
            error
          );

          this.errorMessage.set(
            error.error?.message ??
              'Unable to update the manager assignment.'
          );

          this.selectedManagerIds.update(
            selections => ({
              ...selections,
              [employee.id]:
                employee.managerId
            })
          );

          this.processingId.set(null);
        }
      });
  }

  hasManagerChanged(
    employee: EmployeeManager
  ): boolean {

    if (!this.isAdministrator) {
      return false;
    }

    return (
      this.getSelectedManagerId(
        employee.id
      ) !== employee.managerId
    );
  }
}