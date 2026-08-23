import { useMemo, useState } from "react";
import {
  CalendarRange,
  CheckCircle2,
  ReceiptText,
  ShieldAlert,
} from "lucide-react";
import { useCreateEmergencyFundUse } from "../hooks/useEmergencyFundRestorations";
import type { SavingsGoal } from "../../../types/savings.types";
import { formatCurrency } from "../../../utils/formatCurrency";
import { getApiErrorMessage } from "../../../utils/getApiError";
import { useAccounts } from "../../accounts/hooks/useAccounts";
import { useCategories } from "../../categories/hooks/useCategories";
import {
  addMonthsClamped,
  calculateRestorationPlan,
  formatPlanDate,
  getLocalToday,
  type RestorationPlanMode,
} from "../utils/restorationPlan";
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
const today = getLocalToday();

export default function EmergencyFundUseModal({ goal, onClose }: Props) {
  const [fundedAmount, setFundedAmount] = useState<number | "">("");
  const [description, setDescription] = useState("");
  const [acquisitionDate, setAcquisitionDate] = useState(today);
  const [firstScheduledDate, setFirstScheduledDate] = useState(
    addMonthsClamped(today, 1),
  );
  const [planMode, setPlanMode] = useState<RestorationPlanMode>("deadline");
  const [targetRestorationDate, setTargetRestorationDate] = useState(
    addMonthsClamped(today, 3),
  );
  const [scheduledContributionAmount, setScheduledContributionAmount] =
    useState<number | "">("");
  const [notes, setNotes] = useState("");
  const [useMode, setUseMode] = useState<"expense" | "account_transfer">("expense");
  const [destinationAccountId, setDestinationAccountId] = useState("");
  const [expenseCategoryId, setExpenseCategoryId] = useState("");
  const createMutation = useCreateEmergencyFundUse();

  const { data: accounts } = useAccounts();
  const { data: categories } = useCategories("expense");
  const destinationAccounts =
    accounts?.filter(
      (account) =>
        account.isActive &&
        (account.type === "cash" || account.type === "savings") &&
        account.id !== goal.savingsAccountId,
    ) ?? [];

  const funded = Number(fundedAmount || 0);
  const plan = useMemo(
    () =>
      calculateRestorationPlan({
        outstandingAmount: funded,
        firstScheduledDate,
        mode: planMode,
        targetDate: planMode === "deadline" ? targetRestorationDate : undefined,
        monthlyAmount:
          planMode === "monthly_amount"
            ? Number(scheduledContributionAmount || 0)
            : undefined,
      }),
    [
      firstScheduledDate,
      funded,
      planMode,
      scheduledContributionAmount,
      targetRestorationDate,
    ],
  );
  const resultingBalance = goal.currentAmount - funded;
  const protectedMinimum = goal.minimumProtectedAmount ?? 0;
  const belowProtectedMinimum =
    funded > 0 && resultingBalance < protectedMinimum;
  const datesAreValid =
    acquisitionDate <= today &&
    firstScheduledDate >= acquisitionDate &&
    Boolean(plan);
  const formIsValid =
    funded > 0 &&
    funded <= goal.currentAmount &&
    Boolean(description.trim()) &&
    datesAreValid &&
    (useMode !== "expense" || Boolean(expenseCategoryId)) &&
    (useMode !== "account_transfer" || Boolean(destinationAccountId));

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!formIsValid || !plan) return;
    createMutation.mutate(
      {
        goalId: goal.id,
        data: {
          fundedAmount: funded,
          description: description.trim(),
          acquisitionDate,
          targetRestorationDate: plan.targetDate,
          scheduledContributionAmount: plan.monthlyAmount,
          useMode,
          destinationAccountId: useMode === "account_transfer" ? destinationAccountId : undefined,
          expenseCategoryId: useMode === "expense" ? expenseCategoryId : undefined,
          firstScheduledDate,
          idempotencyKey: crypto.randomUUID(),
          notes: notes.trim() || undefined,
        },
      },
      { onSuccess: onClose },
    );
  };

  return (
    <SavingsModalShell
      eyebrow="Protección financiera"
      title="Usar el fondo de emergencia"
      description="Usa dinero real de la cuenta de ahorro y crea un compromiso interno de reposición."
      onClose={onClose}
      maxWidth="max-w-4xl"
    >
      <form onSubmit={handleSubmit} className="space-y-6 p-6 sm:p-8">
        <SavingsModalSection
          icon={<ReceiptText className="h-5 w-5" />}
          number="1"
          title="Registra el uso"
          description="Describe la emergencia y cuánto deseas retirar de la asignación."
        >
          <div className="mb-5 grid grid-cols-2 gap-2">
            <button
              type="button"
              onClick={() => {
                setUseMode("expense");
                setDestinationAccountId("");
              }}
              className={`rounded-2xl border p-3 text-left text-sm ${useMode === "expense" ? "border-[#9EAB98] bg-[#F3F7F0] text-[#304B38]" : "border-[#E8E1D8] bg-white text-[#6D665F]"}`}
            >
              <strong className="block">Pagar el gasto desde ahorro</strong>
              <span className="mt-1 block text-xs">
                Descuenta Savings y registra el gasto automáticamente.
              </span>
            </button>
            <button
              type="button"
              onClick={() => {
                setUseMode("account_transfer");
                setExpenseCategoryId("");
              }}
              className={`rounded-2xl border p-3 text-left text-sm ${useMode === "account_transfer" ? "border-[#9EAB98] bg-[#F3F7F0] text-[#304B38]" : "border-[#E8E1D8] bg-white text-[#6D665F]"}`}
            >
              <strong className="block">Transferir para usar</strong>
              <span className="mt-1 block text-xs">
                Mueve el dinero a otra cuenta sin crear todavía un gasto.
              </span>
            </button>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <SavingsField label="Motivo o emergencia" className="sm:col-span-2">
              <input
                required
                maxLength={200}
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                placeholder="Ej. Reparación médica inesperada"
                className={savingsInputClass}
              />
            </SavingsField>
            <SavingsField
              label="Monto tomado del fondo"
              hint={`Disponible: ${formatCurrency(goal.currentAmount)}`}
            >
              <input
                type="number"
                min="0.01"
                step="0.01"
                max={goal.currentAmount}
                required
                value={fundedAmount}
                onChange={(event) =>
                  setFundedAmount(
                    event.target.value === "" ? "" : Number(event.target.value),
                  )
                }
                className={savingsInputClass}
              />
            </SavingsField>
            <SavingsField label="Fecha del uso">
              <input
                type="date"
                required
                max={today}
                value={acquisitionDate}
                onChange={(event) => setAcquisitionDate(event.target.value)}
                className={savingsInputClass}
              />
            </SavingsField>
            {useMode === "expense" ? (
              <SavingsField label="Categoría del gasto" className="sm:col-span-2">
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
            ) : (
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
          </div>
        </SavingsModalSection>
        <SavingsModalSection
          icon={<ShieldAlert className="h-5 w-5" />}
          number="2"
          title="Revisa el impacto"
          description="El mínimo protegido es una referencia de seguridad; este flujo puede cruzarlo con una advertencia."
        >
          <div className="grid gap-3 sm:grid-cols-3">
            <SavingsMetric
              label="Fondo actual"
              value={formatCurrency(goal.currentAmount)}
            />
            <SavingsMetric
              label="Después del uso"
              value={formatCurrency(Math.max(0, resultingBalance))}
              tone={belowProtectedMinimum ? "warning" : "default"}
            />
            <SavingsMetric
              label="Mínimo protegido"
              value={formatCurrency(protectedMinimum)}
            />
          </div>
          {belowProtectedMinimum && (
            <p className="mt-3 rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
              El fondo quedará{" "}
              {formatCurrency(protectedMinimum - resultingBalance)} por debajo
              del mínimo protegido.
            </p>
          )}
        </SavingsModalSection>
        <SavingsModalSection
          icon={<CalendarRange className="h-5 w-5" />}
          number="3"
          title="Programa la reposición"
          description="Las fechas son recordatorios. La reposición solo se aplica cuando la registras manualmente."
        >
          <div className="mb-5 grid grid-cols-2 rounded-2xl bg-[#EFEAE2] p-1">
            <PlanModeButton
              active={planMode === "deadline"}
              onClick={() => setPlanMode("deadline")}
              title="Tengo fecha máxima"
              subtitle="Calcula mi aporte"
            />
            <PlanModeButton
              active={planMode === "monthly_amount"}
              onClick={() => setPlanMode("monthly_amount")}
              title="Tengo un monto mensual"
              subtitle="Calcula mi fecha"
            />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <SavingsField label="Primera fecha programada">
              <input
                type="date"
                required
                min={acquisitionDate}
                value={firstScheduledDate}
                onChange={(event) => setFirstScheduledDate(event.target.value)}
                className={savingsInputClass}
              />
            </SavingsField>
            {planMode === "deadline" ? (
              <SavingsField label="Fecha máxima para reponer">
                <input
                  type="date"
                  required
                  min={firstScheduledDate}
                  value={targetRestorationDate}
                  onChange={(event) =>
                    setTargetRestorationDate(event.target.value)
                  }
                  className={savingsInputClass}
                />
              </SavingsField>
            ) : (
              <SavingsField label="Monto mensual sugerido">
                <input
                  type="number"
                  min="0.01"
                  step="0.01"
                  max={funded || undefined}
                  required
                  value={scheduledContributionAmount}
                  onChange={(event) =>
                    setScheduledContributionAmount(
                      event.target.value === ""
                        ? ""
                        : Number(event.target.value),
                    )
                  }
                  className={savingsInputClass}
                />
              </SavingsField>
            )}
          </div>
          {plan && (
            <div className="mt-5 rounded-2xl border border-[#DDE7D8] bg-[#F3F7F0] p-4">
              <div className="flex items-start gap-3">
                <CheckCircle2 className="mt-0.5 h-5 w-5 shrink-0 text-[#5F8667]" />
                <div>
                  <p className="font-medium text-[#304B38]">
                    {formatCurrency(plan.monthlyAmount)} al mes · reposición
                    estimada el {formatPlanDate(plan.estimatedCompletionDate)}
                  </p>
                  <p className="mt-1 text-sm text-[#5E7162]">
                    {plan.paymentsCount} registro
                    {plan.paymentsCount === 1 ? "" : "s"}; el último sería de{" "}
                    {formatCurrency(plan.finalPayment)}.
                  </p>
                </div>
              </div>
            </div>
          )}
        </SavingsModalSection>
        <SavingsField label="Notas opcionales">
          <textarea
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
            rows={2}
            maxLength={500}
            className={`${savingsInputClass} resize-none`}
          />
        </SavingsField>
        {createMutation.error && (
          <p className="rounded-xl bg-red-50 p-3 text-sm text-red-700">
            {getApiErrorMessage(
              createMutation.error,
              "No se pudo registrar el uso del fondo.",
            )}
          </p>
        )}
        <SavingsModalActions
          onClose={onClose}
          isPending={createMutation.isPending}
          submitLabel="Registrar uso y reposición"
          disabled={!formIsValid}
        />
      </form>
    </SavingsModalShell>
  );
}

function PlanModeButton({
  active,
  onClick,
  title,
  subtitle,
}: {
  active: boolean;
  onClick: () => void;
  title: string;
  subtitle: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`rounded-xl px-3 py-2.5 text-left transition ${active ? "bg-white text-finflow-dark shadow-sm" : "text-finflow-muted hover:text-finflow-dark"}`}
    >
      <span className="block text-sm font-semibold">{title}</span>
      <span className="block text-xs font-normal">{subtitle}</span>
    </button>
  );
}
