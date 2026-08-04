import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { savingsApi } from '../../../api/savings.api';
import type { EmergencyFundRestorationPaymentDto, EmergencyFundUseDto } from '../../../types/savings.types';
import { getApiErrorMessage } from '../../../utils/getApiError';

function useRefreshFinancialData() {
  const queryClient = useQueryClient();
  return () => Promise.all([
    queryClient.invalidateQueries({ queryKey: ['savings-goals'] }),
    queryClient.invalidateQueries({ queryKey: ['emergency-fund-restorations'] }),
    queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
    queryClient.invalidateQueries({ queryKey: ['dashboard-current'] }),
    queryClient.invalidateQueries({ queryKey: ['expenses'] }),
    queryClient.invalidateQueries({ queryKey: ['accounts'] }),
  ]);
}

export function useEmergencyFundRestorations(goalId?: string) {
  return useQuery({
    queryKey: ['emergency-fund-restorations', goalId],
    queryFn: () => savingsApi.getRestorations(goalId!),
    enabled: Boolean(goalId),
    refetchOnMount: 'always',
  });
}

export function useCreateEmergencyFundUse() {
  const refresh = useRefreshFinancialData();
  return useMutation({ mutationFn: ({ goalId, data }: { goalId: string; data: EmergencyFundUseDto }) => savingsApi.createEmergencyFundUse(goalId, data), onSuccess: () => { toast.success('Uso del fondo y plan de restauración registrados'); void refresh(); }, onError: (error) => toast.error(getApiErrorMessage(error, 'No se pudo registrar el uso del fondo')) });
}

export function useRegisterRestorationPayment() {
  const refresh = useRefreshFinancialData();
  return useMutation({ mutationFn: ({ restorationId, data }: { restorationId: string; data: EmergencyFundRestorationPaymentDto }) => savingsApi.registerRestorationPayment(restorationId, data), onSuccess: () => { toast.success('Aporte de restauración registrado'); void refresh(); }, onError: (error) => toast.error(getApiErrorMessage(error, 'No se pudo registrar el aporte')) });
}

export function useCancelRestoration() {
  const refresh = useRefreshFinancialData();
  return useMutation({ mutationFn: savingsApi.cancelRestoration, onSuccess: () => { toast.success('Compromiso de restauración cancelado'); void refresh(); }, onError: (error) => toast.error(getApiErrorMessage(error, 'No se pudo cancelar la restauración')) });
}

export function useProcessDueRestorations() {
  const refresh = useRefreshFinancialData();
  return useMutation({
    mutationKey: ['process-due-restorations'],
    mutationFn: (asOfDate: string) => savingsApi.processDueRestorations(asOfDate),
    onSuccess: (result) => {
      if (!result) return;
      if (result.processedCount > 0) {
        toast.success(`${result.processedCount} aporte${result.processedCount === 1 ? '' : 's'} programado${result.processedCount === 1 ? '' : 's'} aplicado${result.processedCount === 1 ? '' : 's'}`, { id: 'due-restorations-processed' });
        void refresh();
      }
      if (result.insufficientFundsCount > 0) toast.error(result.insufficientFundsCount === 1 ? 'Hay un aporte vencido sin saldo suficiente en la cuenta programada' : 'Hay aportes vencidos sin saldo suficiente en sus cuentas programadas', { id: 'due-restorations-insufficient' });
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'No se pudieron procesar los aportes programados'), { id: 'due-restorations-error' }),
  });
}


