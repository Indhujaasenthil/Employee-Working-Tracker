import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  EmployeeProfile
} from '../models/employee-profile';

@Injectable({
  providedIn: 'root'
})
export class EmployeeProfileService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5027/api/employee-profile';

  getProfile(): Observable<EmployeeProfile> {
    return this.http.get<EmployeeProfile>(
      this.apiUrl
    );
  }
}