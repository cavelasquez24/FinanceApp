import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { incomesApi } from '../../../api/incomes.api';
import type {
  IncomeCreateDto,
  IncomeFilter,
  IncomeUpdateDto,
} from '../../../types/income.types';
import toast from 'react-hot-toast';

// --- QUERIES ---

export function useIncomes(filters?: IncomeFilter) {
  return useQuery({
    queryKey: ['incomes', filters],
    queryFn: () => incomesApi.getAll(filters),
  });
}

export function useIncomeSummary(month: number, year: number) {
  return useQuery({
    queryKey: ['incomes', 'summary', month, year],
    queryFn: () => incomesApi.getSummary(month, year),
  });
}

// --- MUTATIONS ---

export function useCreateIncome() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: IncomeCreateDto) => incomesApi.create(dto),
    onSuccess: () => {
      toast.success('Ingreso registrado exitosamente');
      // Invalidamos las queries para refrescar la tabla y el resumen
      queryClient.invalidateQueries({ queryKey: ['incomes'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error?.message || 'Error al registrar ingreso');
    },
  });
}

export function useUpdateIncome() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: IncomeUpdateDto }) =>
      incomesApi.update(id, dto),
    onSuccess: () => {
      toast.success('Ingreso actualizado');
      queryClient.invalidateQueries({ queryKey: ['incomes'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error?.message || 'Error al actualizar ingreso');
    },
  });
}

export function useDeleteIncome() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => incomesApi.delete(id),
    onSuccess: () => {
      toast.success('Ingreso eliminado');
      queryClient.invalidateQueries({ queryKey: ['incomes'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error?.message || 'Error al eliminar ingreso');
    },
  });
}