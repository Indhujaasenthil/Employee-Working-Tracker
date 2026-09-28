import {
  Component,
  inject
} from '@angular/core';

import {
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';

import {
  AuthService
} from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  readonly authService = inject(AuthService);

  logout(): void {
    this.authService.logout();
  }

  isAdministrator(): boolean {
    return this.authService.hasRole(
      'Administrator'
    );
  }

  isManager(): boolean {
    return this.authService.hasRole(
      'Manager'
    );
  }

  isEmployee(): boolean {
    return this.authService.hasRole(
      'Employee'
    );
  }

  canManageEmployees(): boolean {
    return (
      this.isAdministrator() ||
      this.isManager()
    );
  }

  canManageMasters(): boolean {
    return this.isAdministrator();
  }

  canManageProjects(): boolean {
    return (
      this.isAdministrator() ||
      this.isManager()
    );
  }

  canApproveWorkLogs(): boolean {
    return (
      this.isAdministrator() ||
      this.isManager()
    );
  }
}