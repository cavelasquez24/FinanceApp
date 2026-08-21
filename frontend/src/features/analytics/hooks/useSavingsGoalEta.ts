import { useQuery } from '@tanstack/react-query';
import { analyticsApi } from '../api/analytics.api';

export function useSavingsGoalEta() {
  return useQuery({
    queryKey: ['analytics', 'savings-goals-eta'],
    queryFn: () => analyticsApi.getSavingsGoalEta(),
  });
}
