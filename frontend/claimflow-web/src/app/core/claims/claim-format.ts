// The backend has no reference field; the reference is a readable form of the id.
export function claimReference(id: number): string {
  return `CLM-${String(id).padStart(6, '0')}`;
}

export function formatMoney(amountMinor: number, currencyCode: string, decimalPlaces: number): string {
  const amount = (amountMinor / 10 ** decimalPlaces).toLocaleString('en', {
    minimumFractionDigits: decimalPlaces,
    maximumFractionDigits: decimalPlaces,
  });
  return `${currencyCode} ${amount}`;
}

// Converts "1250.5" to 125050 for two decimal places. It works on the text, so there is no
// floating-point rounding. Returns null if the text is not a valid amount for the currency.
export function toMinorUnits(text: string, decimalPlaces: number): number | null {
  const match = /^(\d{1,13})(?:\.(\d+))?$/.exec(text.trim());
  const fraction = match?.[2] ?? '';
  if (!match || fraction.length > decimalPlaces) {
    return null;
  }
  return Number(match[1] + fraction.padEnd(decimalPlaces, '0'));
}
