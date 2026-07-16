import { apiClient } from './client';
import type { SavingsGoal, CreateSavingsGoalDto, DepositDto } from '../types/savings.types';
import type { ApiResponse } from '../types/api.types';

export const savingsApi = {
  // GET /api/v1/savings-goals
  getAll: async () => {
    const response = await apiClient.get<ApiResponse<SavingsGoal[]>>('/savings-goals');
    return response.data.data;
  },

  // GET /api/v1/savings-goals/{id}
  getById: async (id: string) => {
    const response = await apiClient.get<ApiResponse<SavingsGoal>>(`/savings-goals/${id}`);
    return response.data.data;
  },

  // POST /api/v1/savings-goals
  create: async (data: CreateSavingsGoalDto) => {
    const response = await apiClient.post<ApiResponse<SavingsGoal>>('/savings-goals', data);
    return response.data.data;
  },

  // PUT /api/v1/savings-goals/{id}
  update: async (id: string, data: Partial<CreateSavingsGoalDto>) => {
    const response = await apiClient.put<ApiResponse<SavingsGoal>>(`/savings-goals/${id}`, data);
    return response.data.data;
  },

  // DELETE /api/v1/savings-goals/{id}
  delete: async (id: string) => {
    const response = await apiClient.delete<ApiResponse<null>>(`/savings-goals/${id}`);
    return response.data.data;
  },

  // PATCH /api/v1/savings-goals/{id}/deposit
  deposit: async (id: string, data: DepositDto) => {
    const response = await apiClient.patch<ApiResponse<null>>(`/savings-goals/${id}/deposit`, data);
    return response.data.data;
  }
};