import { apiClient } from './client';
import type { ApiResponse } from '../types/api.types';
import type { 
  UserInfo, 
  UpdateProfileDto, 
  ChangePasswordDto, 
  ChangeCurrencyDto 
} from '../types/profile.types';

export const profileApi = {
  // GET /api/v1/profile
  getProfile: async () => {
    const response = await apiClient.get<ApiResponse<UserInfo>>('/profile');
    return response.data.data;
  },

  // PUT /api/v1/profile
  updateProfile: async (data: UpdateProfileDto) => {
    const response = await apiClient.put<ApiResponse<UserInfo>>('/profile', data);
    return response.data.data;
  },

  // PATCH /api/v1/profile/password
  changePassword: async (data: ChangePasswordDto) => {
    const response = await apiClient.patch<ApiResponse<null>>('/profile/password', data);
    return response.data; // Retorna todo el objeto por si necesitamos el mensaje
  },

  // PATCH /api/v1/profile/currency
  changeCurrency: async (data: ChangeCurrencyDto) => {
    const response = await apiClient.patch<ApiResponse<UserInfo>>('/profile/currency', data);
    return response.data.data;
  }
};