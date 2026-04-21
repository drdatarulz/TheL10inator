import { AsyncPipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { UsersService } from '../api/users.service';
import { AuthService, CurrentUser } from '../auth/auth.service';

interface SummaryCard {
  readonly title: string;
  readonly emptyCopy: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [AsyncPipe],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
  private readonly users = inject(UsersService);
  private readonly auth = inject(AuthService);

  private readonly meSubject = new BehaviorSubject<CurrentUser | null>(null);
  protected readonly me$ = this.meSubject.asObservable();

  protected readonly cards: readonly SummaryCard[] = [
    { title: 'Rocks on track', emptyCopy: "No rocks yet — you'll see them here once the team creates quarterly rocks." },
    { title: 'Scorecard', emptyCopy: 'No scorecard metrics yet — add weekly measurables to watch trends here.' },
    { title: 'Open issues', emptyCopy: 'No issues yet — add them during your next L10 meeting.' },
    { title: 'My to-dos', emptyCopy: "No to-dos yet — you'll see seven-day commitments here." },
  ];

  ngOnInit(): void {
    this.users.getMe().subscribe({
      next: (me) => {
        this.meSubject.next(me);
        this.auth.setCurrentUser(me);
      },
      error: () => {
        this.meSubject.next(null);
      },
    });
  }
}
