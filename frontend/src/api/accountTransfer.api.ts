import { apiClient } from './client';
import type { ApiResponse } from '../types/api.types';
import type {
  AccountTransferCreateDto,
  AccountTransferCreateResultDto,
  AccountTransferDto,
  AccountTransferSummaryDto,
} from '../types/accountTransfer.types';

export const accountTransferApi = {
  // POST /api/v1/transfers
  create: async (dto: AccountTransferCreateDto) => {
    const response = await apiClient.post<ApiResponse<AccountTransferCreateResultDto>>(
      '/transfers',
      dto,
    );
    return response.data.data;
  },

  // GET /api/v1/transfers
  getAll: async () => {
    const response = await apiClient.get<ApiResponse<AccountTransferSummaryDto[]>>('/transfers');
    return response.data.data;
  },

  // GET /api/v1/transfers/{id}
  getById: async (id: string) => {
    const response = await apiClient.get<ApiResponse<AccountTransferDto>>(`/transfers/${id}`);
    return response.data.data;
  },

  // DELETE /api/v1/transfers/{id}
  cancel: async (id: string) => {
    const response = await apiClient.delete<ApiResponse<null>>(`/transfers/${id}`);
    return response.data.data;
  },
};
