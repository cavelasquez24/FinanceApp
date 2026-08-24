import { useState } from "react";
import { ArrowDownLeft, ArrowRightLeft, SlidersHorizontal } from "lucide-react";
import { useSavingsGoals, useWithdrawSavings } from "../hooks/useSavings";
import type {
  SavingsGoal,
  SavingsWithdrawalReason,
} from "../../../types/savings.types";
import { todayDateOnly } from "../../../utils/dateOnly";
import { formatCurrency } from "../../../utils/formatCurrency";
import { useAccounts } from "../../accounts/hooks/useAccounts";
import { useCategories } from "../../categories/hooks/useCategories";
import { ReplenishmentCreateForm } from "./ReplenishmentCreateForm";
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

type WithdrawalAction =
  | "transfer"
  | "expense"
  | "reassign"
  | "release"
  | "correction"
  | "loan";

const actions: Array<{
  value: WithdrawalAction;
  label: string;
  hint: string;
}> = [
  {
    value: "transfer",
    label: "Transferir a otra cuenta",
    hint: "Reduce la meta y mueve el dinero desde la cuenta de ahorro.",
  },
  {
    value: "loan",
    label: "Préstamo temporal a mí mismo",
    hint: "Mueve el dinero a una cuenta operativa y programa un plan de reposición automática.",
  },
  {
    value: "expense",
    label: "Consumir ahorro",
    hint: "Reduce la meta, la cuenta de ahorro y registra el gasto una sola vez.",
  },
  {
    value: "reassign",
    label: "Reasignar a otra meta",
    hint: "Cambia el propósito sin mover el dinero físico.",
  },
  {
    value: "release",
    label: "Liberar asignación",
    hint: "El dinero permanece en ahorro y queda disponible para otra meta.",
  },
  {
    value: "correction",
    label: "Corregir saldo",
    hint: "Ajuste administrativo con motivo obligatorio.",
  },
];

