import { useState } from "react";
import {
  AlertTriangle,
  CalendarClock,
  CheckCircle2,
  Clock3,
  X,
} from "lucide-react";
import {
  useCancelRestoration,
  useEmergencyFundRestorations,
} from "../hooks/useEmergencyFundRestorations";
import type {
  EmergencyFundRestoration,
  SavingsGoal,
} from "../../../types/savings.types";
import { formatCurrency } from "../../../utils/formatCurrency";
import { formatPlanDate } from "../utils/restorationPlan";
import RestorationPaymentModal from "./RestorationPaymentModal";

export default function EmergencyFundRestorationsModal({
  goal,
  onClose,
}: {
  goal: SavingsGoal;
  onClose: () => void;
}) {
  const { data: restorations, isLoading } = useEmergencyFundRestorations(
    goal.id,
  );
  const [selected, setSelected] = useState<EmergencyFundRestoration | null>(
    null,
  );
  const cancelMutation = useCancelRestoration();
  const cancelRestoration = (restoration: EmergencyFundRestoration) => {
    if (
      window.confirm(
        "¿Cancelar este compromiso de reposición? El monto pendiente seguirá visible en el historial.",
      )
    )
      cancelMutation.mutate(restoration.id);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-[#2C2A29]/55 p-3 backdrop-blur-sm sm:p-5">
      <div className="max-h-[92vh] w-full max-w-3xl overflow-y-auto rounded-[30px] border border-[#E8E1D8] bg-[#FBF9F4] shadow-2xl">
        <header className="sticky top-0 z-10 flex items-start justify-between gap-4 border-b border-[#E8E1D8] bg-[#FBF9F4]/95 px-6 py-5 backdrop-blur sm:px-8">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.16em] text-[#5F8667]">
              Seguimiento
            </p>
            <h2 className="mt-1 font-serif text-2xl font-semibold text-[#2C2A29]">
              Restauraciones del fondo
            </h2>
            <p className="mt-1 text-sm text-[#7C756E]">
              Pendiente total:{" "}
              <strong className="text-[#2C2A29]">
                {formatCurrency(goal.pendingRestorationAmount)}
              </strong>
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-full p-2 text-[#7C756E] transition hover:bg-[#EFEAE2]"
            aria-label="Cerrar"
          >
            <X className="h-5 w-5" />
          </button>
        </header>

        <div className="space-y-4 p-6 sm:p-8">
          {isLoading && (
            <p className="py-8 text-center text-sm text-[#7C756E]">
              Cargando restauraciones...
            </p>
          )}
          {restorations?.map((restoration) => {
            const progress =
              restoration.originalAmount > 0
                ? Math.min(
                    100,
                    (restoration.restoredAmount / restoration.originalAmount) *
                      100,
                  )
                : 0;
            return (
              <article
                key={restoration.id}
                className={`rounded-[24px] border bg-white/70 p-5 ${restoration.isOverdue && restoration.status === "open" ? "border-amber-300" : "border-[#E8E1D8]"}`}
              >
                <div className="flex flex-wrap items-start justify-between gap-4">
                  <div>
                    <h3 className="text-lg font-semibold text-[#2C2A29]">
                      {restoration.description}
                    </h3>
                    <p className="mt-1 text-xs text-[#7C756E]">
                      Uso registrado el{" "}
                      {formatPlanDate(restoration.acquisitionDate)}
                    </p>
                  </div>
                  {restoration.status === "completed" ? (
                    <span className="inline-flex items-center gap-1.5 rounded-full bg-[#E6EDE2] px-3 py-1.5 text-sm font-medium text-[#52664D]">
                      <CheckCircle2 className="h-4 w-4" /> Finalizado{" "}
                      {restoration.completedDate
                        ? formatPlanDate(restoration.completedDate)
                        : ""}
                    </span>
                  ) : restoration.status === "open" ? (
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => cancelRestoration(restoration)}
                        disabled={cancelMutation.isPending}
                        className="rounded-xl border border-red-200 px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-50 disabled:opacity-50"
                      >
                        Cancelar plan
                      </button>
                      <button
                        type="button"
                        onClick={() => setSelected(restoration)}
                        className="rounded-xl bg-[#2C2A29] px-4 py-2 text-sm font-medium text-white transition hover:bg-[#1A1918]"
                      >
                        Registrar reposición
                      </button>
                    </div>
                  ) : (
                    <span className="rounded-full bg-[#EFEAE2] px-3 py-1.5 text-sm text-[#7C756E]">
                      Cancelado
                    </span>
                  )}
                </div>

                <div className="mt-5 grid grid-cols-3 gap-2 sm:gap-3">
                  <Metric
                    label="Monto usado"
                    value={formatCurrency(restoration.originalAmount)}
                  />
                  <Metric
                    label="Restaurado"
                    value={formatCurrency(restoration.restoredAmount)}
                  />
                  <Metric
                    label="Pendiente"
                    value={formatCurrency(restoration.outstandingAmount)}
                    strong
                  />
                </div>
                <div className="mt-4 h-2 overflow-hidden rounded-full bg-[#EFEAE2]">
                  <div
                    className="h-full rounded-full bg-[#7FA083] transition-all"
                    style={{ width: `${progress}%` }}
                  />
                </div>

                {restoration.status === "open" && restoration.isOverdue && (
                  <div className="mt-4 flex gap-2 rounded-2xl border border-amber-300 bg-amber-50 p-4 text-sm text-amber-950">
                    <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" />
                    <div>
                      <strong>Reposición pendiente</strong>
                      <p>
                        Registra manualmente{" "}
                        {formatCurrency(restoration.nextContributionAmount)}{" "}
                        para mantener el plan al día.
                      </p>
                    </div>
                  </div>
                )}
                {restoration.status === "open" && (
                  <div
                    className={`mt-4 grid gap-3 rounded-2xl border p-4 sm:grid-cols-2 ${restoration.isOverdue ? "border-amber-200 bg-amber-50" : "border-[#DDE7D8] bg-[#F3F7F0]"}`}
                  >
                    <div className="flex gap-2">
                      <CalendarClock
                        className={`mt-0.5 h-4 w-4 shrink-0 ${restoration.isOverdue ? "text-amber-700" : "text-[#5F8667]"}`}
                      />
                      <div>
                        <span className="block text-xs text-[#7C756E]">
                          {restoration.isOverdue
                            ? "Aporte vencido"
                            : "Próximo aporte"}
                        </span>
                        <strong
                          className={
                            restoration.isOverdue
                              ? "text-amber-900"
                              : "text-[#304B38]"
                          }
                        >
                          {formatCurrency(restoration.nextContributionAmount)} ·{" "}
                          {formatPlanDate(restoration.nextScheduledDate)}
                        </strong>
                      </div>
                    </div>
                    <div className="flex gap-2">
                      <Clock3 className="mt-0.5 h-4 w-4 shrink-0 text-[#5F8667]" />
                      <div>
                        <span className="block text-xs text-[#7C756E]">
                          Finalización estimada
                        </span>
                        <strong className="text-[#304B38]">
                          {restoration.estimatedCompletionDate
                            ? formatPlanDate(
                                restoration.estimatedCompletionDate,
                              )
                            : formatPlanDate(restoration.targetRestorationDate)}
                        </strong>
                      </div>
                    </div>
                  </div>
                )}

                {restoration.status === "open" && (
                  <p className="mt-3 text-xs text-[#7C756E]">
                    Plan original:{" "}
                    {formatCurrency(restoration.scheduledContributionAmount)}{" "}
                    mensuales, con fecha máxima{" "}
                    {formatPlanDate(restoration.targetRestorationDate)}.
                  </p>
                )}
              </article>
            );
          })}
          {!isLoading && restorations?.length === 0 && (
            <div className="rounded-[24px] border border-dashed border-[#D8D0C6] px-6 py-12 text-center">
              <CheckCircle2 className="mx-auto h-7 w-7 text-[#7FA083]" />
              <p className="mt-3 font-medium text-[#2C2A29]">
                No hay restauraciones registradas
              </p>
              <p className="mt-1 text-sm text-[#7C756E]">
                Los usos futuros del fondo aparecerán aquí con su avance y fecha
                estimada.
              </p>
            </div>
          )}
        </div>
      </div>
      {selected && (
        <RestorationPaymentModal
          restoration={selected}
          goal={goal}
          onClose={() => setSelected(null)}
        />
      )}
    </div>
  );
}

function Metric({
  label,
  value,
  strong = false,
}: {
  label: string;
  value: string;
  strong?: boolean;
}) {
  return (
    <div className="rounded-2xl bg-[#F6F2EC] p-3">
      <span className="block text-[11px] text-[#7C756E] sm:text-xs">
        {label}
      </span>
      <strong
        className={`${strong ? "text-[#7A4B3A]" : "text-[#2C2A29]"} text-sm sm:text-base`}
      >
        {value}
      </strong>
    </div>
  );
}
