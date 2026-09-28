import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  Employee,
  EmployeeRequest
} from '../models/employee';

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5027/api/employees';

  getEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(this.apiUrl);
  }

  getEmployee(id: number): Observable<Employee> {
    return this.http.get<Employee>(
      `${this.apiUrl}/${id}`
    );
  }

  createEmployee(
    employee: EmployeeRequest
  ): Observable<Employee> {
    return this.http.post<Employee>(
      this.apiUrl,
      employee
    );
  }

  updateEmployee(
    id: number,
    employee: EmployeeRequest
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      employee
    );
  }

  deleteEmployee(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}