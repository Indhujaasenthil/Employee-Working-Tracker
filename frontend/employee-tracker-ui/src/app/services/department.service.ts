import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  Department,
  DepartmentRequest
} from '../models/department';

@Injectable({
  providedIn: 'root'
})
export class DepartmentService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5027/api/departments';

  getDepartments(): Observable<Department[]> {
    return this.http.get<Department[]>(this.apiUrl);
  }

  getDepartment(id: number): Observable<Department> {
    return this.http.get<Department>(
      `${this.apiUrl}/${id}`
    );
  }

  createDepartment(
    department: DepartmentRequest
  ): Observable<Department> {
    return this.http.post<Department>(
      this.apiUrl,
      department
    );
  }

  updateDepartment(
    id: number,
    department: DepartmentRequest
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      department
    );
  }

  deleteDepartment(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}