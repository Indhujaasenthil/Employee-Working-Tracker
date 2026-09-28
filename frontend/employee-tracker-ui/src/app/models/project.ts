export type ProjectStatus =
  | 'Not Started'
  | 'In Progress'
  | 'On Hold'
  | 'Completed'
  | 'Cancelled';

export interface Project {
  id: number;
  projectCode: string;
  projectName: string;
  clientName: string;
  startDate: string;
  deadline: string | null;
  status: ProjectStatus;
  description: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface ProjectRequest {
  projectCode: string;
  projectName: string;
  clientName: string;
  startDate: string;
  deadline: string | null;
  status: ProjectStatus;
  description: string | null;
  isActive: boolean;
}