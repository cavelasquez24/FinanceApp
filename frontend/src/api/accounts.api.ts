import { apiClient } from './client';
import type { ApiResponse } from '../types/api.types';
import type {
  AccountTransaction,
  CreateAccountDto,
  FinancialAccount,
  UpdateAccountDto,
  ReconciliationPreview,
  ReconciliationCreateDto,
  ReconciliationResult,
} from '../types/account.types';

export const accountsApi = {
  getAll: async () => {
    const response = await apiClient.get<ApiResponse<FinancialAccount[]>>('/accounts');
    return response.data.data;
  },
  getTransactions: async (count = 20) => {
    const response = await apiClient.get<ApiResponse<AccountTransaction[]>>(
      `/accounts/transactions?count=${count}`
    );
    return response.data.data;
  },
  create: async (dto: CreateAccountDto) => {
    const response = await apiClient.post<ApiResponse<FinancialAccount>>('/accounts', dto);
    return response.data.data;
  },
  update: async (id: string, dto: UpdateAccountDto) => {
    const response = await apiClient.put<ApiResponse<FinancialAccount>>(`/accounts/${id}`, dto);
    return response.data.data;
  },

  getReconciliationPreview: async (accountId: string) => {
    const response = await apiClient.get<ApiResponse<ReconciliationPreview>>(
      `/accounts/${accountId}/reconciliations/preview`
    );
    return response.data.data;
  },

  applyReconciliation: async (accountId: string, dto: ReconciliationCreateDto) => {
    const response = await apiClient.post<ApiResponse<ReconciliationResult>>(
      `/accounts/${accountId}/reconciliations`,
      dto
    );
    return response.data.data;
  },

  getReconciliationHistory: async (accountId: string, page = 1, pageSize = 20) => {
    const response = await apiClient.get<ApiResponse<ReconciliationResult[]>>(
      `/accounts/${accountId}/reconciliations?page=${page}&pageSize=${pageSize}`
    );
    return response.data.data;
  },
};
