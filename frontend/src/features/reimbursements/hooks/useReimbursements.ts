import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { reimbursementsApi } from '../../../api/reimbursements.api';
import type { CreateReimbursementDto, UpdateReimbursementDto } from '../../../types/reimbursement.types';

const errorMessage = (error: unknown, fallback: string) =>
  (error as { response?: { data?: { error?: { message?: string } } } })
    .response?.data?.error?.message ?? fallback;

function invalidateFinancialViews(queryClient: ReturnType<typeof useQueryClient>) {
  for (const key of ['reimbursements', 'expenses', 'accounts', 'credit-cards', 'dashboard', 'dashboard-current', 'budget']) {
    queryClient.invalidateQueries({ queryKey: [key] });
  }
}

export function useReimbursements(startDate?: string, endDate?: string) {
  return useQuery({
    queryKey: ['reimbursements', startDate, endDate],
    queryFn: () => reimbursementsApi.getAll(startDate, endDate),
  });
}

export function useReimbursementSummary(startDate: string, endDate: string) {
  return useQuery({
    queryKey: ['reimbursements', 'summary', startDate, endDate],
    queryFn: () => reimbursementsApi.getSummary(startDate, endDate),
  });
}

export function useCreateReimbursement() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (dto: CreateReimbursementDto) => reimbursementsApi.create(dto),
    onSuccess: () => {
      toast.success('Reembolso registrado sin sumarlo a ingresos');
      invalidateFinancialViews(queryClient);
    },
    onError: (error) => toast.error(errorMessage(error, 'No se pudo registrar el reembolso')),
  });
}

export function useUpdateReimbursement() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: UpdateReimbursementDto }) =>
      reimbursementsApi.update(id, dto),
    onSuccess: () => {
      toast.success('Reembolso actualizado');
      invalidateFinancialViews(queryClient);
    },
    onError: (error) => toast.error(errorMessage(error, 'No se pudo actualizar el reembolso')),
  });
}

export function useDeleteReimbursement() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: reimbursementsApi.remove,
    onSuccess: () => {
      toast.success('Reembolso anulado y movimientos reversados');
      invalidateFinancialViews(queryClient);
    },
    onError: (error) => toast.error(errorMessage(error, 'No se pudo anular el reembolso')),
  });
}

