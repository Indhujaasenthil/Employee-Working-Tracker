export interface Department {
  id: number;
  departmentCode: string;
  departmentName: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface DepartmentRequest {
  departmentCode: string;
  departmentName: string;
  description: string | null;
  isActive: boolean;
}