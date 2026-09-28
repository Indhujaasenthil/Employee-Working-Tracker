export type UserRole =
  | 'Administrator'
  | 'Manager'
  | 'Employee';

export interface AppUser {
  id: number;
  userName: string;
  role: UserRole;

  employeeId: number | null;
  employeeCode: string | null;
  employeeName: string | null;
}

export interface UserRequest {
  userName: string;
  password: string | null;
  role: UserRole;
  employeeId: number | null;
}