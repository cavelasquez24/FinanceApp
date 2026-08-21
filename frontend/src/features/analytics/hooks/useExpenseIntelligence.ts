import { useQuery } from '@tanstack/react-query';
import { analyticsApi } from '../api/analytics.api';

export function useExpenseIntelligence(month: number, year: number) {
  return useQuery({
    queryKey: ['analytics', 'expense-intelligence', month, year],
    queryFn: () => analyticsApi.getExpenseIntelligence(month, year),
  });
}
