import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { savingsApi } from '../../../api/savings.api';
import toast from 'react-hot-toast';
import type { ArchiveSavingsGoalDto, CreateSavingsGoalDto, DepositDto, WithdrawDto } from '../../../types/savings.types';
import { getApiErrorMessage } from '../../../utils/getApiError';


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
    refetchOnMount: 'always',
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
    onError: (error) => toast.error(getApiErrorMessage(error, 'Error al crear la meta de ahorro')),
  });
}

export function useUpdateSavingsGoal() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<CreateSavingsGoalDto> }) => 
      savingsApi.update(id, data),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['savings-goals'] }); },
    onError: (error) => toast.error(getApiErrorMessage(error, 'No se pudo actualizar la meta')),
  });
}

export function useDeleteSavingsGoal() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data?: ArchiveSavingsGoalDto }) => savingsApi.delete(id, data),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['savings-goals'] }); queryClient.invalidateQueries({ queryKey: ['accounts'] }); queryClient.invalidateQueries({ queryKey: ['dashboard'] }); },
    onError: (error) => toast.error(getApiErrorMessage(error, 'No se pudo archivar la meta')),
  });
}

export function useDepositSavings() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: DepositDto }) => 
      savingsApi.deposit(id, data),
    onSuccess: () => {
      toast.success('Aporte asignado a la meta');
      queryClient.invalidateQueries({ queryKey: ['savings-goals'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
    },
    onError: (error: ApiError) => {
      // Manejar el error 400 "GOAL_ALREADY_COMPLETED" u otros
      const errorCode = error.response?.data?.error?.code;
      if (errorCode === 'GOAL_ALREADY_COMPLETED') {
        toast.error('No se puede aportar a una meta completada');
      } else if (errorCode === 'INSUFFICIENT_ACCOUNT_BALANCE') {
        toast.error('La cuenta elegida no tiene saldo suficiente para respaldar el aporte');
      } else if (errorCode === 'INSUFFICIENT_UNALLOCATED_SAVINGS') {
        toast.error('No tienes saldo líquido sin asignar suficiente');
      } else if (errorCode === 'GOAL_TARGET_EXCEEDED') {
        toast.error('El aporte supera el monto restante de la meta');
      } else {
        toast.error(getApiErrorMessage(error, 'Error al registrar el depósito'));
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
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
    },
    onError: (error: ApiError) => {
      const errorCode = error.response?.data?.error?.code;
      if (errorCode === 'INVALID_LINKED_EXPENSE') {
        toast.error('El gasto vinculado solo aplica cuando el motivo es "Consumido"');
      } else if (errorCode === 'INSUFFICIENT_SAVINGS_BALANCE') {
        toast.error('El retiro supera el saldo asignado');
      } else if (errorCode === 'MINIMUM_PROTECTED_AMOUNT') {
        toast.error('El retiro dejaría el fondo bajo su mínimo protegido');
      } else if (errorCode === 'CORRECTION_REASON_REQUIRED') {
        toast.error('Escribe el motivo de la corrección');
      } else {
        toast.error(getApiErrorMessage(error, 'Error al registrar el retiro'));
      }
    },
  });
}
