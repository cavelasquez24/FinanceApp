// src/features/dashboard/hooks/useDashboard.ts
import { useQuery } from '@tanstack/react-query';
import { dashboardApi } from '../../../api/dashboard.api';

export function useDashboardOverview(month: number, year: number) {
  return useQuery({
    queryKey: ['dashboard', 'overview', month, year],
    queryFn: () => dashboardApi.getOverview(month, year),
  });
}

export function useDashboardTrend(months: number = 12) {
  return useQuery({
    queryKey: ['dashboard', 'trend', months],
    queryFn: () => dashboardApi.getMonthlyTrend(months),
  });
}

export function useDashboardExpensesByCategory(month: number, year: number) {
  return useQuery({
    queryKey: ['dashboard', 'expenses-by-category', month, year],
    queryFn: () => dashboardApi.getExpensesByCategory(month, year),
  });
}