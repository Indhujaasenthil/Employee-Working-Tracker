import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  Project,
  ProjectRequest
} from '../models/project';

@Injectable({
  providedIn: 'root'
})
export class ProjectService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5027/api/projects';

  getProjects(): Observable<Project[]> {
    return this.http.get<Project[]>(
      this.apiUrl
    );
  }

  getProject(id: number): Observable<Project> {
    return this.http.get<Project>(
      `${this.apiUrl}/${id}`
    );
  }

  createProject(
    request: ProjectRequest
  ): Observable<Project> {
    return this.http.post<Project>(
      this.apiUrl,
      request
    );
  }

  updateProject(
    id: number,
    request: ProjectRequest
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  deleteProject(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}