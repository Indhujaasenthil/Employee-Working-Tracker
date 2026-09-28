import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  ProjectMember,
  ProjectMemberRequest
} from '../models/project-member';

@Injectable({
  providedIn: 'root'
})
export class ProjectMemberService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5027/api/projectmembers';

  getProjectMembers(
    projectId?: number
  ): Observable<ProjectMember[]> {
    let params = new HttpParams();

    if (projectId !== undefined) {
      params = params.set(
        'projectId',
        projectId.toString()
      );
    }

    return this.http.get<ProjectMember[]>(
      this.apiUrl,
      { params }
    );
  }

  getProjectMember(
    id: number
  ): Observable<ProjectMember> {
    return this.http.get<ProjectMember>(
      `${this.apiUrl}/${id}`
    );
  }

  createProjectMember(
    request: ProjectMemberRequest
  ): Observable<ProjectMember> {
    return this.http.post<ProjectMember>(
      this.apiUrl,
      request
    );
  }

  updateProjectMember(
    id: number,
    request: ProjectMemberRequest
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  deleteProjectMember(
    id: number
  ): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}