import { useQuery } from '@tanstack/react-query';
import { analyticsApi } from '../api/analytics.api';

export function useHealthScore(month: number, year: number) {
  return useQuery({
    queryKey: ['analytics', 'health-score', month, year],
    queryFn: () => analyticsApi.getHealthScore(month, year),
  });
}
