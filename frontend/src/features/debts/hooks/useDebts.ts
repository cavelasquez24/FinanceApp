import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { debtsApi } from '../../../api/debts.api';
import toast from 'react-hot-toast';
import type {
  CreateDebtDto,
  UpdateDebtDto,
  CreateDebtPaymentDto,
} from '../../../types/debt.types';

export function useDebts() {
  return useQuery({
    queryKey: ['debts'],
    queryFn: () => debtsApi.getAll(),
  });
}

export function useDebtSummary() {
  return useQuery({
    queryKey: ['debts', 'summary'],
    queryFn: () => debtsApi.getSummary(),
  });
}

export function useDebt(id: string) {
  return useQuery({
    queryKey: ['debts', id],
    queryFn: () => debtsApi.getById(id),
    enabled: !!id,
  });
}

export function useDebtPayments(id: string) {
  return useQuery({
    queryKey: ['debts', id, 'payments'],
    queryFn: () => debtsApi.getPayments(id),
    enabled: !!id,
  });
}

export function useCreateDebt() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateDebtDto) => debtsApi.create(data),
    onSuccess: () => {
      toast.success('Deuda registrada exitosamente');
      queryClient.invalidateQueries({ queryKey: ['debts'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: () => toast.error('Error al registrar la deuda'),
  });
}

export function useUpdateDebt() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateDebtDto }) =>
      debtsApi.update(id, data),
    onSuccess: () => {
      toast.success('Deuda actualizada exitosamente');
      queryClient.invalidateQueries({ queryKey: ['debts'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: () => toast.error('Error al actualizar la deuda'),
  });
}

export function useDeleteDebt() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => debtsApi.delete(id),
    onSuccess: () => {
      toast.success('Deuda eliminada exitosamente');
      queryClient.invalidateQueries({ queryKey: ['debts'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: () => toast.error('Error al eliminar la deuda'),
  });
}

export function useAddDebtPayment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CreateDebtPaymentDto }) =>
      debtsApi.addPayment(id, data),
    onSuccess: (_, variables) => {
      toast.success('Pago registrado exitosamente');
      queryClient.invalidateQueries({ queryKey: ['debts'] });
      queryClient.invalidateQueries({ queryKey: ['debts', variables.id, 'payments'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: (error: any) => {
      const message = error?.response?.data?.error?.message || 'Error al registrar el pago';
      toast.error(message);
    },
  });
}