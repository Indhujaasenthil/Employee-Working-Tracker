export interface WorkReport {
  fromDate: string;
  toDate: string;

  employeeId: number | null;
  projectId: number | null;

  totalWorkLogs: number;
  totalHours: number;
  averageProgress: number;

  draftWorkLogs: number;
  submittedWorkLogs: number;
  approvedWorkLogs: number;
  rejectedWorkLogs: number;

  employeeSummaries: EmployeeReportSummary[];
  projectSummaries: ProjectReportSummary[];
  workLogs: ReportWorkLog[];
}

export interface EmployeeReportSummary {
  employeeId: number;
  employeeCode: string;
  employeeName: string;

  totalWorkLogs: number;
  totalHours: number;
  averageProgress: number;
  approvedWorkLogs: number;
}

export interface ProjectReportSummary {
  projectId: number;
  projectCode: string;
  projectName: string;

  totalWorkLogs: number;
  totalHours: number;
  averageProgress: number;
}

export interface ReportWorkLog {
  id: number;
  workDate: string;

  hoursWorked: number;
  progressPercentage: number;
  status: WorkLogStatus;

  workDescription: string;
  blockers: string | null;

  employeeId: number;
  employeeCode: string;
  employeeName: string;

  projectId: number;
  projectCode: string;
  projectName: string;

  workTaskId: number | null;
  taskCode: string | null;
  taskTitle: string | null;
}

export type WorkLogStatus =
  | 'Draft'
  | 'Submitted'
  | 'Approved'
  | 'Rejected';

export interface ReportFilter {
  fromDate: string;
  toDate: string;
  employeeId: number | null;
  projectId: number | null;
}