export type WorkTaskPriority =
  | 'Low'
  | 'Medium'
  | 'High'
  | 'Critical';

export type WorkTaskStatus =
  | 'Not Started'
  | 'In Progress'
  | 'On Hold'
  | 'Completed'
  | 'Cancelled';

export interface WorkTask {
  id: number;
  taskCode: string;
  taskTitle: string;
  description: string | null;

  projectId: number;
  projectCode: string;
  projectName: string;

  assignedEmployeeId: number | null;
  employeeCode: string | null;
  employeeName: string | null;

  startDate: string;
  dueDate: string | null;

  priority: WorkTaskPriority;
  status: WorkTaskStatus;
  estimatedHours: number;
  isActive: boolean;
  createdAt: string;
}

export interface WorkTaskRequest {
  taskCode: string;
  taskTitle: string;
  description: string | null;
  projectId: number;
  assignedEmployeeId: number | null;
  startDate: string;
  dueDate: string | null;
  priority: WorkTaskPriority;
  status: WorkTaskStatus;
  estimatedHours: number;
  isActive: boolean;
}