import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { apiErrorMessages } from '../../core/api/api-error';
import { claimReference } from '../../core/claims/claim-format';
import { ClaimantClaim } from '../../core/claims/claim.models';
import { ClaimantClaimsService } from '../../core/claims/claimant-claims.service';
import { marketLabel } from '../../core/claims/markets';
import { StatusBadge } from '../../shared/status-badge';

@Component({
  selector: 'app-my-claims',
  imports: [DatePipe, RouterLink, StatusBadge],
  templateUrl: './my-claims.html',
  styleUrl: './my-claims.css',
})
export class MyClaims implements OnInit {
  private readonly claimsApi = inject(ClaimantClaimsService);

  // Null while loading.
  protected readonly claims = signal<ClaimantClaim[] | null>(null);
  protected readonly errors = signal<string[]>([]);
  protected readonly reference = claimReference;
  protected readonly market = marketLabel;

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.claims.set(null);
    this.errors.set([]);
    this.claimsApi.list().subscribe({
      next: (claims) => this.claims.set(claims),
      error: (error: unknown) => this.errors.set(apiErrorMessages(error)),
    });
  }
}
