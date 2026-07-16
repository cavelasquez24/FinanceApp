// src/features/categories/hooks/useCategories.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  categoriesApi,
  type CategoryCreateDto,
  type CategoryUpdateDto,
} from '../../../api/categories.api';

export function useCategories(type?: 'income' | 'expense' | 'both') {
  return useQuery({
    queryKey: ['categories', type],
    queryFn: async () => {
      const response = await categoriesApi.getAll(type);
      return response.data.data; // Desempaquetado de ApiResponse
    },
  });
}

export function useCreateCategory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CategoryCreateDto) => categoriesApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] });
    },
  });
}

export function useUpdateCategory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CategoryUpdateDto }) => 
      categoriesApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] });
    },
  });
}

export function useDeleteCategory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => categoriesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] });
    },
  });
}

export function useExpenseCategories() {
  return useQuery({
    queryKey: ['categories', 'expense'],
    queryFn: async () => {
      const response = await categoriesApi.getAll('expense');
      return response.data.data;
    },
  });
}