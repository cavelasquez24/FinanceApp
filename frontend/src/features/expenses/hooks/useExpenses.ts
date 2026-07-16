// src/features/expenses/hooks/useExpenses.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { expensesApi } from '../../../api/expenses.api';
import type { ExpenseCreateDto, ExpenseFilter, ExpenseUpdateDto } from '../../../types/expense.types';
import toast from 'react-hot-toast';

export function useExpenses(filters?: ExpenseFilter) {
  return useQuery({
    queryKey: ['expenses', filters],
    queryFn: () => expensesApi.getAll(filters),
  });
}

export function useCreateExpense() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: ExpenseCreateDto) => expensesApi.create(dto),
    onSuccess: () => {
      toast.success('Gasto registrado exitosamente');
      queryClient.invalidateQueries({ queryKey: ['expenses'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['budget'] });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error?.message || 'Error al registrar gasto');
    },
  });
}

export function useUpdateExpense() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: ExpenseUpdateDto }) =>
      expensesApi.update(id, dto),
    onSuccess: () => {
      toast.success('Gasto actualizado');
      queryClient.invalidateQueries({ queryKey: ['expenses'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['budget'] });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error?.message || 'Error al actualizar gasto');
    },
  });
}

export function useDeleteExpense() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => expensesApi.delete(id),
    onSuccess: () => {
      toast.success('Gasto eliminado');
      queryClient.invalidateQueries({ queryKey: ['expenses'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['budget'] });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error?.message || 'Error al eliminar gasto');
    },
  });
}