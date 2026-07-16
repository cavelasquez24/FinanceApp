// Formatea un número como moneda según el código ISO
export function formatCurrency(
  amount: number,
  currencyCode: string = 'USD'
): string {
  return new Intl.NumberFormat('es-EC', {
    style: 'currency',
    currency: currencyCode,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);
}

// Formatea solo el número sin símbolo de moneda
export function formatNumber(amount: number): string {
  return new Intl.NumberFormat('es-EC', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);
}