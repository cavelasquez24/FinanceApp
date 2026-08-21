import { useQuery } from '@tanstack/react-query';
import { analyticsApi } from '../api/analytics.api';

export function useYearOverYear(year: number) {
  return useQuery({
    queryKey: ['analytics', 'year-over-year', year],
    queryFn: () => analyticsApi.getYearOverYear(year),
  });
}
