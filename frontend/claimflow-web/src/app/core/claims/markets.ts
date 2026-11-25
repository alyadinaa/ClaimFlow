import { Market } from './claim.models';

export interface MarketInfo {
  value: Market;
  label: string;
  currencyCode: string;
  decimalPlaces: number;
  timeZone: string;
}

// A copy of the backend's MarketCatalog, which has no endpoint. The API makes the final check.
export const MARKETS: readonly MarketInfo[] = [
  { value: 'Malaysia', label: 'Malaysia', currencyCode: 'MYR', decimalPlaces: 2, timeZone: 'Asia/Kuala_Lumpur' },
  { value: 'Singapore', label: 'Singapore', currencyCode: 'SGD', decimalPlaces: 2, timeZone: 'Asia/Singapore' },
  { value: 'Australia', label: 'Australia', currencyCode: 'AUD', decimalPlaces: 2, timeZone: 'Australia/Sydney' },
  { value: 'HongKong', label: 'Hong Kong', currencyCode: 'HKD', decimalPlaces: 2, timeZone: 'Asia/Hong_Kong' },
  { value: 'Japan', label: 'Japan', currencyCode: 'JPY', decimalPlaces: 0, timeZone: 'Asia/Tokyo' },
  { value: 'NewZealand', label: 'New Zealand', currencyCode: 'NZD', decimalPlaces: 2, timeZone: 'Pacific/Auckland' },
];

export function findMarket(value: string): MarketInfo | undefined {
  return MARKETS.find((market) => market.value === value);
}

export function marketLabel(value: Market): string {
  return findMarket(value)?.label ?? value;
}

// Today's date in the market, as yyyy-mm-dd.
export function marketToday(market: MarketInfo, now = new Date()): string {
  const parts = new Intl.DateTimeFormat('en', {
    timeZone: market.timeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(now);
  const part = (type: string) => parts.find((p) => p.type === type)!.value;
  return `${part('year')}-${part('month')}-${part('day')}`;
}
