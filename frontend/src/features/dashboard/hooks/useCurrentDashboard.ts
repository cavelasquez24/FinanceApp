import { useQuery } from '@tanstack/react-query';
import { currentDashboardApi } from '../../../api/current-dashboard.api';

export function useCurrentDashboard() {
  return useQuery({
    queryKey: ['dashboard-current'],
    queryFn: currentDashboardApi.get,
  });
}
