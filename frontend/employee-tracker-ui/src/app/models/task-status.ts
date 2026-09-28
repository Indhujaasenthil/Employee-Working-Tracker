export type TaskStatus =
  | 'Not Started'
  | 'In Progress'
  | 'On Hold'
  | 'Completed';

export interface TaskStatusRequest {
  status: TaskStatus;
}

export interface TaskStatusResponse {
  message: string;
  id: number;
  status: TaskStatus;
}