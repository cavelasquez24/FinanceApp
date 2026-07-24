// src/features/budget/hooks/useBudget.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { budgetApi } from '../../../api/budget.api';
import toast from 'react-hot-toast';
import { type CreateBudgetDto, type BudgetUpdateDto } from '../../../types/budget.types';

export function useBudgetByPeriod(year: number, month: number, enabled = true) {
  return useQuery({
    queryKey: ['budget', year, month],
    queryFn: () => budgetApi.getByPeriod(year, month),
    enabled,
  });
}

export function useBudgetStatus(budgetId?: string) {
  return useQuery({
    queryKey: ['budget-status', budgetId],
    queryFn: () => budgetApi.getStatus(budgetId!),
    enabled: !!budgetId,
  });
}

export function useCreateBudget() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateBudgetDto) => budgetApi.create(data),
    onSuccess: () => {
      toast.success('Presupuesto creado exitosamente');
      queryClient.invalidateQueries({ queryKey: ['budget'] });
      queryClient.invalidateQueries({ queryKey: ['budget-status'] });
    },
    onError: () => {
      toast.error('Error al crear el presupuesto');
    },
  });
}

export function useUpdateBudget(budgetId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: BudgetUpdateDto) => budgetApi.update(budgetId, data),
    onSuccess: () => {
      toast.success('Presupuesto actualizado exitosamente');
      queryClient.invalidateQueries({ queryKey: ['budget'] });
      queryClient.invalidateQueries({ queryKey: ['budget-status'] });
    },
    onError: () => {
      toast.error('Error al actualizar el presupuesto');
    },
  });
}