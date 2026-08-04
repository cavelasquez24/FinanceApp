import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import toast from 'react-hot-toast';
import { tagsApi } from '../../../api/tags.api';
import type { ApiResponse } from '../../../types/api.types';

export function useTags(search?: string) {
  return useQuery({
    queryKey: ['tags', search],
    queryFn: async () => (await tagsApi.getAll(search)).data.data ?? [],
  });
}

function useTagMutation<T, TResult>(mutationFn: (data: T) => Promise<TResult>, message: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: () => {
      toast.success(message);
      queryClient.invalidateQueries({ queryKey: ['tags'] });
      queryClient.invalidateQueries({ queryKey: ['expenses'] });
      queryClient.invalidateQueries({ queryKey: ['tag-report'] });
    },
    onError: (error: unknown) => {
      const apiError = error instanceof AxiosError ? error.response?.data as ApiResponse<never> | undefined : undefined;
      toast.error(apiError?.error?.message ?? 'No se pudo completar la operación');
    },
  });
}

export function useCreateTag() {
  return useTagMutation((data: { name: string; color?: string | null }) => tagsApi.create(data), 'Etiqueta creada');
}
export function useUpdateTag() {
  return useTagMutation(({ id, ...data }: { id: string; name: string; color?: string | null }) => tagsApi.update(id, data), 'Etiqueta actualizada');
}
export function useDeleteTag() {
  return useTagMutation((id: string) => tagsApi.delete(id), 'Etiqueta eliminada');
}
export function useMergeTags() {
  return useTagMutation(({ sourceId, targetTagId }: { sourceId: string; targetTagId: string }) => tagsApi.merge(sourceId, targetTagId), 'Etiquetas fusionadas');
}
export function useTagExpenseReport(startDate: string, endDate: string) {
  return useQuery({
    queryKey: ['tag-report', startDate, endDate],
    queryFn: async () => (await tagsApi.expenseReport(startDate, endDate)).data.data,
  });
}
