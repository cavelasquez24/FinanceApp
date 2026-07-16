// src/api/categories.api.ts
import { apiClient } from './client';
import type { Category } from '../types/category.types';
import type { ApiResponse } from '../types/api.types';

export interface CategoryCreateDto {
  name: string;
  type: 'income' | 'expense' | 'both';
  icon?: string;
  color: string;
}

export interface CategoryUpdateDto {
  name: string;
  icon?: string;
  color: string;
}

export const categoriesApi = {
  getAll: (type?: 'income' | 'expense' | 'both') => 
    apiClient.get<ApiResponse<Category[]>>('/categories', { params: { type } }),
    
  create: (data: CategoryCreateDto) =>
    apiClient.post<ApiResponse<Category>>('/categories', data),
    
  update: (id: string, data: CategoryUpdateDto) =>
    apiClient.put<ApiResponse<Category>>(`/categories/${id}`, data),
    
  delete: (id: string) =>
    apiClient.delete<ApiResponse<void>>(`/categories/${id}`),
};