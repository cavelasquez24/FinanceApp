// src/utils/cycleUtils.ts

/**
 * Calcula el rango real del ciclo financiero del usuario.
 * Espeja GetCycleRange de DashboardService.cs (backend) para mostrar
 * el rango exacto sin necesidad de un endpoint nuevo.
 * paydayDay null/undefined → retorna null (mes calendario estándar, sin badge).
 */
export function getCycleRange(
  month: number,
  year: number,
  paydayDay: number | null | undefined
): { start: Date; end: Date } | null {
  if (paydayDay == null) return null;

  const daysInMonth = (y: number, m: number) => new Date(y, m, 0).getDate();

  const day = Math.min(paydayDay, daysInMonth(year, month));
  const start = new Date(year, month - 1, day);

  const nextMonth = month === 12 ? 1 : month + 1;
  const nextYear = month === 12 ? year + 1 : year;
  const nextDay = Math.min(paydayDay, daysInMonth(nextYear, nextMonth));
  const end = new Date(nextYear, nextMonth - 1, nextDay);
  end.setDate(end.getDate() - 1);

  return { start, end };
}

const SHORT_MONTHS = [
  'Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun',
  'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic',
];

export function formatCycleLabel(range: { start: Date; end: Date }): string {
  const fmt = (d: Date) => `${d.getDate()} ${SHORT_MONTHS[d.getMonth()]}`;
  return `${fmt(range.start)} – ${fmt(range.end)} ${range.end.getFullYear()}`;
}