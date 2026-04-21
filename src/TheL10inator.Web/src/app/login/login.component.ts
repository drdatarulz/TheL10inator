import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { UsersService } from '../api/users.service';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly users = inject(UsersService);
  private readonly router = inject(Router);

  protected readonly useDevBypass = environment.useDevBypass;
  protected readonly email = signal('');
  protected readonly errorBanner = signal<string | null>(null);
  protected readonly submitting = signal(false);

  async onSubmit(): Promise<void> {
    this.errorBanner.set(null);
    this.submitting.set(true);
    try {
      await this.auth.login({ email: this.email() });
      const me = await new Promise<import('../api/schema').UserMeResponse>((resolve, reject) => {
        const sub = this.users.getMe().subscribe({
          next: (value) => {
            resolve(value);
            sub.unsubscribe();
          },
          error: (err) => {
            reject(err);
            sub.unsubscribe();
          },
        });
      });
      this.auth.setCurrentUser(me);
      await this.router.navigate(['/dashboard']);
    } catch (err) {
      this.auth.setCurrentUser(null);
      this.errorBanner.set(
        "Your account doesn't have access to this team. Ask an admin to invite you.",
      );
    } finally {
      this.submitting.set(false);
    }
  }
}
