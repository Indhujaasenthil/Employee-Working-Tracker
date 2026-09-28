import {
  Routes
} from '@angular/router';

import {
  authGuard
} from './guards/auth.guard';

import {
  roleGuard
} from './guards/role.guard';

import {
  LoginComponent
} from './pages/login/login';

import {
  DashboardComponent
} from './pages/dashboard/dashboard';

import {
  EmployeeComponent
} from './pages/employee/employee';

import {
  EmployeeManagerComponent
} from './pages/employee-manager/employee-manager';

import {
  DepartmentComponent
} from './pages/department/department';

import {
  DesignationComponent
} from './pages/designation/designation';

import {
  ProjectComponent
} from './pages/project/project';

import {
  ProjectMemberComponent
} from './pages/project-member/project-member';

import {
  WorkTaskComponent
} from './pages/work-task/work-task';

import {
  WorkLogComponent
} from './pages/work-log/work-log';

import {
  WorkLogApprovalComponent
} from './pages/work-log-approval/work-log-approval';

import {
  ReportComponent
} from './pages/report/report';

import {
  UserComponent
} from './pages/user/user';

import {
  EmployeeProfileComponent
} from './pages/employee-profile/employee-profile';

import {
  ChangePasswordComponent
} from './pages/change-password/change-password';

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

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
    title: 'Login'
  },

  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: allRoles
    },
    title: 'Dashboard'
  },

  {
    path: 'employees',
    component: EmployeeComponent,
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: managementRoles
    },
    title: 'Employees'
  },

  {
    path: 'employee-managers',
    component: EmployeeManagerComponent,
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: managementRoles
    },
    title: 'Manager Assignment'
  },

  {
    path: 'departments',
    component: DepartmentComponent,
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: administratorRoles
    },
    title: 'Departments'
  },

  {
    path: 'designations',
    component: DesignationComponent,
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: administratorRoles
    },
    title: 'Designations'
  },

  {
    path: 'projects',
    component: ProjectComponent,
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: managementRoles
    },
    title: 'Projects'
  },

  {
    path: 'project-team',
    component: ProjectMemberComponent,
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: managementRoles
    },
    title: 'Project Team'
  },

  {
    path: 'tasks',
    component: WorkTaskComponent,
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: allRoles
    },
    title: 'Tasks'
  },

  {
    path: 'work-logs',
    component: WorkLogComponent,
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: allRoles
    },
    title: 'Work Logs'
  },

  {
    path: 'work-log-approvals',
    component: WorkLogApprovalComponent,
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: managementRoles
    },
    title: 'Work Log Approvals'
  },

  {
    path: 'reports',
    component: ReportComponent,
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: allRoles
    },
    title: 'Reports'
  },

  {
    path: 'users',
    component: UserComponent,
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: administratorRoles
    },
    title: 'User Management'
  },

  {
    path: 'profile',
    component: EmployeeProfileComponent,
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: allRoles
    },
    title: 'My Profile'
  },

  {
    path: 'change-password',
    component: ChangePasswordComponent,
    canActivate: [
      authGuard,
      roleGuard
    ],
    data: {
      roles: allRoles
    },
    title: 'Change Password'
  },

  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard'
  },

  {
    path: '**',
    redirectTo: 'dashboard'
  }
];