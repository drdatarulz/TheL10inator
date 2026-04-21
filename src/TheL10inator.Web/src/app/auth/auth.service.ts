import { Observable } from 'rxjs';
import { UserMeResponse } from '../api/schema';

/**
 * Angular-side projection of the authenticated caller. Mirrors the Api's
 * {@link UserMeResponse}.
 */
export type CurrentUser = UserMeResponse;

/**
 * Contract implemented by both the MSAL and dev-bypass auth services. Components depend
 * on the token, never on either concrete implementation, so the compile-time environment
 * switch in app.config.ts is invisible to the rest of the app.
 */
export abstract class AuthService {
  abstract currentUser$: Observable<CurrentUser | null>;
  abstract login(credentials?: LoginCredentials): Promise<void>;
  abstract logout(): Promise<void>;
  abstract getAccessToken(): Promise<string>;
  abstract setCurrentUser(user: CurrentUser | null): void;
}

/**
 * Supplied by the login screen when the dev-bypass service is active. Production MSAL
 * login ignores the field entirely.
 */
export interface LoginCredentials {
  email?: string;
}
