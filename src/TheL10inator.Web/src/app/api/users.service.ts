import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { UserMeResponse } from './schema';

/**
 * Hand-written client for the <c>/api/users</c> resource. Following A-3, we keep the
 * service hand-rolled and only generate DTO types via <c>npm run generate:api-types</c>.
 */
@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl || '';

  getMe(): Observable<UserMeResponse> {
    return this.http.get<UserMeResponse>(`${this.baseUrl}/api/users/me`);
  }
}
