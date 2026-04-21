import { Injectable } from '@angular/core';
import {
  PublicClientApplication,
  IPublicClientApplication,
  AccountInfo,
  InteractionRequiredAuthError,
  RedirectRequest,
  Configuration,
} from '@azure/msal-browser';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService, CurrentUser } from './auth.service';

/**
 * Production auth service — wraps {@link PublicClientApplication} from
 * <c>@azure/msal-browser</c> directly. <c>@azure/msal-angular</c> historically lags the
 * current Angular major by one release; we use the browser library directly to keep
 * the peer-dependency surface small.
 */
@Injectable({ providedIn: 'root' })
export class MsalAuthService implements AuthService {
  private readonly subject = new BehaviorSubject<CurrentUser | null>(null);
  private readonly msal: IPublicClientApplication;
  private initialized = false;

  readonly currentUser$: Observable<CurrentUser | null> = this.subject.asObservable();

  constructor() {
    const config: Configuration = {
      auth: {
        clientId: environment.msal.clientId,
        authority: environment.msal.authority,
        redirectUri: this.resolveRedirectUri(environment.msal.redirectUri),
      },
      cache: {
        cacheLocation: 'localStorage',
      },
    };
    this.msal = new PublicClientApplication(config);
  }

  async login(): Promise<void> {
    await this.ensureInitialized();
    const request: RedirectRequest = {
      scopes: environment.msal.apiScopes,
    };
    await this.msal.loginRedirect(request);
  }

  async logout(): Promise<void> {
    await this.ensureInitialized();
    this.subject.next(null);
    await this.msal.logoutRedirect();
  }

  async getAccessToken(): Promise<string> {
    await this.ensureInitialized();
    const account = this.firstAccount();
    if (!account) {
      return '';
    }
    try {
      const result = await this.msal.acquireTokenSilent({
        account,
        scopes: environment.msal.apiScopes,
      });
      return result.accessToken ?? '';
    } catch (err) {
      if (err instanceof InteractionRequiredAuthError) {
        await this.msal.acquireTokenRedirect({ scopes: environment.msal.apiScopes });
      }
      return '';
    }
  }

  setCurrentUser(user: CurrentUser | null): void {
    this.subject.next(user);
  }

  private firstAccount(): AccountInfo | null {
    const accounts = this.msal.getAllAccounts();
    return accounts.length > 0 ? accounts[0] : null;
  }

  private async ensureInitialized(): Promise<void> {
    if (this.initialized) {
      return;
    }
    await this.msal.initialize();
    await this.msal.handleRedirectPromise();
    this.initialized = true;
  }

  private resolveRedirectUri(configured: string): string {
    if (!configured) {
      return window.location.origin + '/login';
    }
    if (configured.startsWith('http')) {
      return configured;
    }
    return window.location.origin + (configured.startsWith('/') ? configured : '/' + configured);
  }
}
