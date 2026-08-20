import { useState } from "react";
import { FilePenLine, ShieldCheck } from "lucide-react";
import { useUpdateSavingsGoal } from "../hooks/useSavings";
import type { SavingsGoal } from "../../../types/savings.types";
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

export default function EditGoalModal({ goal, onClose }: Props) {
  const [name, setName] = useState(goal.name);
  const [targetAmount, setTargetAmount] = useState<number | "">(
    goal.targetAmount,
  );
  const [targetDate, setTargetDate] = useState(goal.targetDate || "");
  const [description, setDescription] = useState(goal.description || "");
  const [purpose, setPurpose] = useState<"general" | "emergency_fund">(
    goal.purpose,
  );
  const [minimumProtectedAmount, setMinimumProtectedAmount] = useState<
    number | ""
  >(goal.minimumProtectedAmount ?? "");
  const { mutate: updateGoal, isPending } = useUpdateSavingsGoal();
  const target = Number(targetAmount || 0);
  const minimum = Number(minimumProtectedAmount || 0);
  const isValid =
    Boolean(name.trim()) &&
    target >= goal.currentAmount &&
    target > 0 &&
    (purpose !== "emergency_fund" || (minimum >= 0 && minimum <= target));

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!isValid) return;
    updateGoal(
      {
        id: goal.id,
        data: {
          name: name.trim(),
          targetAmount: target,
          targetDate: targetDate || undefined,
          description: description.trim() || undefined,
          purpose,
          minimumProtectedAmount:
            purpose === "emergency_fund" ? minimum : undefined,
        },
      },
      { onSuccess: onClose },
    );
  };

  return (
    <SavingsModalShell
      eyebrow="Configuración"
      title="Editar meta"
      description="Actualiza la definición de la meta sin alterar sus movimientos históricos."
      onClose={onClose}
    >
      <form onSubmit={handleSubmit} className="space-y-6 p-6 sm:p-8">
        <SavingsModalSection
          icon={<FilePenLine className="h-5 w-5" />}
          title="Datos principales"
        >
          <div className="grid gap-4 sm:grid-cols-2">
            <SavingsField label="Nombre" className="sm:col-span-2">
              <input
                required
                maxLength={150}
                value={name}
                onChange={(event) => setName(event.target.value)}
                className={savingsInputClass}
              />
            </SavingsField>
            <SavingsField label="Propósito">
              <select
                value={purpose}
                onChange={(event) =>
                  setPurpose(event.target.value as "general" | "emergency_fund")
                }
                disabled={goal.openRestorationsCount > 0}
                className={savingsInputClass}
              >
                <option value="general">Meta personal</option>
                <option value="emergency_fund">Fondo de emergencia</option>
              </select>
            </SavingsField>
            <SavingsField
              label="Monto objetivo"
              hint={`No puede ser menor al asignado actual: ${goal.currentAmount.toFixed(2)}`}
            >
              <input
                type="number"
                step="0.01"
                min={goal.currentAmount}
                required
                value={targetAmount}
                onChange={(event) =>
                  setTargetAmount(
                    event.target.value === "" ? "" : Number(event.target.value),
                  )
                }
                className={savingsInputClass}
              />
            </SavingsField>
          </div>
        </SavingsModalSection>
        {purpose === "emergency_fund" && (
          <SavingsModalSection
            icon={<ShieldCheck className="h-5 w-5" />}
            title="Protección"
          >
            <SavingsField
              label="Mínimo protegido"
              hint="Sirve como referencia; el flujo especial de uso puede cruzarlo mostrando una advertencia."
            >
              <input
                type="number"
                step="0.01"
                min="0"
                max={target || undefined}
                value={minimumProtectedAmount}
                onChange={(event) =>
                  setMinimumProtectedAmount(
                    event.target.value === "" ? "" : Number(event.target.value),
                  )
                }
                className={savingsInputClass}
              />
            </SavingsField>
          </SavingsModalSection>
        )}
        <SavingsModalSection
          icon={<FilePenLine className="h-5 w-5" />}
          title="Detalles opcionales"
        >
          <div className="grid gap-4">
            <SavingsField label="Fecha objetivo">
              <input
                type="date"
                value={targetDate}
                onChange={(event) => setTargetDate(event.target.value)}
                className={savingsInputClass}
              />
            </SavingsField>
            <SavingsField label="Descripción">
              <textarea
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                rows={3}
                maxLength={500}
                className={`${savingsInputClass} resize-none`}
              />
            </SavingsField>
          </div>
        </SavingsModalSection>
        <SavingsModalActions
          onClose={onClose}
          isPending={isPending}
          submitLabel="Guardar cambios"
          disabled={!isValid}
        />
      </form>
    </SavingsModalShell>
  );
}
