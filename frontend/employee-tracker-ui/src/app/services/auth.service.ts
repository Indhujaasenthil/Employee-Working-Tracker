import {
  Injectable,
  inject,
  signal
} from '@angular/core';

import {
  HttpClient
} from '@angular/common/http';

import {
  Observable,
  tap
} from 'rxjs';

import {
  Router
} from '@angular/router';

import {
  ChangePasswordRequest,
  ChangePasswordResponse
} from '../models/change-password';

export interface LoginRequest {
  userName: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  userName: string;
  role: string;
  expiresAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly apiUrl =
    'http://localhost:5027/api/auth';

  private readonly tokenKey =
    'employee_tracker_token';

  private readonly userNameKey =
    'employee_tracker_user_name';

  private readonly roleKey =
    'employee_tracker_role';

  private readonly expiryKey =
    'employee_tracker_token_expiry';

  readonly authenticated =
    signal(this.hasValidToken());

  readonly currentUserName =
    signal(this.readStorage(this.userNameKey));

  readonly currentUserRole =
    signal(this.readStorage(this.roleKey));

  login(
    request: LoginRequest
  ): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(
        `${this.apiUrl}/login`,
        request
      )
      .pipe(
        tap(response => {
          this.saveAuthentication(response);
        })
      );
  }

  changePassword(
    request: ChangePasswordRequest
  ): Observable<ChangePasswordResponse> {
    return this.http
      .post<ChangePasswordResponse>(
        `${this.apiUrl}/change-password`,
        request
      );
  }

  logout(): void {
    this.clearAuthentication();

    void this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    const valid = this.hasValidToken();

    if (!valid) {
      this.clearAuthentication();
    }

    return valid;
  }

  getToken(): string | null {
    if (!this.hasValidToken()) {
      this.clearAuthentication();
      return null;
    }

    return this.readStorage(this.tokenKey);
  }

  getUserName(): string | null {
    return this.readStorage(
      this.userNameKey
    );
  }

  getRole(): string | null {
    return this.readStorage(this.roleKey);
  }

  hasRole(role: string): boolean {
    return (
      this.getRole()?.toLowerCase() ===
      role.toLowerCase()
    );
  }

  private saveAuthentication(
    response: LoginResponse
  ): void {
    if (typeof localStorage === 'undefined') {
      return;
    }

    localStorage.setItem(
      this.tokenKey,
      response.token
    );

    localStorage.setItem(
      this.userNameKey,
      response.userName
    );

    localStorage.setItem(
      this.roleKey,
      response.role
    );

    localStorage.setItem(
      this.expiryKey,
      response.expiresAt
    );

    this.authenticated.set(true);

    this.currentUserName.set(
      response.userName
    );

    this.currentUserRole.set(
      response.role
    );
  }

  private clearAuthentication(): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(
        this.tokenKey
      );

      localStorage.removeItem(
        this.userNameKey
      );

      localStorage.removeItem(
        this.roleKey
      );

      localStorage.removeItem(
        this.expiryKey
      );
    }

    this.authenticated.set(false);
    this.currentUserName.set(null);
    this.currentUserRole.set(null);
  }

  private hasValidToken(): boolean {
    const token =
      this.readStorage(this.tokenKey);

    const expiry =
      this.readStorage(this.expiryKey);

    if (!token || !expiry) {
      return false;
    }

    const expiryTime =
      new Date(expiry).getTime();

    if (Number.isNaN(expiryTime)) {
      return false;
    }

    return expiryTime > Date.now();
  }

  private readStorage(
    key: string
  ): string | null {
    if (typeof localStorage === 'undefined') {
      return null;
    }

    return localStorage.getItem(key);
  }
}