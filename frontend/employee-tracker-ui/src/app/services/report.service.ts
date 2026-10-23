
import { inject, Injectable } from '@angular/core';

import {
  HttpClient,
  HttpParams
} from '@angular/common/http';

import { Observable } from 'rxjs';

import {
  ReportFilter,
  WorkReport
} from '../models/report';

import {
  environment
} from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiUrl}/reports`;

  getWorkSummary(
    filter: ReportFilter
  ): Observable<WorkReport> {

    let params = new HttpParams()
      .set('fromDate', filter.fromDate)
      .set('toDate', filter.toDate);

    if (filter.employeeId !== null) {
      params = params.set(
        'employeeId',
        filter.employeeId.toString()
      );
    }

    if (filter.projectId !== null) {
      params = params.set(
        'projectId',
        filter.projectId.toString()
      );
    }

    return this.http.get<WorkReport>(
      `${this.apiUrl}/work-summary`,
      { params }
    );
  }
}
