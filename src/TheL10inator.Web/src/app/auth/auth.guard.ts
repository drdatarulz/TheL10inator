import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { Observable, map, take } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Guards every non-login route. When no {@link CurrentUser} has been resolved by
 * <c>GET /api/users/me</c>, the guard routes the browser to <c>/login</c>.
 */
export const authGuard: CanActivateFn = (): Observable<boolean | UrlTree> => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.currentUser$.pipe(
    take(1),
    map((user) => (user ? true : router.createUrlTree(['/login']))),
  );
};
