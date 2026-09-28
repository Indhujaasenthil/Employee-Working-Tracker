import {
  inject,
  Injectable
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  ApprovalStatus,
  WorkLogApproval
} from '../models/work-log-approval.model';

@Injectable({
  providedIn: 'root'
})
export class WorkLogApprovalService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5027/api/daily-work-logs';

  getWorkLogs(): Observable<WorkLogApproval[]> {
    return this.http.get<WorkLogApproval[]>(
      this.apiUrl
    );
  }

  updateStatus(
    id: number,
    status: ApprovalStatus
  ): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/${id}/status`,
      { status }
    );
  }
}