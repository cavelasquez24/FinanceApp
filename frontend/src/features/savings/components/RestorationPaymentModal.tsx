import { useEffect, useState } from "react";
import { ArrowUpRight, CalendarCheck2 } from "lucide-react";
import { useRegisterRestorationPayment } from "../hooks/useEmergencyFundRestorations";
import type { EmergencyFundRestoration, SavingsGoal } from "../../../types/savings.types";
import { formatCurrency } from "../../../utils/formatCurrency";
import { getApiErrorMessage } from "../../../utils/getApiError";
import { getLocalToday } from "../utils/restorationPlan";
import { useAccounts } from "../../accounts/hooks/useAccounts";
import { useSavingsGoals } from "../hooks/useSavings";
import {
  SavingsField,
  SavingsMetric,
  SavingsModalActions,
  SavingsModalSection,
  SavingsModalShell,
  savingsInputClass,
} from "./SavingsModalShell";

interface Props {
  restoration: EmergencyFundRestoration;
  onClose: () => void;
  goal: SavingsGoal;
}

export default function RestorationPaymentModal({
  restoration,
  onClose,
  goal,
}: Props) {
  const [amount, setAmount] = useState<number | "">(
    restoration.nextContributionAmount,
  );
  const [paymentDate, setPaymentDate] = useState(getLocalToday());
  const [notes, setNotes] = useState("");
  const [fundingMode, setFundingMode] = useState<
    "existing_balance" | "account_transfer"
  >("account_transfer");
  const [sourceAccountId, setSourceAccountId] = useState("");
  const paymentMutation = useRegisterRestorationPayment();
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
  const numericAmount = Number(amount || 0);
  const resultingOutstanding = Math.max(
    0,
    restoration.outstandingAmount - numericAmount,
  );
  const isValid =
    numericAmount > 0 &&
    numericAmount <= restoration.outstandingAmount &&
    paymentDate >= restoration.acquisitionDate &&
    paymentDate <= getLocalToday() &&
    (fundingMode === "existing_balance"
      ? numericAmount <= unallocatedBalance
      : Boolean(sourceAccountId));

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!isValid) return;
    paymentMutation.mutate(
      {
        restorationId: restoration.id,
        data: {
          amount: numericAmount,
          paymentDate,
          fundingMode,
          sourceAccountId: fundingMode === "account_transfer" ? sourceAccountId : undefined,
          idempotencyKey: crypto.randomUUID(),
          notes: notes.trim() || undefined,
        },
      },
      { onSuccess: onClose },
    );
  };

  return (
    <SavingsModalShell
      eyebrow="Compromiso interno"
      title="Registrar reposición"
      description="Repone el fondo con saldo existente o transfiriendo dinero real hacia la cuenta de ahorro."
      onClose={onClose}
    >
      <form onSubmit={handleSubmit} className="space-y-6 p-6 sm:p-8">
        <SavingsModalSection
          icon={<ArrowUpRight className="h-5 w-5" />}
          title={restoration.description}
          description="Registra el avance real de tu plan de reposición."
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
                Sin asignar: {formatCurrency(unallocatedBalance)}
              </span>
            </button>
            <button
              type="button"
              onClick={() => setFundingMode("account_transfer")}
              className={`rounded-2xl border p-3 text-left text-sm ${fundingMode === "account_transfer" ? "border-[#9EAB98] bg-[#F3F7F0] text-[#304B38]" : "border-[#E8E1D8] bg-white text-[#6D665F]"}`}
            >
              <strong className="block">Transferir a ahorro</strong>
              <span className="mt-1 block text-xs">
                Mueve dinero real desde otra cuenta.
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
                <option value="">Selecciona una cuenta</option>
                {sourceAccounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.name} · {formatCurrency(account.currentBalance)}
                  </option>
                ))}
              </select>
            </SavingsField>
          )}
          <div className="grid gap-4 sm:grid-cols-2">
            <SavingsField label="Monto">
              <input
                type="number"
                min="0.01"
                max={restoration.outstandingAmount}
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
            <SavingsField label="Fecha">
              <input
                type="date"
                required
                min={restoration.acquisitionDate}
                max={getLocalToday()}
                value={paymentDate}
                onChange={(event) => setPaymentDate(event.target.value)}
                className={savingsInputClass}
              />
            </SavingsField>
            <SavingsField label="Nota opcional" className="sm:col-span-2">
              <textarea
                rows={2}
                maxLength={500}
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
                className={`${savingsInputClass} resize-none`}
              />
            </SavingsField>
          </div>
        </SavingsModalSection>
        <SavingsModalSection
          icon={<CalendarCheck2 className="h-5 w-5" />}
          title="Resultado"
        >
          <div className="grid gap-3 sm:grid-cols-3">
            <SavingsMetric
              label="Pendiente actual"
              value={formatCurrency(restoration.outstandingAmount)}
            />
            <SavingsMetric
              label="Reposición"
              value={formatCurrency(numericAmount)}
              tone="success"
            />
            <SavingsMetric
              label="Pendiente posterior"
              value={formatCurrency(resultingOutstanding)}
            />
          </div>
        </SavingsModalSection>
        {paymentMutation.error && (
          <p className="rounded-xl bg-red-50 p-3 text-sm text-red-700">
            {getApiErrorMessage(
              paymentMutation.error,
              "No se pudo registrar la reposición.",
            )}
          </p>
        )}
        <SavingsModalActions
          onClose={onClose}
          isPending={paymentMutation.isPending}
          submitLabel="Registrar reposición"
          disabled={!isValid}
        />
      </form>
    </SavingsModalShell>
  );
}
