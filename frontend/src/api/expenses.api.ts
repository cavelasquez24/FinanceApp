import type { ApiResponse, PagedResponse } from '../types/api.types';
import type {
  Expense,
  ExpenseCreateDto,
  ExpenseFilter,
  ExpenseUpdateDto,
} from '../types/expense.types';
import { apiClient } from './client';

export const expensesApi = {
  getAll: (filters?: ExpenseFilter) =>
    apiClient.get<ApiResponse<PagedResponse<Expense>>>('/expenses', {
      params: filters,
    }),

  getById: (id: string) =>
    apiClient.get<ApiResponse<Expense>>(`/expenses/${id}`),

  create: (dto: ExpenseCreateDto) =>
    apiClient.post<ApiResponse<Expense>>('/expenses', dto),

  update: (id: string, dto: ExpenseUpdateDto) =>
    apiClient.put<ApiResponse<Expense>>(`/expenses/${id}`, dto),

  delete: (id: string) =>
    apiClient.delete<ApiResponse<null>>(`/expenses/${id}`),

  getSummary: (month: number, year: number) =>
    apiClient.get<ApiResponse<any>>('/expenses/summary', {
      params: { month, year },
    }),
};