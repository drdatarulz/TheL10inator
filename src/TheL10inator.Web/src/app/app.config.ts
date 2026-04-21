import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { environment } from '../environments/environment';
import { routes } from './app.routes';
import { AuthService } from './auth/auth.service';
import { DevBypassAuthService } from './auth/dev-bypass-auth.service';
import { MsalAuthService } from './auth/msal-auth.service';
import { authInterceptor } from './auth/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    // Compile-time selection: dev-bypass in development builds (where useDevBypass=true),
    // MSAL-backed service in production. Only one implementation is kept by the bundler.
    environment.useDevBypass
      ? { provide: AuthService, useExisting: DevBypassAuthService }
      : { provide: AuthService, useExisting: MsalAuthService },
  ],
};
