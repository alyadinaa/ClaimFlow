import { Injectable, signal } from '@angular/core';

// One message shown at the top of the current page.
@Injectable({ providedIn: 'root' })
export class NoticeService {
  readonly message = signal<string | null>(null);

  show(message: string): void {
    this.message.set(message);
  }

  clear(): void {
    this.message.set(null);
  }
}
