export interface ProjectMember {
  id: number;

  projectId: number;
  projectCode: string;
  projectName: string;

  employeeId: number;
  employeeCode: string;
  employeeName: string;

  department: string;
  designation: string;

  projectRole: string;
  assignedDate: string;
  isActive: boolean;
}

export interface ProjectMemberRequest {
  projectId: number;
  employeeId: number;
  projectRole: string;
  assignedDate: string;
  isActive: boolean;
}