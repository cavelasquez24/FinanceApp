import { useState } from "react";
import { Scale, ArrowUp, ArrowDown, Minus } from "lucide-react";
import {
  SavingsModalShell,
  SavingsModalSection,
  SavingsField,
  SavingsMetric,
  SavingsModalActions,
} from "../../savings/components/SavingsModalShell";
import { useReconciliationPreview, useApplyReconciliation } from "../hooks/useReconciliation";
import type { FinancialAccount } from "../../../types/account.types";

interface Props {
  account: FinancialAccount;
  onClose: () => void;
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat("es-EC", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
  }).format(value);
}

function today() {
  return new Date().toISOString().split("T")[0];
}

export function ReconciliationModal({ account, onClose }: Props) {
  const { data: preview, isLoading } = useReconciliationPreview(account.id);

  const [actualBalance, setActualBalance] = useState(
    preview?.currentBalance?.toString() ?? ""
  );
  const [reconciliationDate, setReconciliationDate] = useState(today());
  const [notes, setNotes] = useState("");

  const { mutate, isPending } = useApplyReconciliation(onClose);

  const numericActual = parseFloat(actualBalance) || 0;
  const difference = preview ? numericActual - preview.ledgerBalance : 0;
  const hasDifference = difference !== 0;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!preview) return;
    mutate({
      accountId: account.id,
      dto: {
        actualBalance: numericActual,
        reconciliationDate,
        notes: notes.trim() || undefined,
      },
    });
  };

  return (
    <SavingsModalShell
      eyebrow="Conciliación de cuenta"
      title={account.name}
      description="Compara el saldo del sistema con el saldo real de tu banco y aplica el ajuste si hay diferencia."
      onClose={onClose}
      maxWidth="max-w-xl"
    >
      {isLoading ? (
        <div className="flex items-center justify-center py-16 text-finflow-muted">
          Cargando saldo esperado...
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="space-y-5 p-6 sm:p-8">
          {/* Métricas actuales */}
          <SavingsModalSection
            icon={<Scale className="h-4 w-4" />}
            title="Saldos actuales"
          >
            <div className="grid grid-cols-2 gap-3">
              <SavingsMetric
                label="Saldo en sistema (ledger)"
                value={formatCurrency(preview?.ledgerBalance ?? 0)}
              />
              <SavingsMetric
                label="Saldo registrado"
                value={formatCurrency(preview?.currentBalance ?? 0)}
              />
            </div>
            {preview?.lastReconciliationDate && (
              <p className="mt-3 text-xs text-finflow-muted">
                Última conciliación: {preview.lastReconciliationDate} ·{" "}
                {formatCurrency(preview.lastReconciliationActualBalance ?? 0)}
              </p>
            )}
          </SavingsModalSection>

          {/* Saldo real del banco */}
          <SavingsField
            label="Saldo real en tu banco"
            hint="Ingresa exactamente lo que muestra tu banco o app financiera."
          >
            <input
              type="number"
              step="0.01"
              value={actualBalance}
              onChange={(e) => setActualBalance(e.target.value)}
              placeholder={formatCurrency(preview?.currentBalance ?? 0)}
              className="mt-1 w-full rounded-xl border border-[#D8D0C5] bg-white px-4 py-2.5 text-finflow-dark focus:border-[#7A4B3A] focus:outline-none"
              required
            />
          </SavingsField>

          {/* Diferencia calculada */}
          {actualBalance !== "" && (
            <div
              className={`flex items-center gap-3 rounded-2xl border p-4 ${
                !hasDifference
                  ? "border-[#DDE7D8] bg-[#F3F7F0]"
                  : difference > 0
                  ? "border-[#DDE7D8] bg-[#F3F7F0]"
                  : "border-amber-200 bg-amber-50"
              }`}
            >
              <span
                className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full ${
                  !hasDifference
                    ? "bg-[#304B38]/10 text-[#304B38]"
                    : difference > 0
                    ? "bg-[#304B38]/10 text-[#304B38]"
                    : "bg-amber-200 text-amber-800"
                }`}
              >
                {!hasDifference ? (
                  <Minus className="h-4 w-4" />
                ) : difference > 0 ? (
                  <ArrowUp className="h-4 w-4" />
                ) : (
                  <ArrowDown className="h-4 w-4" />
                )}
              </span>
              <div>
                <p className="text-sm font-semibold text-finflow-dark">
                  {!hasDifference
                    ? "Sin diferencia — cuenta cuadrada"
                    : `Diferencia: ${formatCurrency(difference)}`}
                </p>
                <p className="text-xs text-finflow-muted">
                  {!hasDifference
                    ? "No se generará ningún ajuste."
                    : difference > 0
                    ? "Se creará un ajuste positivo en el ledger."
                    : "Se creará un ajuste negativo (gasto no registrado, comisión, etc.)."}
                </p>
              </div>
            </div>
          )}

          {/* Fecha y notas */}
          <SavingsField label="Fecha de conciliación">
            <input
              type="date"
              value={reconciliationDate}
              onChange={(e) => setReconciliationDate(e.target.value)}
              className="mt-1 w-full rounded-xl border border-[#D8D0C5] bg-white px-4 py-2.5 text-finflow-dark focus:border-[#7A4B3A] focus:outline-none"
              required
            />
          </SavingsField>

          <SavingsField
            label="Notas (opcional)"
            hint="Describe el motivo de la diferencia si la hay."
          >
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              rows={2}
              maxLength={500}
              placeholder="Ej: comisión bancaria, transferencia pendiente..."
              className="mt-1 w-full resize-none rounded-xl border border-[#D8D0C5] bg-white px-4 py-2.5 text-sm text-finflow-dark focus:border-[#7A4B3A] focus:outline-none"
            />
          </SavingsField>

          <SavingsModalActions
            onClose={onClose}
            isPending={isPending}
            submitLabel="Aplicar conciliación"
          />
        </form>
      )}
    </SavingsModalShell>
  );
}
