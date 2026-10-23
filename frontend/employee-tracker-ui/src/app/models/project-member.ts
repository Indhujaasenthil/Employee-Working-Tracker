export interface ProjectMember {
  id: number;
  projectId: number;
  employeeId: number;
  projectRole: string;
  assignedDate: string;
  isActive: boolean;

  projectCode: string;
  projectName: string;

  employeeCode: string;
  employeeName: string;

  department: string;
  designation: string;
}

export interface ProjectMemberRequest {
  projectId: number;
  employeeId: number;
  projectRole: string;
  assignedDate: string;
  isActive: boolean;
}