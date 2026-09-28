import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { DailyWorkLog, DailyWorkLogRequest } from '../models/daily-work-log';

@Injectable({
  providedIn: 'root',
})
export class DailyWorkLogService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = 'http://localhost:5027/api/daily-work-logs';

  getWorkLogs(): Observable<DailyWorkLog[]> {
    return this.http.get<DailyWorkLog[]>(this.apiUrl);
  }

  createWorkLog(request: DailyWorkLogRequest): Observable<DailyWorkLog> {
    return this.http.post<DailyWorkLog>(this.apiUrl, request);
  }

  updateWorkLog(id: number, request: DailyWorkLogRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, request);
  }

  deleteWorkLog(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
  updateStatus(id: number, status: 'Approved' | 'Rejected'): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/status`, { status });
  }
}
