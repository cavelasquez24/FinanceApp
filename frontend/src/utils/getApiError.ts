import type { AxiosError } from 'axios';
import type { ApiResponse } from '../types/api.types';

type ApiErrorPayload = ApiResponse<unknown> & { message?: string | null };

export function getApiErrorMessage(error: unknown, fallback: string) {
  const payload = (error as AxiosError<ApiErrorPayload>).response?.data;
  return payload?.error?.message ?? payload?.message ?? fallback;
}

export function getApiErrorCode(error: unknown) {
  return (error as AxiosError<ApiErrorPayload>).response?.data?.error?.code;
}
