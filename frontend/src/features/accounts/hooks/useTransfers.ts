import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { accountTransferApi } from '../../../api/accountTransfer.api';
import type { AccountTransferCreateDto } from '../../../types/accountTransfer.types';
import { getApiErrorMessage } from '../../../utils/getApiError';

export function useTransfers() {
  return useQuery({
    queryKey: ['transfers'],
    queryFn: accountTransferApi.getAll,
  });
}

export function useCreateTransfer(onSuccess?: () => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: AccountTransferCreateDto) => accountTransferApi.create(dto),
    onSuccess: (result) => {
      // El modal se cierra primero: un aviso de saldo bajo mientras el
      // formulario sigue abierto se lee como si la operación hubiera fallado.
      onSuccess?.();

      queryClient.invalidateQueries({ queryKey: ['transfers'] });
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard-current'] });

      toast.success('Transferencia realizada');
      if (result?.insufficientFundsWarning) {
        toast('La cuenta de origen quedó con saldo bajo', {
          icon: '⚠️',
          style: { background: '#FFFBEB', color: '#92400E', border: '1px solid #FDE68A' },
        });
      }
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, 'No se pudo registrar la transferencia')),
  });
}

export function useCancelTransfer(onSuccess?: () => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => accountTransferApi.cancel(id),
    onSuccess: () => {
      toast.success('Transferencia cancelada');
      queryClient.invalidateQueries({ queryKey: ['transfers'] });
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard-current'] });
      onSuccess?.();
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, 'No se pudo cancelar la transferencia')),
  });
}
