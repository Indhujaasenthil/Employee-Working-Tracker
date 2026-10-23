export interface Employee {
  id: number;

  employeeCode: string;
  fullName: string;
  email: string;

  department: string;
  designation: string;

  managerId: number | null;

  managerName: string | null;

  managerEmployeeCode: string | null;

  isActive: boolean;

  createdAt: string;
}

export interface EmployeeRequest {
  employeeCode: string;

  fullName: string;

  email: string;

  department: string;

  designation: string;

  managerId: number | null;

  isActive: boolean;
}