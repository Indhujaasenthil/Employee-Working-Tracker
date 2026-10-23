
import {
  Routes
} from '@angular/router';

import {
  authGuard
} from './guards/auth.guard';

import {
  roleGuard
} from './guards/role.guard';

// ==========================================
// USER ROLES
// ==========================================

const allRoles = [
  'Administrator',
  'Manager',
  'Employee'
];

const managementRoles = [
  'Administrator',
  'Manager'
];

const administratorRoles = [
  'Administrator'
];

// ==========================================
// APPLICATION ROUTES
// ==========================================

export const routes: Routes = [

  // ========================================
  // LOGIN
  // ========================================

  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login')
        .then(m => m.LoginComponent),
    title: 'Login'
  },

  // ========================================
  // DASHBOARD
  // ========================================

  {
    path: 'dashboard',
    loadComponent: () =>
      import('./pages/dashboard/dashboard')
        .then(m => m.DashboardComponent),
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: allRoles
    },
    title: 'Dashboard'
  },

  // ========================================
  // EMPLOYEES
  // ========================================

  {
    path: 'employees',
    loadComponent: () =>
      import('./pages/employee/employee')
        .then(m => m.EmployeeComponent),
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: managementRoles
    },
    title: 'Employees'
  },

  // ========================================
  // MANAGER ASSIGNMENT
  // ========================================

  {
    path: 'employee-managers',
    loadComponent: () =>
      import('./pages/employee-manager/employee-manager')
        .then(m => m.EmployeeManagerComponent),
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: managementRoles
    },
    title: 'Manager Assignment'
  },

  // ========================================
  // DEPARTMENTS
  // ========================================

  {
    path: 'departments',
    loadComponent: () =>
      import('./pages/department/department')
        .then(m => m.DepartmentComponent),
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: administratorRoles
    },
    title: 'Departments'
  },

  // ========================================
  // DESIGNATIONS
  // ========================================

  {
    path: 'designations',
    loadComponent: () =>
      import('./pages/designation/designation')
        .then(m => m.DesignationComponent),
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: administratorRoles
    },
    title: 'Designations'
  },

  // ========================================
  // PROJECTS
  // ========================================

  {
    path: 'projects',
    loadComponent: () =>
      import('./pages/project/project')
        .then(m => m.ProjectComponent),
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: managementRoles
    },
    title: 'Projects'
  },

  // ========================================
  // PROJECT TEAM
  // ========================================

  {
    path: 'project-team',
    loadComponent: () =>
      import('./pages/project-member/project-member')
        .then(m => m.ProjectMemberComponent),
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: managementRoles
    },
    title: 'Project Team'
  },

  // ========================================
  // TASKS
  // ========================================

  {
    path: 'tasks',
    loadComponent: () =>
      import('./pages/work-task/work-task')
        .then(m => m.WorkTaskComponent),
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: allRoles
    },
    title: 'Tasks'
  },

  // ========================================
  // WORK LOGS
  // ========================================

  {
    path: 'work-logs',
    loadComponent: () =>
      import('./pages/work-log/work-log')
        .then(m => m.WorkLogComponent),
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: allRoles
    },
    title: 'Work Logs'
  },

  // ========================================
  // WORK LOG APPROVALS
  // ========================================

  {
    path: 'work-log-approvals',
    loadComponent: () =>
      import('./pages/work-log-approval/work-log-approval')
        .then(m => m.WorkLogApprovalComponent),
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: managementRoles
    },
    title: 'Work Log Approvals'
  },

  // ========================================
  // REPORTS
  // ========================================

  {
    path: 'reports',
    loadComponent: () =>
      import('./pages/report/report')
        .then(m => m.ReportComponent),
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: allRoles
    },
    title: 'Reports'
  },

  // ========================================
  // USER MANAGEMENT
  // ========================================

  {
    path: 'users',
    loadComponent: () =>
      import('./pages/user/user')
        .then(m => m.UserComponent),
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: administratorRoles
    },
    title: 'User Management'
  },

  // ========================================
  // MY PROFILE
  // ========================================

  {
    path: 'profile',
    loadComponent: () =>
      import('./pages/employee-profile/employee-profile')
        .then(m => m.EmployeeProfileComponent),
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: allRoles
    },
    title: 'My Profile'
  },

  // ========================================
  // CHANGE PASSWORD
  // ========================================

  {
    path: 'change-password',
    loadComponent: () =>
      import('./pages/change-password/change-password')
        .then(m => m.ChangePasswordComponent),
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: allRoles
    },
    title: 'Change Password'
  },

  // ========================================
  // DEFAULT ROUTE
  // ========================================

  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard'
  },

  // ========================================
  // UNKNOWN ROUTES
  // ========================================

  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
