import { useMemo, useState } from "react";
import { PiggyBank } from "lucide-react";
import { useAccounts } from "../../accounts/hooks/useAccounts";
import { useCreateSavingsReplenishment } from "../hooks/useSavingsReplenishments";
import { formatCurrency } from "../../../utils/formatCurrency";
import { formatMonthYear } from "../../../utils/formatDate";
import { getApiErrorMessage } from "../../../utils/getApiError";
import {
  SavingsField,
  SavingsMetric,
  SavingsModalActions,
  SavingsModalSection,
  savingsInputClass,
} from "./SavingsModalShell";

interface Props {
  goalId: string;
  goalName: string;
  savingsAccountId?: string | null;
  amountTaken: number;
  /** Solo relevante si este formulario se reutiliza para editar un plan
   *  existente en vez de crear uno nuevo — el ETA debe calcularse sobre
   *  el pendiente real, no sobre el monto total tomado. */
  amountReplenished?: number;
  defaultSourceAccountId?: string;
  onSuccess: () => void;
  onCancel: () => void;
}

const capitalize = (value: string) =>
  value.length > 0 ? value[0].toUpperCase() + value.slice(1) : value;

export function ReplenishmentCreateForm({
  goalId,
  goalName,
  savingsAccountId,
  amountTaken,
  amountReplenished = 0,
  defaultSourceAccountId,
  onSuccess,
  onCancel,
}: Props) {
  const today = new Date();
  const [name, setName] = useState(
    `Reposición — ${goalName} ${capitalize(formatMonthYear(today.getMonth() + 1, today.getFullYear()))}`,
  );
  const [amount, setAmount] = useState<number | "">(amountTaken);
  const [sourceAccountId, setSourceAccountId] = useState(
    defaultSourceAccountId ?? "",
  );
  const [monthlyDebit, setMonthlyDebit] = useState<number | "">("");
  const [autoDebitEnabled, setAutoDebitEnabled] = useState(true);

  const { data: accounts } = useAccounts();
  const sourceAccounts =
    accounts?.filter(
      (account) =>
        account.isActive &&
        (account.type === "cash" || account.type === "savings") &&
        account.id !== savingsAccountId,
    ) ?? [];

  const createMutation = useCreateSavingsReplenishment();

  const numericAmount = Number(amount || 0);
  const numericMonthlyDebit = Number(monthlyDebit || 0);
  const pendingAmount = Math.max(0, numericAmount - amountReplenished);
  const estimatedCycles = useMemo(
    () =>
      numericMonthlyDebit > 0
        ? Math.ceil(pendingAmount / numericMonthlyDebit)
        : 0,
    [pendingAmount, numericMonthlyDebit],
  );

  const isValid =
    name.trim().length > 0 &&
    Boolean(sourceAccountId) &&
    numericAmount > 0 &&
    numericMonthlyDebit > 0 &&
    numericMonthlyDebit <= numericAmount;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!isValid) return;
    createMutation.mutate(
      {
        savingsGoalId: goalId,
        sourceAccountId,
        name: name.trim(),
        amountTaken: numericAmount,
        monthlyDebitAmount: numericMonthlyDebit,
        autoDebitEnabled,
      },
      { onSuccess },
    );
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <SavingsModalSection
        icon={<PiggyBank className="h-5 w-5" />}
        title="Programa la reposición"
        description="Un débito automático repondrá este monto por ciclo, sin afectar tu patrimonio."
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <SavingsField label="Nombre del plan" className="sm:col-span-2">
            <input
              required
              maxLength={200}
              value={name}
              onChange={(event) => setName(event.target.value)}
              className={savingsInputClass}
            />
          </SavingsField>
          <SavingsField label="Monto tomado">
            <input
              type="number"
              min="0.01"
              step="0.01"
              required
              value={amount}
              onChange={(event) =>
                setAmount(
                  event.target.value === "" ? "" : Number(event.target.value),
                )
              }
              className={savingsInputClass}
            />
          </SavingsField>
          <SavingsField label="Débito por ciclo">
            <input
              type="number"
              min="0.01"
              step="0.01"
              max={numericAmount || undefined}
              required
              value={monthlyDebit}
              onChange={(event) =>
                setMonthlyDebit(
                  event.target.value === "" ? "" : Number(event.target.value),
                )
              }
              className={savingsInputClass}
            />
          </SavingsField>
          <SavingsField label="Cuenta origen del débito" className="sm:col-span-2">
            <select
              required
              value={sourceAccountId}
              onChange={(event) => setSourceAccountId(event.target.value)}
              className={savingsInputClass}
            >
              <option value="">Selecciona la cuenta que financiará el débito</option>
              {sourceAccounts.map((account) => (
                <option key={account.id} value={account.id}>
                  {account.name} · {formatCurrency(account.currentBalance)}
                </option>
              ))}
            </select>
          </SavingsField>
          <label className="flex items-center gap-2 text-sm text-[#5F5953] sm:col-span-2">
            <input
              type="checkbox"
              checked={autoDebitEnabled}
              onChange={(event) => setAutoDebitEnabled(event.target.checked)}
            />
            Activar débito automático por ciclo
          </label>
        </div>
      </SavingsModalSection>

      <div className="grid gap-3 sm:grid-cols-2">
        <SavingsMetric
          label="Pendiente por reponer"
          value={formatCurrency(pendingAmount)}
        />
        <SavingsMetric
          label="Ciclos estimados"
          value={
            estimatedCycles > 0
              ? `~${estimatedCycles} ciclo${estimatedCycles === 1 ? "" : "s"}`
              : "—"
          }
          tone="success"
        />
      </div>

      {createMutation.error && (
        <p className="rounded-xl bg-red-50 p-3 text-sm text-red-700">
          {getApiErrorMessage(
            createMutation.error,
            "No se pudo crear el plan de reposición.",
          )}
        </p>
      )}

      <SavingsModalActions
        onClose={onCancel}
        isPending={createMutation.isPending}
        submitLabel="Crear plan de reposición"
        disabled={!isValid}
      />
    </form>
  );
}