export default function WithdrawModal({ goal, onClose }: Props) {
  const [amount, setAmount] = useState<number | "">("");
  const [action, setAction] = useState<WithdrawalAction>("transfer");
  const [targetGoalId, setTargetGoalId] = useState("");
  const [destinationAccountId, setDestinationAccountId] = useState("");
  const [expenseCategoryId, setExpenseCategoryId] = useState("");
  const [expenseDescription, setExpenseDescription] = useState("");
  const [notes, setNotes] = useState("");
  const [withdrawalDate, setWithdrawalDate] = useState(todayDateOnly());
  const [loanWithdrawal, setLoanWithdrawal] = useState<
    { amount: number; destinationAccountId: string } | null
  >(null);
  const { data: goals } = useSavingsGoals();
  const { data: accounts } = useAccounts();
  const { data: categories } = useCategories("expense");
  const { mutate: withdraw, isPending } = useWithdrawSavings();
  const numericAmount = Number(amount || 0);
  const destinationAccounts =
    accounts?.filter(
      (account) =>
        account.isActive &&
        (account.type === "cash" || account.type === "savings") &&
        account.id !== goal.savingsAccountId,
    ) ?? [];
  const targetGoals = (goals ?? []).filter(
    (item) =>
      item.id !== goal.id &&
      item.savingsAccountId === goal.savingsAccountId &&
      !item.isCompleted &&
      item.targetAmount - item.currentAmount >= numericAmount,
  );
  const reason: SavingsWithdrawalReason =
    action === "expense"
      ? "Consumed"
      : action === "reassign"
        ? "ReallocatedToOtherGoal"
        : action === "correction"
          ? "Correction"
          : action === "loan"
            ? "TemporaryLoan"
            : "ReallocatedToLiquid";
  const isValid =
    numericAmount > 0 &&
    numericAmount <= goal.currentAmount &&
    (action !== "transfer" || Boolean(destinationAccountId)) &&
    (action !== "loan" || Boolean(destinationAccountId)) &&
    (action !== "expense" ||
      (Boolean(expenseCategoryId) && Boolean(expenseDescription.trim()))) &&
    (action !== "reassign" || Boolean(targetGoalId)) &&
    (action !== "correction" || Boolean(notes.trim()));
  const selectedAction = actions.find((item) => item.value === action)!;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!isValid) return;
    withdraw(
      {
        id: goal.id,
        data: {
          amount: numericAmount,
          reason,
          withdrawalDate,
          targetGoalId: reason === "ReallocatedToOtherGoal" ? targetGoalId : undefined,
          destinationAccountId:
            action === "transfer" || action === "loan"
              ? destinationAccountId
              : undefined,
          expenseCategoryId: action === "expense" ? expenseCategoryId : undefined,
          expenseDescription: action === "expense" ? expenseDescription.trim() : undefined,
          idempotencyKey: crypto.randomUUID(),
          notes: notes.trim() || undefined,
        },
      },
      {
        onSuccess: () => {
          if (action === "loan") {
            // Segundo paso: programar la reposición en el mismo modal,
            // en vez de cerrarlo — el retiro ya se registró.
            setLoanWithdrawal({ amount: numericAmount, destinationAccountId });
          } else {
            onClose();
          }
        },
      },
    );
  };

  if (loanWithdrawal) {
    return (
      <SavingsModalShell
        eyebrow="Movimiento de meta"
        title="¿Cómo quieres reponer este dinero?"
        description="El retiro ya se registró. Programa un débito automático por ciclo hasta saldar el pendiente."
        onClose={onClose}
      >
        <div className="p-6 sm:p-8">
          <ReplenishmentCreateForm
            goalId={goal.id}
            goalName={goal.name}
            savingsAccountId={goal.savingsAccountId}
            amountTaken={loanWithdrawal.amount}
            defaultSourceAccountId={loanWithdrawal.destinationAccountId}
            onSuccess={onClose}
            onCancel={onClose}
          />
        </div>
      </SavingsModalShell>
    );
  }

  return (
    <SavingsModalShell
      eyebrow="Movimiento de meta"
      title={`Retirar de ${goal.name}`}
      description="Mueve dinero real, registra un gasto o cambia únicamente su asignación según la opción elegida."
      onClose={onClose}
    >
      <form onSubmit={handleSubmit} className="space-y-6 p-6 sm:p-8">
        <SavingsModalSection
          icon={<ArrowDownLeft className="h-5 w-5" />}
          number="1"
          title="Define el movimiento"
          description="Elige si quieres liberar la asignación, moverla o corregirla."
        >
          <div className="grid gap-2 sm:grid-cols-2">
            {actions.map((item) => (
              <button
                key={item.value}
                type="button"
                onClick={() => {
                  setAction(item.value);
                  setTargetGoalId("");
                  setDestinationAccountId("");
                  setExpenseCategoryId("");
                  setExpenseDescription("");
                }}
                className={`rounded-2xl border p-3 text-left text-sm transition ${action === item.value ? "border-[#9EAB98] bg-[#F3F7F0] text-[#304B38]" : "border-[#E8E1D8] bg-white/70 text-[#6D665F]"}`}
              >
                <strong className="block">{item.label}</strong>
              </button>
            ))}
          </div>
          <p className="mt-3 text-xs text-finflow-muted">{selectedAction.hint}</p>
        </SavingsModalSection>
        <SavingsModalSection
          icon={
            action === "reassign" ? (
              <ArrowRightLeft className="h-5 w-5" />
            ) : (
              <SlidersHorizontal className="h-5 w-5" />
            )
          }
          number="2"
          title="Completa los datos"
        >
          <div className="grid gap-4 sm:grid-cols-2">
            <SavingsField label="Monto">
              <input
                type="number"
                step="0.01"
                min="0.01"
                max={goal.currentAmount}
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
                value={withdrawalDate}
                onChange={(event) => setWithdrawalDate(event.target.value)}
                className={savingsInputClass}
              />
            </SavingsField>
            {(action === "transfer" || action === "loan") && (
              <SavingsField label="Cuenta destino" className="sm:col-span-2">
                <select
                  required
                  value={destinationAccountId}
                  onChange={(event) => setDestinationAccountId(event.target.value)}
                  className={savingsInputClass}
                >
                  <option value="">Selecciona dónde recibirás el dinero</option>
                  {destinationAccounts.map((account) => (
                    <option key={account.id} value={account.id}>
                      {account.name} · {formatCurrency(account.currentBalance)}
                    </option>
                  ))}
                </select>
              </SavingsField>
            )}
            {action === "expense" && (
              <>
                <SavingsField label="Categoría del gasto">
                  <select
                    required
                    value={expenseCategoryId}
                    onChange={(event) => setExpenseCategoryId(event.target.value)}
                    className={savingsInputClass}
                  >
                    <option value="">Selecciona una categoría</option>
                    {categories?.map((category) => (
                      <option key={category.id} value={category.id}>
                        {category.name}
                      </option>
                    ))}
                  </select>
                </SavingsField>
                <SavingsField label="Descripción del gasto">
                  <input
                    required
                    maxLength={200}
                    value={expenseDescription}
                    onChange={(event) => setExpenseDescription(event.target.value)}
                    placeholder="Ej. Compra de pasajes"
                    className={savingsInputClass}
                  />
                </SavingsField>
              </>
            )}
            {action === "reassign" && (
              <SavingsField label="Meta destino" className="sm:col-span-2">
                <select
                  required
                  value={targetGoalId}
                  onChange={(event) => setTargetGoalId(event.target.value)}
                  className={savingsInputClass}
                >
                  <option value="">Selecciona otra meta</option>
                  {targetGoals.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.name} · espacio{" "}
                      {formatCurrency(item.targetAmount - item.currentAmount)}
                    </option>
                  ))}
                </select>
              </SavingsField>
            )}
            <SavingsField
              label={
                action === "correction"
                  ? "Motivo de la corrección"
                  : "Nota opcional"
              }
              className="sm:col-span-2"
            >
              <textarea
                required={action === "correction"}
                rows={2}
                maxLength={200}
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
                className={`${savingsInputClass} resize-none`}
              />
            </SavingsField>
          </div>
        </SavingsModalSection>
        <div className="grid gap-3 sm:grid-cols-3">
          <SavingsMetric
            label="Asignado ahora"
            value={formatCurrency(goal.currentAmount)}
          />
          <SavingsMetric
            label="Monto del movimiento"
            value={formatCurrency(numericAmount)}
          />
          <SavingsMetric
            label="Saldo posterior"
            value={formatCurrency(
              Math.max(0, goal.currentAmount - numericAmount),
            )}
            tone="warning"
          />
        </div>
        <SavingsModalActions
          onClose={onClose}
          isPending={isPending}
          submitLabel={
            action === "reassign"
              ? "Reasignar monto"
              : "Confirmar retiro"
          }
          disabled={!isValid}
        />
      </form>
    </SavingsModalShell>
  );
}
