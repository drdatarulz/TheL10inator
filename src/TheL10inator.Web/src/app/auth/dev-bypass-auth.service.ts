import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { AuthService, CurrentUser, LoginCredentials } from './auth.service';

/**
 * Dev-only auth implementation. Persists the chosen email in <c>localStorage</c> so the
 * login survives a page reload; the HTTP interceptor stamps it on every outbound call via
 * the <c>X-Dev-User-Email</c> header. The matching Api handler lives in
 * <c>DevBypassAuthHandler</c> in the .NET Api.
 */
@Injectable({ providedIn: 'root' })
export class DevBypassAuthService implements AuthService {
  private static readonly EmailStorageKey = 'theL10inator.devBypassEmail';

  private readonly subject = new BehaviorSubject<CurrentUser | null>(null);

  readonly currentUser$: Observable<CurrentUser | null> = this.subject.asObservable();

  get devBypassEmail(): string | null {
    try {
      return localStorage.getItem(DevBypassAuthService.EmailStorageKey);
    } catch {
      return null;
    }
  }

  async login(credentials?: LoginCredentials): Promise<void> {
    const email = credentials?.email?.trim();
    if (!email) {
      throw new Error('Email is required to sign in via dev bypass.');
    }
    try {
      localStorage.setItem(DevBypassAuthService.EmailStorageKey, email);
    } catch {
      // ignore — persistence is a convenience, not a requirement
    }
  }

  async logout(): Promise<void> {
    try {
      localStorage.removeItem(DevBypassAuthService.EmailStorageKey);
    } catch {
      // ignore
    }
    this.subject.next(null);
  }

  async getAccessToken(): Promise<string> {
    return '';
  }

  setCurrentUser(user: CurrentUser | null): void {
    this.subject.next(user);
  }
}
