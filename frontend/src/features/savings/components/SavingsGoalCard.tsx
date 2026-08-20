import {
  AlertTriangle,
  Archive,
  CalendarClock,
  Pencil,
  ShieldCheck,
} from "lucide-react";
import { type SavingsGoal } from "../../../types/savings.types";
import { formatCurrency } from "../../../utils/formatCurrency";
import { formatPlanDate } from "../utils/restorationPlan";
import { useEmergencyFundRestorations } from "../hooks/useEmergencyFundRestorations";

interface Props {
  goal: SavingsGoal;
  onDeposit: () => void;
  onWithdraw: () => void;
  onEdit: () => void;
  onDelete: () => void;
  onUseEmergencyFund: () => void;
  onViewRestorations: () => void;
}

export default function SavingsGoalCard({
  goal,
  onDeposit,
  onWithdraw,
  onEdit,
  onDelete,
  onUseEmergencyFund,
  onViewRestorations,
}: Props) {
  const isEmergencyFund = goal.purpose === "emergency_fund";
  const hasPendingRestoration =
    isEmergencyFund && goal.pendingRestorationAmount > 0;
  const isDepositDisabled = goal.isCompleted || hasPendingRestoration;
  const protectedMinimum = goal.minimumProtectedAmount ?? 0;
  const canArchive = !hasPendingRestoration;
  const progress = Math.min(100, Math.max(0, goal.progressPercentage));
  const { data: restorations } = useEmergencyFundRestorations(
    isEmergencyFund ? goal.id : undefined,
  );
  const overdue = restorations?.find(
    (item) => item.status === "open" && item.isOverdue,
  );

  return (
    <article
      className={`group relative flex flex-col rounded-[28px] border bg-[#FBF9F4]/95 p-6 shadow-sm backdrop-blur-md transition-all duration-300 hover:shadow-md ${isEmergencyFund ? "border-[#D7E1D2]" : "border-[#EFEAE2]"}`}
    >
      <div className="absolute right-5 top-5 flex items-center space-x-2 opacity-100 transition-opacity md:opacity-0 md:group-hover:opacity-100">
        <button
          type="button"
          onClick={onEdit}
          className="rounded-full p-1.5 text-[#7C756E] transition-colors hover:bg-[#EFEAE2]"
          title="Editar meta"
        >
          <Pencil className="h-4 w-4" />
        </button>
        <button
          type="button"
          onClick={onDelete}
          disabled={!canArchive}
          className="rounded-full p-1.5 text-red-400 transition-colors hover:bg-red-50 hover:text-red-600 disabled:cursor-not-allowed disabled:opacity-35"
          title={
            hasPendingRestoration
              ? "Completa o cancela las reposiciones antes de archivar"
              : "Archivar meta"
          }
        >
          <Archive className="h-4 w-4" />
        </button>
      </div>

      <div className="mb-6 pr-16">
        {isEmergencyFund && (
          <span className="mb-3 inline-flex items-center gap-1.5 rounded-full bg-[#E6EDE2] px-2.5 py-1 text-xs font-medium text-[#52664D]">
            <ShieldCheck className="h-3.5 w-3.5" /> Fondo de emergencia
          </span>
        )}
        <h3 className="text-xl font-semibold leading-tight text-[#2C2A29]">
          {goal.name}
        </h3>
        <p className="mt-1 text-xs font-medium text-[#5E7162]">
          En {goal.savingsAccountName ?? "cuenta de ahorro"}
          {goal.savingsAccountBalance != null
            ? ` · saldo real ${formatCurrency(goal.savingsAccountBalance)}`
            : ""}
        </p>
        {goal.description && (
          <p className="mt-2 line-clamp-2 text-sm text-[#7C756E]">
            {goal.description}
          </p>
        )}
        {goal.targetDate && (
          <p className="mt-2 text-xs text-[#7C756E]">
            Objetivo: {formatPlanDate(goal.targetDate)}
          </p>
        )}
      </div>

      <div className="mt-auto space-y-5">
        <div>
          <div className="mb-2 flex justify-between text-sm">
            <span className="font-semibold text-[#2C2A29]">
              {formatCurrency(goal.currentAmount)}
            </span>
            <span className="text-[#7C756E]">
              de {formatCurrency(goal.targetAmount)}
            </span>
          </div>
          <div className="h-2.5 w-full overflow-hidden rounded-full bg-[#EFEAE2]">
            <div
              className="h-full rounded-full bg-[#9EAB98] transition-all duration-500 ease-out"
              style={{
                width: `${progress}%`,
                backgroundColor: goal.isCompleted ? "#7FA083" : "#9EAB98",
              }}
            />
          </div>
          {goal.isCompleted && (
            <p className="mt-2 text-xs font-medium text-[#5F8667]">
              Meta alcanzada
            </p>
          )}
        </div>

        {isEmergencyFund && (
          <div className="grid grid-cols-2 gap-2">
            <Metric
              label="Mínimo protegido"
              value={formatCurrency(protectedMinimum)}
            />
            <Metric
              label="Reposición pendiente"
              value={formatCurrency(goal.pendingRestorationAmount)}
            />
          </div>
        )}

        {overdue && (
          <button
            type="button"
            onClick={onViewRestorations}
            className="w-full rounded-2xl border border-amber-300 bg-amber-50 p-3.5 text-left"
          >
            <span className="flex items-center gap-1.5 text-xs font-semibold text-amber-900">
              <AlertTriangle className="h-4 w-4" /> Reposición pendiente
            </span>
            <span className="mt-1 block text-sm text-amber-900">
              Registra manualmente{" "}
              {formatCurrency(overdue.nextContributionAmount)} para mantener el
              plan al día.
            </span>
          </button>
        )}
        {hasPendingRestoration && (
          <button
            type="button"
            onClick={onViewRestorations}
            className="w-full rounded-2xl border border-amber-200 bg-amber-50 p-3.5 text-left transition hover:border-amber-300"
          >
            <span className="flex items-center gap-1.5 text-xs font-medium text-amber-800">
              <CalendarClock className="h-4 w-4" /> Pendiente por restaurar
            </span>
            <strong className="mt-1 block text-lg text-amber-950">
              {formatCurrency(goal.pendingRestorationAmount)}
            </strong>
            {goal.nextRestorationDate && (
              <span className="mt-0.5 block text-xs text-amber-800">
                Próximo aporte: {formatPlanDate(goal.nextRestorationDate)}
              </span>
            )}
          </button>
        )}

        <div className="flex gap-2">
          {hasPendingRestoration ? (
            <button
              type="button"
              onClick={onViewRestorations}
              className="flex-1 rounded-xl bg-[#2C2A29] py-2.5 font-medium text-[#FBF9F4] transition hover:bg-[#1A1918]"
            >
              Ver restauración
            </button>
          ) : (
            <button
              type="button"
              onClick={onDeposit}
              disabled={isDepositDisabled}
              className={`flex-1 rounded-xl py-2.5 font-medium transition-colors ${isDepositDisabled ? "cursor-not-allowed bg-[#EFEAE2] text-[#7C756E] opacity-60" : "bg-[#2C2A29] text-[#FBF9F4] hover:bg-[#1A1918]"}`}
            >
              {goal.isCompleted ? "Completada" : "Aportar"}
            </button>
          )}
          {!isEmergencyFund && (
            <button
              type="button"
              onClick={onWithdraw}
              disabled={goal.currentAmount <= 0}
              className="flex-1 rounded-xl border border-[#E2DBD2] py-2.5 font-medium text-[#6D665F] transition-colors hover:bg-[#EFEAE2] disabled:cursor-not-allowed disabled:opacity-40"
            >
              Retirar
            </button>
          )}
        </div>

        {isEmergencyFund && !hasPendingRestoration && (
          <button
            type="button"
            onClick={onUseEmergencyFund}
            disabled={goal.currentAmount <= 0}
            className="w-full rounded-xl border border-[#D9B5A5] bg-[#F4E8E2] py-2.5 font-medium text-[#7A4B3A] transition hover:bg-[#EFDCD3] disabled:opacity-40"
          >
            Usar fondo
          </button>
        )}
      </div>
    </article>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl bg-[#F3F0EA] p-3">
      <span className="block text-[11px] text-[#7C756E]">{label}</span>
      <strong className="mt-0.5 block text-sm text-[#2C2A29]">{value}</strong>
    </div>
  );
}
