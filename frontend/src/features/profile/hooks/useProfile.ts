import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { profileApi } from '../../../api/profile.api';
import toast from 'react-hot-toast';
import type {
  UpdateProfileDto,
  ChangePasswordDto,
  ChangeCurrencyDto,
  UpdatePaydayDto,
} from '../../../types/profile.types';

export function useProfile() {
  return useQuery({
    queryKey: ['profile'],
    queryFn: () => profileApi.getProfile(),
  });
}

export function useUpdateProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateProfileDto) => profileApi.updateProfile(data),
    onSuccess: () => {
      toast.success('Perfil actualizado exitosamente');
      queryClient.invalidateQueries({ queryKey: ['profile'] });
    },
    onError: () => toast.error('Error al actualizar el perfil'),
  });
}

export function useChangePassword() {
  return useMutation({
    mutationFn: (data: ChangePasswordDto) => profileApi.changePassword(data),
    onSuccess: () => toast.success('Contraseña actualizada exitosamente'),
    onError: (error: any) => {
      const message = error.response?.data?.error?.message || 'Error al cambiar la contraseña';
      toast.error(message);
    },
  });
}

export function useChangeCurrency() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: ChangeCurrencyDto) => profileApi.changeCurrency(data),
    onSuccess: () => {
      toast.success('Moneda actualizada exitosamente');
      queryClient.invalidateQueries({ queryKey: ['profile'] });
      queryClient.invalidateQueries();
    },
    onError: () => toast.error('Error al actualizar la moneda'),
  });
}

export function useUpdatePayday() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdatePaydayDto) => profileApi.updatePayday(data),
    onSuccess: () => {
      toast.success('Día de pago actualizado exitosamente');
      queryClient.invalidateQueries({ queryKey: ['profile'] });
      // Dashboard depende del ciclo, recalcular
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: (error: any) => {
      const message = error.response?.data?.error?.message || 'Error al actualizar el día de pago';
      toast.error(message);
    },
  });
}