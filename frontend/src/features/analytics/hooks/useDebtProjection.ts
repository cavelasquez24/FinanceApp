import { useQuery } from '@tanstack/react-query';
import { analyticsApi } from '../api/analytics.api';

export function useDebtProjection() {
  return useQuery({
    queryKey: ['analytics', 'debt-projection'],
    queryFn: () => analyticsApi.getDebtProjection(),
  });
}
