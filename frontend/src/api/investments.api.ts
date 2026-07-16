import { apiClient } from './client';
import type { 
  Investment, 
  InvestmentSummary, 
  InvestmentRecord,
  CreateInvestmentDto,
  UpdateInvestmentDto,
  CreateInvestmentRecordDto
} from '../types/investment.types';
import type { ApiResponse } from '../types/api.types';


export const investmentsApi = {
  // GET /api/v1/investments
  getAll: async () => {
    const response = await apiClient.get<ApiResponse<Investment[]>>('/investments');
    return response.data.data;
  },

  // GET /api/v1/investments/summary
  getSummary: async () => {
    const response = await apiClient.get<ApiResponse<InvestmentSummary>>('/investments/summary');
    return response.data.data;
  },

  // GET /api/v1/investments/{id}
  getById: async (id: string) => {
    const response = await apiClient.get<ApiResponse<Investment>>(`/investments/${id}`);
    return response.data.data;
  },

  // POST /api/v1/investments
  create: async (data: CreateInvestmentDto) => {
    const response = await apiClient.post<ApiResponse<Investment>>('/investments', data);
    return response.data.data;
  },

  // PUT /api/v1/investments/{id}
  update: async (id: string, data: UpdateInvestmentDto) => {
    const response = await apiClient.put<ApiResponse<Investment>>(`/investments/${id}`, data);
    return response.data.data;
  },

  // DELETE /api/v1/investments/{id}
  delete: async (id: string) => {
    const response = await apiClient.delete<ApiResponse<null>>(`/investments/${id}`);
    return response.data.data;
  },

  // GET /api/v1/investments/{id}/records
  getRecords: async (id: string) => {
    const response = await apiClient.get<ApiResponse<InvestmentRecord[]>>(`/investments/${id}/records`);
    return response.data.data;
  },

  // POST /api/v1/investments/{id}/records
  addRecord: async (id: string, data: CreateInvestmentRecordDto) => {
    const response = await apiClient.post<ApiResponse<InvestmentRecord>>(`/investments/${id}/records`, data);
    return response.data.data;
  }
};
