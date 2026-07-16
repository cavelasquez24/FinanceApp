export interface Expense {
  id: string;
  categoryId: string;
  categoryName: string;
  categoryColor: string;
  categoryIcon: string | null;
  amount: number;
  description: string | null;
  date: string;
  paymentMethod: string;
  isRecurring: boolean;
  recurrenceType: RecurrenceType | null;
  notes: string | null;
  createdAt: string;
}

export interface ExpenseCreateDto {
  categoryId: string;
  amount: number;
  description?: string;
  date: string;
  paymentMethod: string;
  isRecurring: boolean;
  recurrenceType?: string | null;
  notes?: string;
}

export interface ExpenseUpdateDto extends ExpenseCreateDto {}

export interface ExpenseFilter {
  page?: number;
  pageSize?: number;
  categoryId?: string;
  startDate?: string;
  endDate?: string;
  paymentMethod?: string;
  isRecurring?: boolean;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export type RecurrenceType =
  | 'daily'
  | 'weekly'
  | 'biweekly'
  | 'monthly'
  | 'yearly';