import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { savingsApi } from "../../../api/savings.api";
import type {
  EmergencyFundRestorationPaymentDto,
  EmergencyFundUseDto,
} from "../../../types/savings.types";
import { getApiErrorMessage } from "../../../utils/getApiError";

function useRefreshFinancialData() {
  const queryClient = useQueryClient();
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ["savings-goals"] }),
      queryClient.invalidateQueries({
        queryKey: ["emergency-fund-restorations"],
      }),
      queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
    ]);
}

export function useEmergencyFundRestorations(goalId?: string) {
  return useQuery({
    queryKey: ["emergency-fund-restorations", goalId],
    queryFn: () => savingsApi.getRestorations(goalId!),
    enabled: Boolean(goalId),
    refetchOnMount: "always",
  });
}

export function useCreateEmergencyFundUse() {
  const refresh = useRefreshFinancialData();
  return useMutation({
    mutationFn: ({
      goalId,
      data,
    }: {
      goalId: string;
      data: EmergencyFundUseDto;
    }) => savingsApi.createEmergencyFundUse(goalId, data),
    onSuccess: () => {
      toast.success("Uso del fondo y plan de reposición registrados");
      void refresh();
    },
    onError: (error) =>
      toast.error(
        getApiErrorMessage(error, "No se pudo registrar el uso del fondo"),
      ),
  });
}

export function useRegisterRestorationPayment() {
  const refresh = useRefreshFinancialData();
  return useMutation({
    mutationFn: ({
      restorationId,
      data,
    }: {
      restorationId: string;
      data: EmergencyFundRestorationPaymentDto;
    }) => savingsApi.registerRestorationPayment(restorationId, data),
    onSuccess: () => {
      toast.success("Reposición registrada");
      void refresh();
    },
    onError: (error) =>
      toast.error(
        getApiErrorMessage(error, "No se pudo registrar la reposición"),
      ),
  });
}

export function useCancelRestoration() {
  const refresh = useRefreshFinancialData();
  return useMutation({
    mutationFn: savingsApi.cancelRestoration,
    onSuccess: () => {
      toast.success("Compromiso de restauración cancelado");
      void refresh();
    },
    onError: (error) =>
      toast.error(
        getApiErrorMessage(error, "No se pudo cancelar la restauración"),
      ),
  });
}
