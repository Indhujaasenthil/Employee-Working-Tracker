export interface Employee {
  id: number;
  employeeCode: string;
  fullName: string;
  email: string;
  department: string;
  designation: string;
  isActive: boolean;
  createdAt: string;
}

export interface EmployeeRequest {
  employeeCode: string;
  fullName: string;
  email: string;
  department: string;
  designation: string;
  isActive: boolean;
}