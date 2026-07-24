import { apiClient } from './client';
import type { ApiResponse } from '../types/api.types';
import type {
  AccountTransaction,
  CreateAccountDto,
  FinancialAccount,
  UpdateAccountDto,
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
};
