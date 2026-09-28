import {
  inject,
  Injectable
} from '@angular/core';

import {
  HttpClient
} from '@angular/common/http';

import {
  Observable
} from 'rxjs';

import {
  TaskStatus,
  TaskStatusRequest,
  TaskStatusResponse
} from '../models/task-status';

@Injectable({
  providedIn: 'root'
})
export class TaskStatusService {
  private readonly http =
    inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5027/api/tasks';

  updateStatus(
    taskId: number,
    status: TaskStatus
  ): Observable<TaskStatusResponse> {
    const request: TaskStatusRequest = {
      status
    };

    return this.http.patch<TaskStatusResponse>(
      `${this.apiUrl}/${taskId}/status`,
      request
    );
  }
}