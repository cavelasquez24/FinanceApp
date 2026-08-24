import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { savingsReplenishmentApi } from "../../../api/savingsReplenishment.api";
import type {
  SavingsReplenishmentCreateDto,
  SavingsReplenishmentManualDebitDto,
  SavingsReplenishmentPauseDto,
} from "../../../types/savingsReplenishment.types";
import { getApiErrorMessage } from "../../../utils/getApiError";

function useRefreshReplenishments() {
  const queryClient = useQueryClient();
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ["savings-replenishments"] }),
      queryClient.invalidateQueries({ queryKey: ["savings-goals"] }),
      queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
      queryClient.invalidateQueries({ queryKey: ["dashboard-current"] }),
    ]);
}

export function useSavingsReplenishmentsByGoal(goalId?: string) {
  return useQuery({
    queryKey: ["savings-replenishments", "goal", goalId],
    queryFn: () => savingsReplenishmentApi.getByGoal(goalId!),
    enabled: Boolean(goalId),
    refetchOnMount: "always",
  });
}

export function useCreateSavingsReplenishment() {
  const refresh = useRefreshReplenishments();
  return useMutation({
    mutationFn: (data: SavingsReplenishmentCreateDto) =>
      savingsReplenishmentApi.create(data),
    onSuccess: () => {
      toast.success("Plan de reposición creado");
      void refresh();
    },
    onError: (error) =>
      toast.error(
        getApiErrorMessage(error, "No se pudo crear el plan de reposición"),
      ),
  });
}

export function useApplyManualDebit() {
  const refresh = useRefreshReplenishments();
  return useMutation({
    mutationFn: ({
      id,
      data,
    }: {
      id: string;
      data: SavingsReplenishmentManualDebitDto;
    }) => savingsReplenishmentApi.manualDebit(id, data),
    onSuccess: () => {
      toast.success("Abono registrado");
      void refresh();
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "No se pudo registrar el abono")),
  });
}

export function usePauseReplenishment() {
  const refresh = useRefreshReplenishments();
  return useMutation({
    mutationFn: ({
      id,
      data,
    }: {
      id: string;
      data: SavingsReplenishmentPauseDto;
    }) => savingsReplenishmentApi.pause(id, data),
    onSuccess: () => {
      toast.success("Plan pausado");
      void refresh();
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "No se pudo pausar el plan")),
  });
}

export function useResumeReplenishment() {
  const refresh = useRefreshReplenishments();
  return useMutation({
    mutationFn: (id: string) => savingsReplenishmentApi.resume(id),
    onSuccess: () => {
      toast.success("Plan reanudado");
      void refresh();
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "No se pudo reanudar el plan")),
  });
}

export function useCancelReplenishment() {
  const refresh = useRefreshReplenishments();
  return useMutation({
    mutationFn: (id: string) => savingsReplenishmentApi.cancel(id),
    onSuccess: () => {
      toast.success("Plan de reposición cancelado");
      void refresh();
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "No se pudo cancelar el plan")),
  });
}
