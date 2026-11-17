import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { map, Observable, tap } from 'rxjs';

interface LoginRequest {
  email: string;
  password: string;
}

interface LoginResponse {
  token: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phone: string;
  password: string;
}

export interface RegisteredUser {
  userId: number;
  fullName: string;
  email: string;
  phone: string;
  roleName: string;
}

export interface SessionClaims {
  userId: number | null;
  email: string | null;
  role: string | null;
}

export interface UserProfile {
  userId: number;
  fullName: string;
  email: string;
  phone: string;
  roleName: string;
  createdAt: string;
}

const tokenStorageKey = 'railway-access-token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  readonly token = signal<string | null>(this.readToken());
  readonly claims = computed(() => this.readClaims(this.token()));
  readonly profile = signal<UserProfile | null>(null);

  login(credentials: LoginRequest): Observable<void> {
    return this.http.post<LoginResponse>('/api/auth/login', credentials).pipe(
      tap(({ token }) => {
        sessionStorage.setItem(tokenStorageKey, token);
        this.token.set(token);
      }),
      map(() => undefined)
    );
  }

  register(details: RegisterRequest): Observable<RegisteredUser> {
    return this.http.post<RegisteredUser>('/api/auth/register', details);
  }

  getUserDetails(userId: number): Observable<UserProfile> {
    return this.http.get<UserProfile>(`/api/users/${userId}`);
  }

  logout(): void {
    sessionStorage.removeItem(tokenStorageKey);
    this.token.set(null);
    this.profile.set(null);
  }

  private readToken(): string | null {
    return typeof sessionStorage === 'undefined'
      ? null
      : sessionStorage.getItem(tokenStorageKey);
  }

  private readClaims(token: string | null): SessionClaims | null {
    if (!token) return null;

    try {
      const payload = token.split('.')[1];
      if (!payload) return null;

      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const decoded = JSON.parse(atob(normalized)) as Record<string, unknown>;
      const claim = (...keys: string[]): string | null => {
        for (const key of keys) {
          const value = decoded[key];
          if (typeof value === 'string') return value;
          if (Array.isArray(value) && typeof value[0] === 'string') return value[0];
        }
        return null;
      };

      const userId = claim('nameid', 'sub', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier');
      return {
        userId: userId && /^\d+$/.test(userId) ? Number(userId) : null,
        email: claim('email', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'),
        role: claim('role', 'roles', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role')
      };
    } catch {
      return null;
    }
  }
}
