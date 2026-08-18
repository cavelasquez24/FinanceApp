/**
 * Las fechas de negocio son fechas civiles (YYYY-MM-DD), no instantes UTC.
 * Nunca derives una de `toISOString()`: en zonas al oeste de UTC eso puede
 * adelantar el día cuando el servidor UTC ya pasó medianoche.
 */
const pad = (value: number) => String(value).padStart(2, '0');

export function todayDateOnly(now: Date = new Date()): string {
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

export function dateOnlyFromParts(year: number, month: number, day: number): string {
  return `${year}-${pad(month)}-${pad(day)}`;
}

export function monthStartDateOnly(year: number, month: number): string {
  return dateOnlyFromParts(year, month, 1);
}

export function monthEndDateOnly(year: number, month: number): string {
  return dateOnlyFromParts(year, month, new Date(year, month, 0).getDate());
}


/** Construye una fecha local exclusivamente para formatos de presentación. */
export function parseDateOnly(dateOnly: string): Date {
  const [year, month, day] = dateOnly.split('-').map(Number);
  return new Date(year, month - 1, day);
}
