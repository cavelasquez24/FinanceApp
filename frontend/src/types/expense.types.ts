import type { Tag } from './tag.types';

export interface Expense {
  id: string;
  categoryId: string;
  categoryName: string;
  categoryColor: string;
  categoryIcon: string | null;
  amount: number;
  description: string | null;
  merchant: string | null;
  tags: Tag[];
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
  merchant?: string;
  tagIds: string[];
  date: string;
  paymentMethod: string;
  isRecurring: boolean;
  recurrenceType?: string | null;
  notes?: string;
}

export type ExpenseUpdateDto = ExpenseCreateDto;

export interface ExpenseFilter {
  page?: number;
  pageSize?: number;
  categoryId?: string;
  startDate?: string;
  endDate?: string;
  paymentMethod?: string;
  minAmount?: number;
  maxAmount?: number;
  search?: string;
  merchant?: string;
  tagIds?: string[];
  tagMatch?: 'any' | 'all';
  untagged?: boolean;
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