import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { investmentsApi } from '../../../api/investments.api';
import toast from 'react-hot-toast';
import type { CreateInvestmentDto, UpdateInvestmentDto, CreateInvestmentRecordDto } from '../../../types/investment.types';

export function useInvestments() {
  return useQuery({
    queryKey: ['investments'],
    queryFn: () => investmentsApi.getAll(),
  });
}

export function useInvestmentSummary() {
  return useQuery({
    queryKey: ['investments', 'summary'],
    queryFn: () => investmentsApi.getSummary(),
  });
}

export function useCreateInvestment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateInvestmentDto) => investmentsApi.create(data),
    onSuccess: () => {
      toast.success('Inversión registrada exitosamente');
      queryClient.invalidateQueries({ queryKey: ['investments'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: () => toast.error('Error al registrar la inversión'),
  });
}

export function useAddInvestmentRecord() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CreateInvestmentRecordDto }) => 
      investmentsApi.addRecord(id, data),
    onSuccess: (_, variables) => {
      toast.success('Registro de valor agregado exitosamente');
      queryClient.invalidateQueries({ queryKey: ['investments'] });
      queryClient.invalidateQueries({ queryKey: ['investments', variables.id, 'records'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: () => toast.error('Error al agregar el registro'),
  });
}

export function useInvestment(id: string) {
  return useQuery({
    queryKey: ['investments', id],
    queryFn: () => investmentsApi.getById(id),
    enabled: !!id,
  });
}

export function useUpdateInvestment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateInvestmentDto }) => investmentsApi.update(id, data),
    onSuccess: () => {
      toast.success('Inversión actualizada exitosamente');
      queryClient.invalidateQueries({ queryKey: ['investments'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: () => toast.error('Error al actualizar la inversión'),
  });
}

export function useDeleteInvestment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => investmentsApi.delete(id),
    onSuccess: () => {
      toast.success('Inversión eliminada exitosamente');
      queryClient.invalidateQueries({ queryKey: ['investments'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: () => toast.error('Error al eliminar la inversión'),
  });
}