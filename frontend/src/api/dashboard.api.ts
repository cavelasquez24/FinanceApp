// src/api/dashboard.api.ts
import { apiClient } from './client';
import type {
  DashboardOverview,
  MonthlyTrend,
  ExpensesByCategoryResponse,
  CashFlowStatement,
} from '../types/dashboard.types';
import type { ApiResponse } from '../types/api.types';

export const dashboardApi = {
  getOverview: async (month: number, year: number) => {
    const response = await apiClient.get<ApiResponse<DashboardOverview>>(
      `/dashboard/overview?month=${month}&year=${year}`
    );
    return response.data.data;
  },

  getMonthlyTrend: async (months: number = 12) => {
    const response = await apiClient.get<ApiResponse<MonthlyTrend>>(
      `/dashboard/monthly-trend?months=${months}`
    );
    return response.data.data;
  },

  getExpensesByCategory: async (month: number, year: number) => {
    const response = await apiClient.get<ApiResponse<ExpensesByCategoryResponse>>(
      `/dashboard/expenses-by-category?month=${month}&year=${year}`
    );
    return response.data.data;
  },

  getCashFlowStatement: async (month: number, year: number) => {
    const response = await apiClient.get<ApiResponse<CashFlowStatement>>(
      `/dashboard/cashflow-statement?month=${month}&year=${year}`
    );
    return response.data.data;
  },
};