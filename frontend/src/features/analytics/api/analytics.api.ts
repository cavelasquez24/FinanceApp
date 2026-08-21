import { apiClient } from '../../../api/client';
import type { ApiResponse } from '../../../types/api.types';
import type {
  FinancialHealthScoreDto,
  NetWorthTimelineDto,
  ExpenseIntelligenceDto,
  DebtProjectionDto,
  SavingsGoalEtaDto,
  YearOverYearDto,
  BudgetVsActualHistoryDto,
} from '../types/analytics.types';

export const analyticsApi = {
  getNetWorthTimeline: async (months = 12): Promise<NetWorthTimelineDto> => {
    const res = await apiClient.get<ApiResponse<NetWorthTimelineDto>>(
      `/analytics/net-worth-timeline?months=${months}`
    );
    return res.data.data!;
  },

  getHealthScore: async (month: number, year: number): Promise<FinancialHealthScoreDto> => {
    const res = await apiClient.get<ApiResponse<FinancialHealthScoreDto>>(
      `/analytics/health-score?month=${month}&year=${year}`
    );
    return res.data.data!;
  },

  getExpenseIntelligence: async (month: number, year: number): Promise<ExpenseIntelligenceDto> => {
    const res = await apiClient.get<ApiResponse<ExpenseIntelligenceDto>>(
      `/analytics/expense-intelligence?month=${month}&year=${year}`
    );
    return res.data.data!;
  },

  getDebtProjection: async (): Promise<DebtProjectionDto> => {
    const res = await apiClient.get<ApiResponse<DebtProjectionDto>>(
      `/analytics/debt-projection`
    );
    return res.data.data!;
  },

  getSavingsGoalEta: async (): Promise<SavingsGoalEtaDto[]> => {
    const res = await apiClient.get<ApiResponse<SavingsGoalEtaDto[]>>(
      `/analytics/savings-goals-eta`
    );
    return res.data.data!;
  },

  getYearOverYear: async (year: number): Promise<YearOverYearDto> => {
    const res = await apiClient.get<ApiResponse<YearOverYearDto>>(
      `/analytics/year-over-year?year=${year}`
    );
    return res.data.data!;
  },

  getBudgetVsActual: async (months = 6): Promise<BudgetVsActualHistoryDto> => {
    const res = await apiClient.get<ApiResponse<BudgetVsActualHistoryDto>>(
      `/analytics/budget-vs-actual?months=${months}`
    );
    return res.data.data!;
  },
};
