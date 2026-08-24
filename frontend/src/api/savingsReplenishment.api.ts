import { apiClient } from "./client";
import type { ApiResponse } from "../types/api.types";
import type {
  ReplenishmentCycleResultDto,
  SavingsReplenishmentCreateDto,
  SavingsReplenishmentDto,
  SavingsReplenishmentManualDebitDto,
  SavingsReplenishmentPauseDto,
} from "../types/savingsReplenishment.types";

export const savingsReplenishmentApi = {
  // GET /api/v1/savings-replenishments
  getAll: async () => {
    const response = await apiClient.get<ApiResponse<SavingsReplenishmentDto[]>>(
      "/savings-replenishments",
    );
    return response.data.data;
  },

  // GET /api/v1/savings-replenishments/{id}
  getById: async (id: string) => {
    const response = await apiClient.get<ApiResponse<SavingsReplenishmentDto>>(
      `/savings-replenishments/${id}`,
    );
    return response.data.data;
  },

  // GET /api/v1/savings-replenishments/goal/{goalId}
  getByGoal: async (goalId: string) => {
    const response = await apiClient.get<ApiResponse<SavingsReplenishmentDto[]>>(
      `/savings-replenishments/goal/${goalId}`,
    );
    return response.data.data;
  },

  // POST /api/v1/savings-replenishments
  create: async (data: SavingsReplenishmentCreateDto) => {
    const response = await apiClient.post<ApiResponse<SavingsReplenishmentDto>>(
      "/savings-replenishments",
      data,
    );
    return response.data.data;
  },

  // POST /api/v1/savings-replenishments/{id}/manual-debit
  manualDebit: async (id: string, data: SavingsReplenishmentManualDebitDto) => {
    const response = await apiClient.post<ApiResponse<SavingsReplenishmentDto>>(
      `/savings-replenishments/${id}/manual-debit`,
      data,
    );
    return response.data.data;
  },

  // PATCH /api/v1/savings-replenishments/{id}/pause
  pause: async (id: string, data: SavingsReplenishmentPauseDto) => {
    const response = await apiClient.patch<ApiResponse<SavingsReplenishmentDto>>(
      `/savings-replenishments/${id}/pause`,
      data,
    );
    return response.data.data;
  },

  // PATCH /api/v1/savings-replenishments/{id}/resume
  resume: async (id: string) => {
    const response = await apiClient.patch<ApiResponse<SavingsReplenishmentDto>>(
      `/savings-replenishments/${id}/resume`,
    );
    return response.data.data;
  },

  // DELETE /api/v1/savings-replenishments/{id}
  cancel: async (id: string) => {
    const response = await apiClient.delete<ApiResponse<null>>(
      `/savings-replenishments/${id}`,
    );
    return response.data.data;
  },

  // POST /api/v1/savings-replenishments/execute-cycle
  executeCycle: async () => {
    const response = await apiClient.post<ApiResponse<ReplenishmentCycleResultDto>>(
      "/savings-replenishments/execute-cycle",
    );
    return response.data.data;
  },
};
