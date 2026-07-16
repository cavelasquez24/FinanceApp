import type { ApiResponse } from '../types/api.types';
import type { AuthResponse, LoginDto, RegisterDto } from '../types/auth.types';
import { apiClient } from './client';

export const authApi = {
  register: (dto: RegisterDto) =>
    apiClient.post<ApiResponse<AuthResponse>>('/auth/register', dto),

  login: (dto: LoginDto) =>
    apiClient.post<ApiResponse<AuthResponse>>('/auth/login', dto),

  refreshToken: (refreshToken: string) =>
    apiClient.post<ApiResponse<AuthResponse>>('/auth/refresh-token', {
      refreshToken,
    }),

  logout: () =>
    apiClient.post<ApiResponse<null>>('/auth/logout'),

  getMe: () =>
    apiClient.get<ApiResponse<AuthResponse['user']>>('/auth/me'),
};