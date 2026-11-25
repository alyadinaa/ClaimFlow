import { Component, inject, input } from '@angular/core';
import { NavigationStart, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { AuthService } from '../core/auth/auth.service';
import { NoticeService } from '../core/notice/notice.service';

export interface NavLink {
  label: string;
  path: string;
}

// Header, navigation and page area used by both roles.
@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './shell.html',
  styleUrl: './shell.css',
})
export class Shell {
  readonly area = input.required<string>();
  readonly links = input.required<NavLink[]>();

  protected readonly auth = inject(AuthService);
  protected readonly notice = inject(NoticeService);

  constructor() {
    // A message belongs to the page it appeared on.
    inject(Router)
      .events.pipe(
        filter((event) => event instanceof NavigationStart),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.notice.clear());
  }
}
