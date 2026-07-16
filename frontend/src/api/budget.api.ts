import { apiClient } from './client';
import type { CreateBudgetDto, BudgetUpdateDto } from '../types/budget.types';
import type { ApiResponse } from '../types/api.types';

export interface BudgetPeriod {
  id: string;
  month: number;
  year: number;
  period: string;
  totalLimit: number | null;
  notes: string | null;
  categories: Array<{
    id: string;
    categoryId: string;
    categoryName: string;
    categoryColor: string;
    categoryIcon: string | null;
    amountLimit: number;
  }>;
}

export interface BudgetStatus {
  period: string;
  totalLimit: number;
  totalSpent: number;
  totalRemaining: number;
  percentageUsed: number;
  isOverBudget: boolean;
  categories: Array<{
    categoryName: string;
    categoryColor: string;
    categoryIcon: string | null;
    amountLimit: number;
    amountSpent: number;
    amountRemaining: number;
    percentageUsed: number;
    isOverBudget: boolean;
    alert: boolean;
  }>;
}

export const budgetApi = {
  create: async (data: CreateBudgetDto) => {
    const response = await apiClient.post<ApiResponse<BudgetPeriod>>('/budgets', data);
    return response.data.data;
  },

  // GET /api/v1/budgets/{year}/{month}
  getByPeriod: async (year: number, month: number) => {
    const response = await apiClient.get<ApiResponse<BudgetPeriod | null>>(`/budgets/${year}/${month}`);
    return response.data.data;
  },

  // GET /api/v1/budgets/{id}/status
  getStatus: async (id: string) => {
    const response = await apiClient.get<ApiResponse<BudgetStatus>>(`/budgets/${id}/status`);
    return response.data.data;
  },

  update: async (id: string, data: BudgetUpdateDto) => {
    const response = await apiClient.put<ApiResponse<BudgetPeriod>>(`/budgets/${id}`, data);
    return response.data.data;
  },
};