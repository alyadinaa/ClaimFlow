import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { apiErrorMessages } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { formatMoney } from '../../core/claims/claim-format';
import { OfficerClaimsService } from '../../core/claims/officer-claims.service';
import { CurrencyExposure, Dashboard } from '../../core/claims/officer.models';

export function formatDuration(seconds: number): string {
  const minutes = Math.floor(seconds / 60);
  if (minutes < 1) {
    return 'Less than 1 min';
  }
  const days = Math.floor(minutes / 1440);
  const hours = Math.floor((minutes % 1440) / 60);
  if (days > 0) {
    return `${days} d ${hours} h`;
  }
  return hours > 0 ? `${hours} h ${minutes % 60} min` : `${minutes} min`;
}

@Component({
  selector: 'app-officer-dashboard',
  templateUrl: './officer-dashboard.html',
  styleUrls: ['../../shared/claim-table.css', './officer-dashboard.css'],
})
export class OfficerDashboard implements OnInit {
  private readonly claimsApi = inject(OfficerClaimsService);
  protected readonly userId = inject(AuthService).userId();

  // Null while loading.
  protected readonly data = signal<{ dashboard: Dashboard; exposure: CurrencyExposure[] } | null>(null);
  protected readonly errors = signal<string[]>([]);

  protected readonly cards = computed(() => {
    const dashboard = this.data()?.dashboard;
    if (!dashboard) {
      return [];
    }
    const awaiting = dashboard.claimsByStatus.find((s) => s.status === 'AwaitingInformation');
    const mine = dashboard.officers.find((o) => o.officerId === this.userId);
    return [
      { label: 'Total claims', value: dashboard.totalClaims },
      { label: 'Unassigned', value: dashboard.unassignedClaims },
      { label: 'Open', value: dashboard.openClaims },
      { label: 'Awaiting information', value: awaiting?.count ?? 0 },
      { label: 'Decided', value: dashboard.decidedClaims },
      { label: 'My open claims', value: mine?.openAssigned ?? 0 },
    ];
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.data.set(null);
    this.errors.set([]);
    forkJoin({ dashboard: this.claimsApi.dashboard(), exposure: this.claimsApi.exposure() }).subscribe({
      next: (data) => this.data.set(data),
      error: (error: unknown) => this.errors.set(apiErrorMessages(error)),
    });
  }

  protected average(seconds: number | null): string {
    return seconds === null ? '–' : formatDuration(seconds);
  }

  protected exposure(row: CurrencyExposure): string {
    return formatMoney(row.exposureMinor, row.currencyCode, row.currencyDecimalPlaces);
  }
}
