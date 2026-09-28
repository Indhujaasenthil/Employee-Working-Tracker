export interface WorkLogApproval {
  id: number;
  employeeId: number;
  employeeName: string;
  projectId: number;
  projectName: string;
  taskId: number;
  taskTitle: string;
  workDate: string;
  hoursWorked: number;
  progressPercentage: number;
  status: string;
  workDescription: string;
  blockers: string | null;
  createdAt: string;
}

export type ApprovalStatus =
  | 'Approved'
  | 'Rejected';