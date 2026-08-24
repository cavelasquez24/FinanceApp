import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { accountsApi } from '../../../api/accounts.api';
import type { CreateAccountDto, UpdateAccountDto } from '../../../types/account.types';

export function useAccounts() {
  return useQuery({ queryKey: ['accounts'], queryFn: accountsApi.getAll });
}

export function useAccountTransactions(count = 20) {
  return useQuery({
    queryKey: ['accounts', 'transactions', count],
    queryFn: () => accountsApi.getTransactions(count),
  });
}

export function useCreateAccount() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (dto: CreateAccountDto) => accountsApi.create(dto),
    onSuccess: () => {
      toast.success('Cuenta creada');
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard-current'] });
    },
    onError: () => toast.error('No se pudo crear la cuenta'),
  });
}

export function useUpdateAccount() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: UpdateAccountDto }) =>
      accountsApi.update(id, dto),
    onSuccess: () => {
      toast.success('Saldo actualizado');
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard-current'] });
    },
    onError: () => toast.error('No se pudo actualizar la cuenta'),
  });
}
