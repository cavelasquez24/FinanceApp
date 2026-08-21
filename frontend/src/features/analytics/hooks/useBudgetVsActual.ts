import { useQuery } from '@tanstack/react-query';
import { analyticsApi } from '../api/analytics.api';

export function useBudgetVsActual(months = 6) {
  return useQuery({
    queryKey: ['analytics', 'budget-vs-actual', months],
    queryFn: () => analyticsApi.getBudgetVsActual(months),
  });
}
