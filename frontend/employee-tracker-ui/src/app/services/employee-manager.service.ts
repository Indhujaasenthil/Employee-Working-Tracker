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
  AvailableManager,
  EmployeeManager,
  EmployeeManagerRequest,
  EmployeeManagerResponse
} from '../models/employee-manager';

@Injectable({
  providedIn: 'root'
})
export class EmployeeManagerService {
  private readonly http =
    inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5027/api/employee-managers';

  getEmployees():
    Observable<EmployeeManager[]> {

    return this.http.get<EmployeeManager[]>(
      this.apiUrl
    );
  }

  getAvailableManagers():
    Observable<AvailableManager[]> {

    return this.http.get<AvailableManager[]>(
      `${this.apiUrl}/available-managers`
    );
  }

  assignManager(
    employeeId: number,
    managerId: number | null
  ): Observable<EmployeeManagerResponse> {
    const request: EmployeeManagerRequest = {
      managerId
    };

    return this.http.put<EmployeeManagerResponse>(
      `${this.apiUrl}/${employeeId}`,
      request
    );
  }
}