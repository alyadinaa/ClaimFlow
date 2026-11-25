import { claimReference, formatMoney, toMinorUnits } from './claim-format';
import { MARKETS, findMarket, marketToday } from './markets';

describe('claim formatting', () => {
  it('builds the reference from the id', () => {
    expect(claimReference(123)).toBe('CLM-000123');
    expect(claimReference(1234567)).toBe('CLM-1234567');
  });

  it('formats money using the currency decimal places', () => {
    expect(formatMoney(125050, 'SGD', 2)).toBe('SGD 1,250.50');
    expect(formatMoney(10000, 'JPY', 0)).toBe('JPY 10,000');
    expect(formatMoney(1_000_000_000_000, 'MYR', 2)).toBe('MYR 10,000,000,000.00');
  });

  it.each([
    ['1250.50', 2, 125050],
    ['1250.5', 2, 125050],
    ['0.29', 2, 29],
    ['1.15', 2, 115],
    [' 10 ', 2, 1000],
    ['10000', 0, 10000],
    ['10000000000', 2, 1_000_000_000_000],
    ['1.234', 2, null],
    ['100.5', 0, null],
    ['1,000', 2, null],
    ['-5', 2, null],
    ['1e5', 2, null],
    ['abc', 2, null],
    ['', 2, null],
  ])('converts "%s" with %i decimal places to %s', (text, places, expected) => {
    expect(toMinorUnits(text, places)).toBe(expected);
  });
});

describe('markets', () => {
  it('lists the six supported markets', () => {
    expect(MARKETS.map((m) => `${m.value} ${m.currencyCode} ${m.decimalPlaces}`)).toEqual([
      'Malaysia MYR 2',
      'Singapore SGD 2',
      'Australia AUD 2',
      'HongKong HKD 2',
      'Japan JPY 0',
      'NewZealand NZD 2',
    ]);
  });

  it('uses the date in the market', () => {
    const now = new Date('2026-11-25T12:00:00Z');
    expect(marketToday(findMarket('Singapore')!, now)).toBe('2026-11-25');
    expect(marketToday(findMarket('NewZealand')!, now)).toBe('2026-11-26');
  });
});
