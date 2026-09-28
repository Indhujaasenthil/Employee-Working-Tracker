import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  Designation,
  DesignationRequest
} from '../models/designation';

@Injectable({
  providedIn: 'root'
})
export class DesignationService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5027/api/designations';

  getDesignations(): Observable<Designation[]> {
    return this.http.get<Designation[]>(
      this.apiUrl
    );
  }

  getDesignation(
    id: number
  ): Observable<Designation> {
    return this.http.get<Designation>(
      `${this.apiUrl}/${id}`
    );
  }

  createDesignation(
    request: DesignationRequest
  ): Observable<Designation> {
    return this.http.post<Designation>(
      this.apiUrl,
      request
    );
  }

  updateDesignation(
    id: number,
    request: DesignationRequest
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  deleteDesignation(
    id: number
  ): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}