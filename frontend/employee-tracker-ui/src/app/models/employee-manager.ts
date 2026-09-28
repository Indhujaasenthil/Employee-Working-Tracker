export interface EmployeeManager {
  id: number;
  employeeCode: string;
  fullName: string;
  email: string;
  department: string;
  designation: string;
  isActive: boolean;
  managerId: number | null;
  managerName: string | null;
}

export interface AvailableManager {
  id: number;
  employeeCode: string;
  fullName: string;
  designation: string;
}

export interface EmployeeManagerRequest {
  managerId: number | null;
}

export interface EmployeeManagerResponse {
  message: string;
  id?: number;
  managerId?: number | null;
  managerName?: string | null;
}