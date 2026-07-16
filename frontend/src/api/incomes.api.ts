import type { ApiResponse, PagedResponse } from '../types/api.types';
import type {
  Income,
  IncomeCreateDto,
  IncomeFilter,
  IncomeSummary,
  IncomeUpdateDto,
} from '../types/income.types';
import { apiClient } from './client';

export const incomesApi = {
  getAll: (filters?: IncomeFilter) =>
    apiClient.get<ApiResponse<PagedResponse<Income>>>('/incomes', {
      params: filters,
    }),

  getById: (id: string) =>
    apiClient.get<ApiResponse<Income>>(`/incomes/${id}`),

  create: (dto: IncomeCreateDto) =>
    apiClient.post<ApiResponse<Income>>('/incomes', dto),

  update: (id: string, dto: IncomeUpdateDto) =>
    apiClient.put<ApiResponse<Income>>(`/incomes/${id}`, dto),

  delete: (id: string) =>
    apiClient.delete<ApiResponse<null>>(`/incomes/${id}`),

  getSummary: (month: number, year: number) =>
    apiClient.get<ApiResponse<IncomeSummary>>('/incomes/summary', {
      params: { month, year },
    }),
};