import { useQuery } from '@tanstack/react-query';
import { analyticsApi } from '../api/analytics.api';

export function useNetWorthTimeline(months = 12) {
  return useQuery({
    queryKey: ['analytics', 'net-worth-timeline', months],
    queryFn: () => analyticsApi.getNetWorthTimeline(months),
  });
}
