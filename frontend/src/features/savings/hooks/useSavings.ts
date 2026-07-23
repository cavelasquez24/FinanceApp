import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { savingsApi } from '../../../api/savings.api';
import toast from 'react-hot-toast';
import type { CreateSavingsGoalDto, DepositDto, WithdrawDto } from '../../../types/savings.types';


interface ApiError {
  response?: {
    data?: {
      error?: { code?: string };
    };
  };
}


export function useSavingsGoals() {
  return useQuery({
    queryKey: ['savings-goals'],
    queryFn: () => savingsApi.getAll(),
  });
}

export function useCreateSavingsGoal() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateSavingsGoalDto) => savingsApi.create(data),
    onSuccess: () => {
      toast.success('Meta de ahorro creada exitosamente');
      queryClient.invalidateQueries({ queryKey: ['savings-goals'] });
      // Invalida el dashboard si las metas se reflejan allí
      queryClient.invalidateQueries({ queryKey: ['dashboard'] }); 
    },
    onError: () => toast.error('Error al crear la meta de ahorro'),
  });
}

export function useUpdateSavingsGoal() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<CreateSavingsGoalDto> }) => 
      savingsApi.update(id, data),
    onSuccess: () => {
      // Invalidar la caché para refrescar la lista automáticamente
      queryClient.invalidateQueries({ queryKey: ['savings-goals'] });
    },
  });
}

export function useDeleteSavingsGoal() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (id: string) => savingsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['savings-goals'] });
    },
  });
}

export function useDepositSavings() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: DepositDto }) => 
      savingsApi.deposit(id, data),
    onSuccess: () => {
      toast.success('Depósito registrado exitosamente');
      queryClient.invalidateQueries({ queryKey: ['savings-goals'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: (error: ApiError) => {
      // Manejar el error 400 "GOAL_ALREADY_COMPLETED" u otros
      const errorCode = error.response?.data?.error?.code;
      if (errorCode === 'GOAL_ALREADY_COMPLETED') {
        toast.error('No se puede depositar en una meta completada');
      } else {
        toast.error('Error al registrar el depósito');
      }
    },
  });
}

export function useWithdrawSavings() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: WithdrawDto }) =>
      savingsApi.withdraw(id, data),
    onSuccess: () => {
      toast.success('Retiro registrado exitosamente');
      queryClient.invalidateQueries({ queryKey: ['savings-goals'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: (error: ApiError) => {
      const errorCode = error.response?.data?.error?.code;
      if (errorCode === 'INVALID_LINKED_EXPENSE') {
        toast.error('El gasto vinculado solo aplica cuando el motivo es "Consumido"');
      } else if (errorCode === 'INSUFFICIENT_SAVINGS_BALANCE') {
        toast.error('El retiro supera el saldo disponible');
      } else {
        toast.error('Error al registrar el retiro');
      }
    },
  });
}