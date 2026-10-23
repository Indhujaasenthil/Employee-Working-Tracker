
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  Project,
  ProjectRequest
} from '../models/project';

import {
  environment
} from '../../environments/environment';

export interface ProjectFile {
  id: number;
  projectId: number;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class ProjectService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiUrl}/projects`;

  getProjects(): Observable<Project[]> {
    return this.http.get<Project[]>(this.apiUrl);
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

  // =====================================
  // PROJECT FILE MANAGEMENT
  // =====================================

  getProjectFiles(
    projectId: number
  ): Observable<ProjectFile[]> {
    return this.http.get<ProjectFile[]>(
      `${this.apiUrl}/${projectId}/files`
    );
  }

  uploadProjectFile(
    projectId: number,
    file: File
  ): Observable<unknown> {
    const formData = new FormData();

    formData.append('file', file, file.name);

    return this.http.post(
      `${this.apiUrl}/${projectId}/files`,
      formData
    );
  }

  downloadProjectFile(
    projectId: number,
    fileId: number
  ): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/${projectId}/files/${fileId}/download`,
      {
        responseType: 'blob'
      }
    );
  }

  previewProjectFile(
    projectId: number,
    fileId: number
  ): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/${projectId}/files/${fileId}/preview`,
      {
        responseType: 'blob'
      }
    );
  }

  deleteProjectFile(
    projectId: number,
    fileId: number
  ): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${projectId}/files/${fileId}`
    );
  }
}
