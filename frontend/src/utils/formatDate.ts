import { format, parseISO } from 'date-fns';
import { es } from 'date-fns/locale';

// Formatea una fecha ISO a formato legible
export function formatDate(dateString: string): string {
  return format(parseISO(dateString), 'dd MMM yyyy', { locale: es });
}

// Formatea solo mes y año
export function formatMonthYear(month: number, year: number): string {
  const date = new Date(year, month - 1);
  return format(date, 'MMMM yyyy', { locale: es });
}

// Retorna el mes y año actual
export function getCurrentPeriod() {
  const now = new Date();
  return {
    month: now.getMonth() + 1,
    year: now.getFullYear(),
  };
}

// Convierte DateOnly del backend (YYYY-MM-DD) a formato local
export function formatShortDate(dateString: string): string {
  return format(parseISO(dateString), 'dd/MM/yyyy');
}