import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';
import { DevBypassAuthService } from './dev-bypass-auth.service';

/**
 * Attaches either <c>Authorization: Bearer &lt;token&gt;</c> (production) or
 * <c>X-Dev-User-Email: &lt;email&gt;</c> (dev bypass) to every <c>/api/</c> and
 * <c>/hubs/</c> request. Requests outside those prefixes pass through untouched so static
 * assets and third-party URLs do not receive auth headers.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isInternalRequest(req.url)) {
    return next(req);
  }

  if (environment.useDevBypass) {
    const devService = inject(DevBypassAuthService);
    const email = devService.devBypassEmail;
    if (!email) {
      return next(req);
    }
    const cloned = req.clone({
      setHeaders: { 'X-Dev-User-Email': email },
    });
    return next(cloned);
  }

  const authService = inject(AuthService);
  return from(authService.getAccessToken()).pipe(
    switchMap((token) => {
      if (!token) {
        return next(req);
      }
      const cloned = req.clone({
        setHeaders: { Authorization: `Bearer ${token}` },
      });
      return next(cloned);
    }),
  );
};

function isInternalRequest(url: string): boolean {
  try {
    const parsed = new URL(url, window.location.origin);
    return parsed.pathname.startsWith('/api/') || parsed.pathname.startsWith('/hubs/');
  } catch {
    return url.startsWith('/api/') || url.startsWith('/hubs/');
  }
}
