import { apiClient } from './client';
import type { ApiResponse } from '../types/api.types';
import type { Tag, TagExpenseReport } from '../types/tag.types';

export const tagsApi = {
  getAll: (search?: string) =>
    apiClient.get<ApiResponse<Tag[]>>('/tags', { params: { search } }),
  create: (data: { name: string; color?: string | null }) =>
    apiClient.post<ApiResponse<Tag>>('/tags', data),
  update: (id: string, data: { name: string; color?: string | null }) =>
    apiClient.put<ApiResponse<Tag>>('/tags/' + id, data),
  delete: (id: string) =>
    apiClient.delete<ApiResponse<void>>('/tags/' + id),
  merge: (sourceId: string, targetTagId: string) =>
    apiClient.post<ApiResponse<Tag>>('/tags/' + sourceId + '/merge', { targetTagId }),
  expenseReport: (startDate: string, endDate: string) =>
    apiClient.get<ApiResponse<TagExpenseReport>>('/tags/expense-report', {
      params: { startDate, endDate },
    }),
};
