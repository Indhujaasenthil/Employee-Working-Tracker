
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  WorkTask,
  WorkTaskRequest
} from '../models/work-task';

import {
  environment
} from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class WorkTaskService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiUrl}/worktasks`;

  getWorkTasks(): Observable<WorkTask[]> {
    return this.http.get<WorkTask[]>(
      this.apiUrl
    );
  }

  getWorkTask(id: number): Observable<WorkTask> {
    return this.http.get<WorkTask>(
      `${this.apiUrl}/${id}`
    );
  }

  createWorkTask(
    request: WorkTaskRequest
  ): Observable<WorkTask> {
    return this.http.post<WorkTask>(
      this.apiUrl,
      request
    );
  }

  updateWorkTask(
    id: number,
    request: WorkTaskRequest
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  deleteWorkTask(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}
