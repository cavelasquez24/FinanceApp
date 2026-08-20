import { useEffect, useState } from "react";
import { ArrowUpRight, CalendarDays } from "lucide-react";
import { useDepositSavings, useSavingsGoals } from "../hooks/useSavings";
import type { SavingsGoal } from "../../../types/savings.types";
import { todayDateOnly } from "../../../utils/dateOnly";
import { formatCurrency } from "../../../utils/formatCurrency";
import { useAccounts } from "../../accounts/hooks/useAccounts";
import {
  SavingsField,
  SavingsMetric,
  SavingsModalActions,
  SavingsModalSection,
  SavingsModalShell,
  savingsInputClass,
} from "./SavingsModalShell";

interface Props {
  goal: SavingsGoal;
  onClose: () => void;
}

export default function DepositModal({ goal, onClose }: Props) {
  const [amount, setAmount] = useState<number | "">("");
  const [contributionDate, setContributionDate] = useState(todayDateOnly());
  const [notes, setNotes] = useState("");
  const [fundingMode, setFundingMode] = useState<
    "existing_balance" | "account_transfer"
  >("account_transfer");
  const [sourceAccountId, setSourceAccountId] = useState("");
  const { mutate: deposit, isPending } = useDepositSavings();
  const { data: accounts } = useAccounts();
  const { data: goals } = useSavingsGoals();
  const sourceAccounts =
    accounts?.filter(
      (account) =>
        account.isActive &&
        (account.type === "cash" || account.type === "savings") &&
        account.id !== goal.savingsAccountId,
    ) ?? [];
  useEffect(() => {
    if (sourceAccountId || fundingMode !== "account_transfer") return;
    const preferred =
      sourceAccounts.find(
        (account) => account.type === "cash" && account.isDefault,
      ) ?? sourceAccounts[0];
    if (preferred) setSourceAccountId(preferred.id);
  }, [fundingMode, sourceAccountId, sourceAccounts]);
  const allocatedInAccount = (goals ?? [])
    .filter((item) => item.savingsAccountId === goal.savingsAccountId)
    .reduce((sum, item) => sum + item.currentAmount, 0);
  const unallocatedBalance = Math.max(
    0,
    (goal.savingsAccountBalance ?? 0) - allocatedInAccount,
  );
  const remaining = Math.max(0, goal.targetAmount - goal.currentAmount);
  const numericAmount = Number(amount || 0);
  const resultingAmount = goal.currentAmount + numericAmount;
  const isValid =
    numericAmount > 0 &&
    numericAmount <= remaining &&
    (fundingMode === "existing_balance"
      ? numericAmount <= unallocatedBalance
      : Boolean(sourceAccountId));

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!isValid) return;
    deposit(
      {
        id: goal.id,
        data: {
          amount: numericAmount,
          fundingMode,
          sourceAccountId: fundingMode === "account_transfer" ? sourceAccountId : undefined,
          contributionDate,
          idempotencyKey: crypto.randomUUID(),
          notes: notes.trim() || undefined,
        },
      },
      { onSuccess: onClose },
    );
  };

  return (
    <SavingsModalShell
      eyebrow="Movimiento de meta"
      title={`Aportar a ${goal.name}`}
      description="Asigna saldo existente o transfiere dinero real hacia la cuenta de ahorro."
      onClose={onClose}
    >
      <form onSubmit={handleSubmit} className="space-y-6 p-6 sm:p-8">
        <SavingsModalSection
          icon={<ArrowUpRight className="h-5 w-5" />}
          title="Registra el aporte"
          description={`Respaldada por ${goal.savingsAccountName ?? "tu cuenta de ahorro"}.`}
        >
          <div className="mb-4 grid grid-cols-2 gap-2">
            <button
              type="button"
              onClick={() => {
                setFundingMode("existing_balance");
                setSourceAccountId("");
              }}
              className={`rounded-2xl border p-3 text-left text-sm ${fundingMode === "existing_balance" ? "border-[#9EAB98] bg-[#F3F7F0] text-[#304B38]" : "border-[#E8E1D8] bg-white text-[#6D665F]"}`}
            >
              <strong className="block">Asignar saldo existente</strong>
              <span className="mt-1 block text-xs">
                Disponible sin asignar: {formatCurrency(unallocatedBalance)}
              </span>
            </button>
            <button
              type="button"
              onClick={() => setFundingMode("account_transfer")}
              className={`rounded-2xl border p-3 text-left text-sm ${fundingMode === "account_transfer" ? "border-[#9EAB98] bg-[#F3F7F0] text-[#304B38]" : "border-[#E8E1D8] bg-white text-[#6D665F]"}`}
            >
              <strong className="block">Transferir nuevo ahorro</strong>
              <span className="mt-1 block text-xs">
                Aumenta el saldo de {goal.savingsAccountName ?? "Savings"}.
              </span>
            </button>
          </div>
          {fundingMode === "account_transfer" && (
            <SavingsField label="Cuenta de origen" className="mb-4">
              <select
                required
                value={sourceAccountId}
                onChange={(event) => setSourceAccountId(event.target.value)}
                className={savingsInputClass}
              >
                <option value="">Selecciona la cuenta de origen</option>
                {sourceAccounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.name} · {formatCurrency(account.currentBalance)}
                  </option>
                ))}
              </select>
            </SavingsField>
          )}
          <div className="grid gap-4 sm:grid-cols-2">
            <SavingsField label="Monto del aporte">
              <input
                type="number"
                step="0.01"
                min="0.01"
                max={remaining}
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
            <SavingsField label="Fecha">
              <input
                type="date"
                required
                value={contributionDate}
                onChange={(event) => setContributionDate(event.target.value)}
                className={savingsInputClass}
              />
            </SavingsField>
            <SavingsField label="Nota opcional" className="sm:col-span-2">
              <textarea
                rows={2}
                maxLength={200}
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
                className={`${savingsInputClass} resize-none`}
              />
            </SavingsField>
          </div>
        </SavingsModalSection>
        <SavingsModalSection
          icon={<CalendarDays className="h-5 w-5" />}
          title="Resultado"
          description="Revisa cómo cambiará el progreso antes de confirmar."
        >
          <div className="grid gap-3 sm:grid-cols-3">
            <SavingsMetric
              label="Asignado ahora"
              value={formatCurrency(goal.currentAmount)}
            />
            <SavingsMetric
              label="Después del aporte"
              value={formatCurrency(resultingAmount)}
              tone="success"
            />
            <SavingsMetric
              label="Pendiente"
              value={formatCurrency(
                Math.max(0, goal.targetAmount - resultingAmount),
              )}
            />
          </div>
          {numericAmount > remaining && (
            <p className="mt-3 rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
              El aporte supera el monto restante de la meta.
            </p>
          )}
        </SavingsModalSection>
        <SavingsModalActions
          onClose={onClose}
          isPending={isPending}
          submitLabel="Registrar aporte"
          disabled={!isValid}
        />
      </form>
    </SavingsModalShell>
  );
}
