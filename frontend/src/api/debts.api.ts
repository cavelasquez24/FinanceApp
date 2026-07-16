import { apiClient } from './client';
import type {
  Debt,
  DebtSummary,
  DebtPayment,
  CreateDebtDto,
  UpdateDebtDto,
  CreateDebtPaymentDto,
} from '../types/debt.types';
import type { ApiResponse } from '../types/api.types';

export const debtsApi = {
  // GET /api/v1/debts
  getAll: async () => {
    const response = await apiClient.get<ApiResponse<Debt[]>>('/debts');
    return response.data.data;
  },

  // GET /api/v1/debts/summary
  getSummary: async () => {
    const response = await apiClient.get<ApiResponse<DebtSummary>>('/debts/summary');
    return response.data.data;
  },

  // GET /api/v1/debts/{id}
  getById: async (id: string) => {
    const response = await apiClient.get<ApiResponse<Debt>>(`/debts/${id}`);
    return response.data.data;
  },

  // POST /api/v1/debts
  create: async (data: CreateDebtDto) => {
    const response = await apiClient.post<ApiResponse<Debt>>('/debts', data);
    return response.data.data;
  },

  // PUT /api/v1/debts/{id}
  update: async (id: string, data: UpdateDebtDto) => {
    const response = await apiClient.put<ApiResponse<Debt>>(`/debts/${id}`, data);
    return response.data.data;
  },

  // DELETE /api/v1/debts/{id}
  delete: async (id: string) => {
    const response = await apiClient.delete<ApiResponse<null>>(`/debts/${id}`);
    return response.data.data;
  },

  // GET /api/v1/debts/{id}/payments
  getPayments: async (id: string) => {
    const response = await apiClient.get<ApiResponse<DebtPayment[]>>(`/debts/${id}/payments`);
    return response.data.data;
  },

  // POST /api/v1/debts/{id}/payments
  addPayment: async (id: string, data: CreateDebtPaymentDto) => {
    const response = await apiClient.post<ApiResponse<DebtPayment>>(`/debts/${id}/payments`, data);
    return response.data.data;
  },
};