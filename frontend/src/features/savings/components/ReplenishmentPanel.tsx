import { useState } from "react";
import { ChevronDown, ChevronUp, RotateCcw, Wallet2, X } from "lucide-react";
import type { SavingsReplenishmentDto } from "../../../types/savingsReplenishment.types";
import { formatCurrency } from "../../../utils/formatCurrency";
import { getApiErrorMessage } from "../../../utils/getApiError";
import {
  useApplyManualDebit,
  useCancelReplenishment,
  usePauseReplenishment,
  useResumeReplenishment,
} from "../hooks/useSavingsReplenishments";
import { ReplenishmentDebitHistory } from "./ReplenishmentDebitHistory";

interface Props {
  replenishment: SavingsReplenishmentDto;
}

export function ReplenishmentPanel({ replenishment }: Props) {
  const [showHistory, setShowHistory] = useState(false);
  const [showManualDebit, setShowManualDebit] = useState(false);
  const [manualAmount, setManualAmount] = useState<number | "">("");

  const pauseMutation = usePauseReplenishment();
  const resumeMutation = useResumeReplenishment();
  const cancelMutation = useCancelReplenishment();
  const manualDebitMutation = useApplyManualDebit();

  const isOpen =
    replenishment.status === "Active" || replenishment.status === "Paused";
  const progress = Math.min(100, Math.max(0, replenishment.progressPercent));

  const handlePause = () =>
    pauseMutation.mutate({ id: replenishment.id, data: {} });

  const handleResume = () => resumeMutation.mutate(replenishment.id);

  const handleCancel = () => {
    if (!window.confirm("¿Cancelar este plan de reposición? El pendiente quedará sin débitos futuros."))
      return;
    cancelMutation.mutate(replenishment.id);
  };

  const handleManualDebit = (event: React.FormEvent) => {
    event.preventDefault();
    const amount = Number(manualAmount || 0);
    if (amount <= 0 || amount > replenishment.pendingAmount) return;
    manualDebitMutation.mutate(
      {
        id: replenishment.id,
        data: { amount, idempotencyKey: crypto.randomUUID() },
      },
      {
        onSuccess: () => {
          setShowManualDebit(false);
          setManualAmount("");
        },
      },
    );
  };

  return (
    <div className="rounded-2xl border border-[#EFEAE2] bg-white/70 p-4">
      <div className="mb-2 flex items-center justify-between text-sm">
        <span className="font-medium text-finflow-dark">{replenishment.name}</span>
        <span className="text-xs text-finflow-muted">
          {replenishment.isPaused ? "Pausado" : replenishment.status === "Completed" ? "Completado" : replenishment.status === "Cancelled" ? "Cancelado" : "Activo"}
        </span>
      </div>

      <div className="h-2 w-full overflow-hidden rounded-full bg-[#EFEAE2]">
        <div
          className="h-full rounded-full bg-finflow-green transition-all duration-500 ease-out"
          style={{ width: `${progress}%` }}
        />
      </div>

      <div className="mt-3 grid grid-cols-2 gap-2 text-xs sm:grid-cols-4">
        <Metric label="Repuesto" value={formatCurrency(replenishment.amountReplenished)} />
        <Metric label="Pendiente" value={formatCurrency(replenishment.pendingAmount)} />
        <Metric label="Débito/ciclo" value={formatCurrency(replenishment.monthlyDebitAmount)} />
        <Metric
          label="ETA"
          value={
            replenishment.estimatedCyclesRemaining > 0
              ? `~${replenishment.estimatedCyclesRemaining} ciclo${replenishment.estimatedCyclesRemaining === 1 ? "" : "s"}`
              : "—"
          }
        />
      </div>

      {isOpen && (
        <div className="mt-3 flex flex-wrap gap-2">
          {replenishment.status === "Active" ? (
            <ActionButton onClick={handlePause} pending={pauseMutation.isPending}>
              Pausar
            </ActionButton>
          ) : (
            <ActionButton onClick={handleResume} pending={resumeMutation.isPending} icon={<RotateCcw className="h-3.5 w-3.5" />}>
              Reanudar
            </ActionButton>
          )}
          <ActionButton
            onClick={() => setShowManualDebit((prev) => !prev)}
            icon={<Wallet2 className="h-3.5 w-3.5" />}
          >
            Abonar ahora
          </ActionButton>
          <ActionButton onClick={handleCancel} pending={cancelMutation.isPending} danger icon={<X className="h-3.5 w-3.5" />}>
            Cancelar
          </ActionButton>
        </div>
      )}

      {showManualDebit && isOpen && (
        <form onSubmit={handleManualDebit} className="mt-3 flex items-center gap-2">
          <input
            type="number"
            min="0.01"
            step="0.01"
            max={replenishment.pendingAmount}
            placeholder={`Máx. ${formatCurrency(replenishment.pendingAmount)}`}
            value={manualAmount}
            onChange={(event) =>
              setManualAmount(event.target.value === "" ? "" : Number(event.target.value))
            }
            className="input-restoration flex-1 !py-2 text-sm"
          />
          <button
            type="submit"
            disabled={manualDebitMutation.isPending}
            className="shrink-0 rounded-xl bg-finflow-dark px-3 py-2 text-sm font-medium text-finflow-cream transition hover:bg-[#1A1918] disabled:opacity-50"
          >
            Confirmar
          </button>
        </form>
      )}
      {manualDebitMutation.error && (
        <p className="mt-2 text-xs text-finflow-rust">
          {getApiErrorMessage(manualDebitMutation.error, "No se pudo registrar el abono.")}
        </p>
      )}

      {replenishment.debits.length > 0 && (
        <button
          type="button"
          onClick={() => setShowHistory((prev) => !prev)}
          className="mt-3 flex items-center gap-1 text-xs font-medium text-finflow-blue"
        >
          {showHistory ? "Ocultar historial" : "Ver historial de débitos"}
          {showHistory ? <ChevronUp className="h-3.5 w-3.5" /> : <ChevronDown className="h-3.5 w-3.5" />}
        </button>
      )}
      {showHistory && (
        <div className="mt-2">
          <ReplenishmentDebitHistory debits={replenishment.debits} />
        </div>
      )}
    </div>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl bg-[#F3F0EA] p-2">
      <span className="block text-[10px] text-finflow-muted">{label}</span>
      <strong className="mt-0.5 block text-finflow-dark">{value}</strong>
    </div>
  );
}

function ActionButton({
  onClick,
  children,
  pending = false,
  danger = false,
  icon,
}: {
  onClick: () => void;
  children: React.ReactNode;
  pending?: boolean;
  danger?: boolean;
  icon?: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={pending}
      className={`flex items-center gap-1.5 rounded-xl border px-3 py-1.5 text-xs font-medium transition disabled:opacity-50 ${
        danger
          ? "border-finflow-rust/30 text-finflow-rust hover:bg-finflow-rust/10"
          : "border-[#E2DBD2] text-finflow-dark hover:bg-[#EFEAE2]"
      }`}
    >
      {icon}
      {children}
    </button>
  );
}
