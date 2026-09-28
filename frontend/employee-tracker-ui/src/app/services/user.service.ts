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
  AppUser,
  UserRequest
} from '../models/user';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly http =
    inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5027/api/users';

  getUsers(): Observable<AppUser[]> {
    return this.http.get<AppUser[]>(
      this.apiUrl
    );
  }

  getUser(
    id: number
  ): Observable<AppUser> {
    return this.http.get<AppUser>(
      `${this.apiUrl}/${id}`
    );
  }

  createUser(
    request: UserRequest
  ): Observable<AppUser> {
    return this.http.post<AppUser>(
      this.apiUrl,
      request
    );
  }

  updateUser(
    id: number,
    request: UserRequest
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  deleteUser(
    id: number
  ): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}