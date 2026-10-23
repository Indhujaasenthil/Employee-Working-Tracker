
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
import { environment } from '../../environments/environment';


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

  // =====================================
  // API URL FROM ENVIRONMENT
  // =====================================

  private readonly apiUrl =
    `${environment.apiUrl}/auth`;

  // =====================================
  // LOCAL STORAGE KEYS
  // =====================================

  private readonly tokenKey =
    'employee_tracker_token';

  private readonly userNameKey =
    'employee_tracker_user_name';

  private readonly roleKey =
    'employee_tracker_role';

  private readonly expiryKey =
    'employee_tracker_token_expiry';

  // =====================================
  // AUTHENTICATION SIGNALS
  // =====================================

  readonly authenticated =
    signal(this.hasValidToken());

  readonly currentUserName =
    signal<string | null>(
      this.hasValidToken()
        ? this.readStorage(this.userNameKey)
        : null
    );

  readonly currentUserRole =
    signal<string | null>(
      this.hasValidToken()
        ? this.readStorage(this.roleKey)
        : null
    );

  // =====================================
  // LOGIN
  // =====================================

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

  // =====================================
  // CHANGE PASSWORD
  // =====================================

  changePassword(
    request: ChangePasswordRequest
  ): Observable<ChangePasswordResponse> {

    return this.http
      .post<ChangePasswordResponse>(
        `${this.apiUrl}/change-password`,
        request
      );
  }

  // =====================================
  // LOGOUT
  // =====================================

  logout(): void {

    this.clearAuthentication();

    void this.router.navigate(['/login']);
  }

  // =====================================
  // AUTHENTICATION CHECK
  // =====================================

  isAuthenticated(): boolean {

    const valid = this.hasValidToken();

    if (!valid) {
      this.clearAuthentication();
    }

    return valid;
  }

  // =====================================
  // GET JWT TOKEN
  // =====================================

  getToken(): string | null {

    if (!this.isAuthenticated()) {
      return null;
    }

    return this.readStorage(this.tokenKey);
  }

  // =====================================
  // GET CURRENT USERNAME
  // =====================================

  getUserName(): string | null {

    if (!this.isAuthenticated()) {
      return null;
    }

    return this.readStorage(this.userNameKey);
  }

  // =====================================
  // GET CURRENT ROLE
  // =====================================

  getRole(): string | null {

    if (!this.isAuthenticated()) {
      return null;
    }

    return this.readStorage(this.roleKey);
  }

  // =====================================
  // ROLE CHECK
  // =====================================

  hasRole(role: string): boolean {

    return (
      this.getRole()?.toLowerCase() ===
      role.toLowerCase()
    );
  }

  // =====================================
  // SAVE AUTHENTICATION
  // =====================================

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

    this.authenticated.set(
      this.hasValidToken()
    );

    this.currentUserName.set(
      response.userName
    );

    this.currentUserRole.set(
      response.role
    );
  }

  // =====================================
  // CLEAR AUTHENTICATION
  // =====================================

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

  // =====================================
  // VALIDATE JWT EXPIRATION
  // =====================================

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

    if (!Number.isFinite(expiryTime)) {
      return false;
    }

    return expiryTime > Date.now();
  }

  // =====================================
  // READ LOCAL STORAGE
  // =====================================

  private readStorage(
    key: string
  ): string | null {

    if (typeof localStorage === 'undefined') {
      return null;
    }

    return localStorage.getItem(key);
  }
}
