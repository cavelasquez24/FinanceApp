export type RestorationPlanMode = 'deadline' | 'monthly_amount';

export interface RestorationPlanPreview {
  monthlyAmount: number;
  targetDate: string;
  estimatedCompletionDate: string;
  paymentsCount: number;
  finalPayment: number;
  paymentDates: string[];
}

interface PlanInput {
  outstandingAmount: number;
  firstScheduledDate: string;
  mode: RestorationPlanMode;
  targetDate?: string;
  monthlyAmount?: number;
}

const roundCurrency = (value: number) => Math.round((value + Number.EPSILON) * 100) / 100;
const ceilCurrency = (value: number) => Math.ceil((value - Number.EPSILON) * 100) / 100;

function parseDate(value: string): Date | null {
  const parts = value.split('-').map(Number);
  if (parts.length !== 3 || parts.some((part) => !Number.isFinite(part))) return null;
  const [year, month, day] = parts;
  const date = new Date(year, month - 1, day);
  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day
    ? date
    : null;
}

function toDateString(date: Date): string {
  return [
    date.getFullYear(),
    String(date.getMonth() + 1).padStart(2, '0'),
    String(date.getDate()).padStart(2, '0'),
  ].join('-');
}

export function addMonthsClamped(value: string, months: number): string {
  const source = parseDate(value);
  if (!source) return '';

  const originalDay = source.getDate();
  const target = new Date(source.getFullYear(), source.getMonth() + months, 1);
  const lastDay = new Date(target.getFullYear(), target.getMonth() + 1, 0).getDate();
  target.setDate(Math.min(originalDay, lastDay));
  return toDateString(target);
}

export function countMonthlyOccurrences(firstDate: string, targetDate: string): number {
  const first = parseDate(firstDate);
  const target = parseDate(targetDate);
  if (!first || !target || first > target) return 0;

  let count = 0;
  let cursor = firstDate;
  while (cursor && cursor <= targetDate && count < 1200) {
    count += 1;
    cursor = addMonthsClamped(firstDate, count);
  }
  return count;
}

export function calculateRestorationPlan(input: PlanInput): RestorationPlanPreview | null {
  const outstanding = roundCurrency(input.outstandingAmount);
  if (outstanding <= 0 || !parseDate(input.firstScheduledDate)) return null;

  let monthlyAmount: number;
  let targetDate: string;

  if (input.mode === 'deadline') {
    if (!input.targetDate) return null;
    const occurrences = countMonthlyOccurrences(input.firstScheduledDate, input.targetDate);
    if (occurrences <= 0) return null;
    monthlyAmount = ceilCurrency(outstanding / occurrences);
    targetDate = input.targetDate;
  } else {
    monthlyAmount = roundCurrency(input.monthlyAmount ?? 0);
    if (monthlyAmount <= 0 || monthlyAmount > outstanding) return null;
    const payments = Math.ceil(outstanding / monthlyAmount);
    targetDate = addMonthsClamped(input.firstScheduledDate, payments - 1);
  }

  const paymentsCount = Math.ceil(outstanding / monthlyAmount);
  const estimatedCompletionDate = addMonthsClamped(input.firstScheduledDate, paymentsCount - 1);
  const finalPayment = roundCurrency(outstanding - monthlyAmount * (paymentsCount - 1));
  const paymentDates = Array.from(
    { length: Math.min(paymentsCount, 4) },
    (_, index) => addMonthsClamped(input.firstScheduledDate, index),
  );

  return {
    monthlyAmount,
    targetDate,
    estimatedCompletionDate,
    paymentsCount,
    finalPayment,
    paymentDates,
  };
}

export function getLocalToday(): string {
  return toDateString(new Date());
}

export function formatPlanDate(value: string): string {
  const date = parseDate(value);
  return date
    ? new Intl.DateTimeFormat('es-EC', { day: 'numeric', month: 'short', year: 'numeric' }).format(date)
    : value;
}
