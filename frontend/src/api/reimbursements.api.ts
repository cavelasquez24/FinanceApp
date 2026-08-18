import { apiClient } from './client';
import type { ApiResponse } from '../types/api.types';
import type {
  CreateReimbursementDto,
  Reimbursement,
  ReimbursementSummary,
  UpdateReimbursementDto,
} from '../types/reimbursement.types';

export const reimbursementsApi = {
  getAll: async (startDate?: string, endDate?: string) => {
    const response = await apiClient.get<ApiResponse<Reimbursement[]>>('/reimbursements', {
      params: { startDate, endDate },
    });
    return response.data.data;
  },
  getSummary: async (startDate: string, endDate: string) => {
    const response = await apiClient.get<ApiResponse<ReimbursementSummary>>('/reimbursements/summary', {
      params: { startDate, endDate },
    });
    return response.data.data;
  },
  create: async (dto: CreateReimbursementDto) => {
    const response = await apiClient.post<ApiResponse<Reimbursement>>('/reimbursements', dto);
    return response.data.data;
  },
  update: async (id: string, dto: UpdateReimbursementDto) => {
    const response = await apiClient.put<ApiResponse<Reimbursement>>(`/reimbursements/${id}`, dto);
    return response.data.data;
  },
  remove: async (id: string) => {
    await apiClient.delete(`/reimbursements/${id}`);
  },
};

