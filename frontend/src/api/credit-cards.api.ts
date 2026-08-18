import { apiClient } from './client';
import type { ApiResponse } from '../types/api.types';
import type {
  CreateCreditCardDto,
  CreditCard,
  CreditCardChargeDto,
  CreditCardPayment,
  CreditCardPaymentDto,
  CreditCardPaymentVoidDto,
  CreditCardTransaction,
  UpdateCreditCardDto,
} from '../types/credit-card.types';

export const creditCardsApi = {
  getAll: async () => {
    const response = await apiClient.get<ApiResponse<CreditCard[]>>('/credit-cards');
    return response.data.data;
  },
  getTransactions: async (id: string) => {
    const response = await apiClient.get<ApiResponse<CreditCardTransaction[]>>(
      `/credit-cards/${id}/transactions`
    );
    return response.data.data;
  },
  getPayments: async (id: string) => {
    const response = await apiClient.get<ApiResponse<CreditCardPayment[]>>(
      `/credit-cards/${id}/payments`
    );
    return response.data.data;
  },
  create: async (dto: CreateCreditCardDto) => {
    const response = await apiClient.post<ApiResponse<CreditCard>>('/credit-cards', dto);
    return response.data.data;
  },
  update: async (id: string, dto: UpdateCreditCardDto) => {
    const response = await apiClient.put<ApiResponse<CreditCard>>(`/credit-cards/${id}`, dto);
    return response.data.data;
  },
  pay: async (id: string, dto: CreditCardPaymentDto) => {
    const response = await apiClient.post<ApiResponse<CreditCardPayment>>(
      `/credit-cards/${id}/payments`, dto
    );
    return response.data.data;
  },
  voidPayment: async (id: string, paymentId: string, dto: CreditCardPaymentVoidDto) => {
    const response = await apiClient.post<ApiResponse<CreditCardPayment>>(
      `/credit-cards/${id}/payments/${paymentId}/void`, dto
    );
    return response.data.data;
  },
  addCharge: async (id: string, dto: CreditCardChargeDto) => {
    const response = await apiClient.post<ApiResponse<CreditCardTransaction>>(
      `/credit-cards/${id}/charges`, dto
    );
    return response.data.data;
  },
};
