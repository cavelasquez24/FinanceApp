import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { creditCardsApi } from '../../../api/credit-cards.api';
import type {
  CreateCreditCardDto,
  CreditCardChargeDto,
  CreditCardPaymentDto,
  CreditCardPaymentVoidDto,
  UpdateCreditCardDto,
} from '../../../types/credit-card.types';

const errorMessage = (error: unknown, fallback: string) =>
  (error as { response?: { data?: { error?: { message?: string } } } })
    .response?.data?.error?.message ?? fallback;

function invalidateFinancialViews(queryClient: ReturnType<typeof useQueryClient>) {
  queryClient.invalidateQueries({ queryKey: ['credit-cards'] });
  queryClient.invalidateQueries({ queryKey: ['accounts'] });
  queryClient.invalidateQueries({ queryKey: ['expenses'] });
  queryClient.invalidateQueries({ queryKey: ['dashboard'] });
  queryClient.invalidateQueries({ queryKey: ['dashboard-current'] });
  queryClient.invalidateQueries({ queryKey: ['budget'] });
}

export function useCreditCards() {
  return useQuery({ queryKey: ['credit-cards'], queryFn: creditCardsApi.getAll });
}

export function useCreditCardTransactions(cardId?: string) {
  return useQuery({
    queryKey: ['credit-cards', cardId, 'transactions'],
    queryFn: () => creditCardsApi.getTransactions(cardId!),
    enabled: Boolean(cardId),
  });
}

export function useCreditCardPayments(cardId?: string) {
  return useQuery({
    queryKey: ['credit-cards', cardId, 'payments'],
    queryFn: () => creditCardsApi.getPayments(cardId!),
    enabled: Boolean(cardId),
  });
}

export function useCreateCreditCard() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (dto: CreateCreditCardDto) => creditCardsApi.create(dto),
    onSuccess: () => {
      toast.success('Tarjeta registrada');
      invalidateFinancialViews(queryClient);
    },
    onError: (error) => toast.error(errorMessage(error, 'No se pudo registrar la tarjeta')),
  });
}

export function useUpdateCreditCard() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: UpdateCreditCardDto }) =>
      creditCardsApi.update(id, dto),
    onSuccess: () => {
      toast.success('Tarjeta actualizada');
      invalidateFinancialViews(queryClient);
    },
    onError: (error) => toast.error(errorMessage(error, 'No se pudo actualizar la tarjeta')),
  });
}

export function usePayCreditCard() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: CreditCardPaymentDto }) =>
      creditCardsApi.pay(id, dto),
    onSuccess: () => {
      toast.success('Pago registrado sin duplicar el gasto');
      invalidateFinancialViews(queryClient);
    },
    onError: (error) => toast.error(errorMessage(error, 'No se pudo registrar el pago')),
  });
}
export function useVoidCreditCardPayment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, paymentId, dto }: {
      id: string; paymentId: string; dto: CreditCardPaymentVoidDto;
    }) => creditCardsApi.voidPayment(id, paymentId, dto),
    onSuccess: () => {
      toast.success('Pago anulado; saldos y comisión fueron reversados');
      invalidateFinancialViews(queryClient);
    },
    onError: (error) => toast.error(
      errorMessage(error, 'No se pudo anular el pago')
    ),
  });
}


export function useAddCreditCardCharge() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: CreditCardChargeDto }) =>
      creditCardsApi.addCharge(id, dto),
    onSuccess: () => {
      toast.success('Cargo financiero registrado');
      invalidateFinancialViews(queryClient);
    },
    onError: (error) => toast.error(errorMessage(error, 'No se pudo registrar el cargo')),
  });
}
