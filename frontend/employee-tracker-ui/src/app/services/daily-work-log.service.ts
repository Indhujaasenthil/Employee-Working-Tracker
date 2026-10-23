
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  DailyWorkLog,
  DailyWorkLogRequest
} from '../models/daily-work-log';

import {
  environment
} from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class DailyWorkLogService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiUrl}/daily-work-logs`;

  // GET all work logs
  getWorkLogs(): Observable<DailyWorkLog[]> {
    return this.http.get<DailyWorkLog[]>(
      this.apiUrl
    );
  }

  // CREATE work log
  createWorkLog(
    request: DailyWorkLogRequest
  ): Observable<DailyWorkLog> {
    return this.http.post<DailyWorkLog>(
      this.apiUrl,
      request
    );
  }

  // UPDATE work log
  updateWorkLog(
    id: number,
    request: DailyWorkLogRequest
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  // DELETE work log
  deleteWorkLog(
    id: number
  ): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }

  // APPROVE / REJECT work log
  updateStatus(
    id: number,
    status: 'Approved' | 'Rejected'
  ): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/${id}/status`,
      { status }
    );
  }
}
