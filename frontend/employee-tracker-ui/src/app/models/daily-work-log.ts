export type WorkLogStatus =
  | 'Draft'
  | 'Submitted'
  | 'Approved'
  | 'Rejected';

export interface DailyWorkLog {
  id: number;

  employeeId: number;
  employeeCode: string;
  employeeName: string;

  projectId: number;
  projectCode: string;
  projectName: string;

  taskId: number | null;
  taskCode: string | null;
  taskTitle: string | null;

  workDate: string;
  workDescription: string;
  hoursWorked: number;
  progressPercentage: number;
  blockers: string | null;
  status: WorkLogStatus;
  createdAt: string;
}

export interface DailyWorkLogRequest {
  employeeId: number;
  projectId: number;
  taskId: number | null;
  workDate: string;
  workDescription: string;
  hoursWorked: number;
  progressPercentage: number;
  blockers: string | null;
  status: WorkLogStatus;
}
