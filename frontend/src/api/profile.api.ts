import { apiClient } from './client';
import type { ApiResponse } from '../types/api.types';
import type {
  UserInfo,
  UpdateProfileDto,
  ChangePasswordDto,
  ChangeCurrencyDto,
  UpdatePaydayDto,
} from '../types/profile.types';

export const profileApi = {
  getProfile: async () => {
    const response = await apiClient.get<ApiResponse<UserInfo>>('/profile');
    return response.data.data;
  },

  updateProfile: async (data: UpdateProfileDto) => {
    const response = await apiClient.put<ApiResponse<UserInfo>>('/profile', data);
    return response.data.data;
  },

  changePassword: async (data: ChangePasswordDto) => {
    const response = await apiClient.patch<ApiResponse<null>>('/profile/password', data);
    return response.data;
  },

  changeCurrency: async (data: ChangeCurrencyDto) => {
    const response = await apiClient.patch<ApiResponse<UserInfo>>('/profile/currency', data);
    return response.data.data;
  },

  updatePayday: async (data: UpdatePaydayDto) => {
    const response = await apiClient.patch<ApiResponse<UserInfo>>('/profile/payday', data);
    return response.data.data;
  },
};