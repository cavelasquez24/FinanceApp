import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { profileApi } from '../../../api/profile.api';
import toast from 'react-hot-toast';
import type { UpdateProfileDto, ChangePasswordDto, ChangeCurrencyDto } from '../../../types/profile.types';

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
      // Invalidar todos los queries que dependen de la moneda (dashboard, gastos, ingresos, etc)
      queryClient.invalidateQueries(); 
    },
    onError: () => toast.error('Error al actualizar la moneda'),
  });
}