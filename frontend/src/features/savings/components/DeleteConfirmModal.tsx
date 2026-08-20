import { useState } from "react";
import { Archive, ArrowRightLeft, Unlock } from "lucide-react";
import { useDeleteSavingsGoal, useSavingsGoals } from "../hooks/useSavings";
import type { SavingsGoal } from "../../../types/savings.types";
import { todayDateOnly } from "../../../utils/dateOnly";
import { formatCurrency } from "../../../utils/formatCurrency";
import {
  SavingsField,
  SavingsModalActions,
  SavingsModalSection,
  SavingsModalShell,
  savingsInputClass,
} from "./SavingsModalShell";

interface Props {
  goal: SavingsGoal;
  onClose: () => void;
}

export default function DeleteConfirmModal({ goal, onClose }: Props) {
  const [resolution, setResolution] = useState<"release" | "reassign">(
    "release",
  );
  const [targetGoalId, setTargetGoalId] = useState("");
  const { data: goals } = useSavingsGoals();
  const { mutate: deleteGoal, isPending } = useDeleteSavingsGoal();
  const hasBalance = goal.currentAmount > 0;
  const targetGoals = (goals ?? []).filter(
    (item) =>
      item.id !== goal.id &&
      item.targetAmount - item.currentAmount >= goal.currentAmount,
  );
  const blocked = goal.openRestorationsCount > 0;
  const isValid =
    !hasBalance || resolution === "release" || Boolean(targetGoalId);

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!isValid || blocked) return;
    deleteGoal(
      {
        id: goal.id,
        data: hasBalance
          ? {
              resolution,
              targetGoalId:
                resolution === "reassign" ? targetGoalId : undefined,
              date: todayDateOnly(),
              idempotencyKey: crypto.randomUUID(),
            }
          : undefined,
      },
      { onSuccess: onClose },
    );
  };

  return (
    <SavingsModalShell
      eyebrow="Organización"
      title="Archivar meta"
      description="La meta dejará de estar activa, pero conservará su historial completo."
      onClose={onClose}
    >
      <form onSubmit={handleSubmit} className="space-y-6 p-6 sm:p-8">
        <SavingsModalSection
          icon={<Archive className="h-5 w-5" />}
          title={goal.name}
          description={
            hasBalance
              ? `Tiene ${formatCurrency(goal.currentAmount)} asignados. Indica qué hacer con ese monto antes de archivar.`
              : "No tiene monto asignado y puede archivarse directamente."
          }
        >
          {blocked && (
            <p className="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
              Completa o cancela las reposiciones pendientes antes de archivar
              este fondo.
            </p>
          )}
          {hasBalance && !blocked && (
            <div className="grid grid-cols-2 gap-3">
              <button
                type="button"
                onClick={() => setResolution("release")}
                className={`rounded-2xl border p-4 text-left ${resolution === "release" ? "border-[#9EAB98] bg-[#F3F7F0] text-[#304B38]" : "border-[#E8E1D8] bg-white/70 text-[#6D665F]"}`}
              >
                <Unlock className="mb-3 h-5 w-5" />
                <strong className="block text-sm">Liberar asignación</strong>
                <span className="mt-1 block text-xs">
                  Deja la meta en cero.
                </span>
              </button>
              <button
                type="button"
                onClick={() => setResolution("reassign")}
                className={`rounded-2xl border p-4 text-left ${resolution === "reassign" ? "border-[#9EAB98] bg-[#F3F7F0] text-[#304B38]" : "border-[#E8E1D8] bg-white/70 text-[#6D665F]"}`}
              >
                <ArrowRightLeft className="mb-3 h-5 w-5" />
                <strong className="block text-sm">Reasignar</strong>
                <span className="mt-1 block text-xs">
                  Mueve el monto a otra meta.
                </span>
              </button>
            </div>
          )}
          {hasBalance && !blocked && resolution === "reassign" && (
            <SavingsField label="Meta destino" className="mt-4">
              <select
                required
                value={targetGoalId}
                onChange={(event) => setTargetGoalId(event.target.value)}
                className={savingsInputClass}
              >
                <option value="">Selecciona otra meta</option>
                {targetGoals.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </SavingsField>
          )}
        </SavingsModalSection>
        <SavingsModalActions
          onClose={onClose}
          isPending={isPending}
          submitLabel="Archivar meta"
          disabled={!isValid || blocked}
          danger
        />
      </form>
    </SavingsModalShell>
  );
}
