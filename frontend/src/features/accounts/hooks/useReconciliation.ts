import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { accountsApi } from "../../../api/accounts.api";
import toast from "react-hot-toast";
import type { ReconciliationCreateDto } from "../../../types/account.types";
import { getApiErrorMessage } from "../../../utils/getApiError";

export function useReconciliationPreview(accountId: string | null) {
  return useQuery({
    queryKey: ["reconciliation-preview", accountId],
    queryFn: () => accountsApi.getReconciliationPreview(accountId!),
    enabled: !!accountId,
  });
}

export function useApplyReconciliation(onSuccess?: () => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      accountId,
      dto,
    }: {
      accountId: string;
      dto: ReconciliationCreateDto;
    }) => accountsApi.applyReconciliation(accountId, dto),
    onSuccess: (_, { accountId }) => {
      toast.success("Conciliación aplicada correctamente");
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      queryClient.invalidateQueries({ queryKey: ["reconciliation-preview", accountId] });
      queryClient.invalidateQueries({ queryKey: ["reconciliation-history", accountId] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      onSuccess?.();
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Error al aplicar la conciliación")),
  });
}

export function useReconciliationHistory(accountId: string | null, page = 1) {
  return useQuery({
    queryKey: ["reconciliation-history", accountId, page],
    queryFn: () => accountsApi.getReconciliationHistory(accountId!, page),
    enabled: !!accountId,
  });
}
