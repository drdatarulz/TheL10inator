import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { AuthService } from '../auth/auth.service';

interface ShellLink {
  readonly label: string;
  readonly milestone: string;
  readonly adminOnly?: boolean;
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, AsyncPipe],
  templateUrl: './app-shell.component.html',
  styleUrls: ['./app-shell.component.scss'],
})
export class AppShellComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly currentUser$ = this.auth.currentUser$;

  protected readonly memberLinks: readonly ShellLink[] = [
    { label: 'V/TO', milestone: 'M2' },
    { label: 'Accountability Chart', milestone: 'M3' },
    { label: 'Scorecard', milestone: 'M4' },
    { label: 'Rocks', milestone: 'M5' },
    { label: 'Issues', milestone: 'M6' },
    { label: 'To-Dos', milestone: 'M6' },
    { label: 'People Analyzer', milestone: 'M9' },
    { label: 'Meetings', milestone: 'M7' },
  ];

  protected readonly adminLinks: readonly ShellLink[] = [
    { label: 'Team Settings', milestone: 'M1', adminOnly: true },
    { label: 'Audit Log', milestone: 'M1', adminOnly: true },
  ];

  async signOut(): Promise<void> {
    await this.auth.logout();
    await this.router.navigate(['/login']);
  }
}
