import { apiClient } from './client';
import type { ApiResponse } from '../types/api.types';
import type { CurrentDashboard } from '../types/current-dashboard.types';

export const currentDashboardApi = {
  get: async () => {
    const response = await apiClient.get<ApiResponse<CurrentDashboard>>('/dashboard/current');
    return response.data.data;
  },
};
