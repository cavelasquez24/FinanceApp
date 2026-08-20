import { useEffect, useState } from "react";
import { Coins, FileText, ShieldCheck, Target } from "lucide-react";
import { useCreateSavingsGoal, useSavingsGoals } from "../hooks/useSavings";
import { todayDateOnly } from "../../../utils/dateOnly";
import { useAccounts } from "../../accounts/hooks/useAccounts";
import { formatCurrency } from "../../../utils/formatCurrency";
import {
  SavingsField,
  SavingsMetric,
  SavingsModalActions,
  SavingsModalSection,
  SavingsModalShell,
  savingsInputClass,
} from "./SavingsModalShell";

interface Props {
  onClose: () => void;
}

export default function CreateGoalModal({ onClose }: Props) {
  const [name, setName] = useState("");
  const [targetAmount, setTargetAmount] = useState<number | "">("");
  const [initialAmount, setInitialAmount] = useState<number | "">("");
  const [targetDate, setTargetDate] = useState("");
  const [description, setDescription] = useState("");
  const [purpose, setPurpose] = useState<"general" | "emergency_fund">(
    "general",
  );
  const [minimumProtectedAmount, setMinimumProtectedAmount] = useState<
    number | ""
  >("");
  const [savingsAccountId, setSavingsAccountId] = useState("");
  const [fundingMode, setFundingMode] = useState<
    "existing_balance" | "account_transfer"
  >("existing_balance");
  const [sourceAccountId, setSourceAccountId] = useState("");
  const { mutate: createGoal, isPending } = useCreateSavingsGoal();
  const { data: accounts } = useAccounts();
  const { data: goals } = useSavingsGoals();
  const savingsAccounts =
    accounts?.filter((account) => account.type === "savings" && account.isActive) ??
    [];
  const sourceAccounts =
    accounts?.filter(
      (account) =>
        account.isActive &&
        (account.type === "cash" || account.type === "savings") &&
        account.id !== savingsAccountId,
    ) ?? [];

  useEffect(() => {
    if (savingsAccountId || savingsAccounts.length === 0) return;
    setSavingsAccountId(
      savingsAccounts.find((account) => account.isDefault)?.id ??
        savingsAccounts[0].id,
    );
  }, [savingsAccountId, savingsAccounts]);

  const selectedSavings = savingsAccounts.find(
    (account) => account.id === savingsAccountId,
  );
  const allocatedInAccount = (goals ?? [])
    .filter((goal) => goal.savingsAccountId === savingsAccountId)
    .reduce((sum, goal) => sum + goal.currentAmount, 0);
  const unallocatedBalance = Math.max(
    0,
    (selectedSavings?.currentBalance ?? 0) - allocatedInAccount,
  );
  const target = Number(targetAmount || 0);
  const initial = Number(initialAmount || 0);
  const minimum = Number(minimumProtectedAmount || 0);
  const isValid =
    Boolean(name.trim()) &&
    Boolean(savingsAccountId) &&
    target > 0 &&
    initial >= 0 &&
    initial <= target &&
    (initial === 0 ||
      (fundingMode === "existing_balance"
        ? initial <= unallocatedBalance
        : Boolean(sourceAccountId))) &&
    (purpose !== "emergency_fund" || (minimum >= 0 && minimum <= target));

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!isValid) return;
    createGoal(
      {
        name: name.trim(),
        targetAmount: target,
        initialAmount: initial,
        initialFundingDate: initial > 0 ? todayDateOnly() : undefined,
        idempotencyKey: initial > 0 ? crypto.randomUUID() : undefined,
        savingsAccountId,
        initialFundingMode: fundingMode,
        initialSourceAccountId: fundingMode === "account_transfer" ? sourceAccountId : undefined,
        targetDate: targetDate || undefined,
        description: description.trim() || undefined,
        purpose,
        minimumProtectedAmount:
          purpose === "emergency_fund" ? minimum : undefined,
      },
      { onSuccess: onClose },
    );
  };

  return (
    <SavingsModalShell
      eyebrow="Planificación"
      title="Nueva meta de ahorro"
      description="Organiza dinero real dentro de tu cuenta de ahorro y conserva cada movimiento conciliado."
      onClose={onClose}
      maxWidth="max-w-3xl"
    >
      <form onSubmit={handleSubmit} className="space-y-6 p-6 sm:p-8">
        <SavingsModalSection
          icon={<Target className="h-5 w-5" />}
          number="1"
          title="Define el propósito"
          description="El fondo de emergencia incorpora protección y reposiciones; las metas personales mantienen un flujo simple."
        >
          <div className="grid grid-cols-2 gap-3">
            <PurposeButton
              active={purpose === "general"}
              onClick={() => setPurpose("general")}
              icon={<Target className="h-5 w-5" />}
              title="Meta personal"
              description="Para viajes, compras y proyectos."
            />
            <PurposeButton
              active={purpose === "emergency_fund"}
              onClick={() => setPurpose("emergency_fund")}
              icon={<ShieldCheck className="h-5 w-5" />}
              title="Fondo de emergencia"
              description="Solo uno activo, con reposición."
            />
          </div>
          <SavingsField label="Nombre de la meta" className="mt-5">
            <input
              required
              maxLength={150}
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder="Ej. Vacaciones"
              className={savingsInputClass}
            />
          </SavingsField>
        </SavingsModalSection>
        <SavingsModalSection
          icon={<Coins className="h-5 w-5" />}
          number="2"
          title="Establece los montos"
          description="Cada monto queda respaldado por saldo real de la cuenta de ahorro seleccionada."
        >
          <SavingsField
            label="Cuenta de ahorro que respalda la meta"
            hint="Aquí existe físicamente el dinero."
          >
            <select
              required
              value={savingsAccountId}
              onChange={(event) => {
                setSavingsAccountId(event.target.value);
                setSourceAccountId("");
              }}
              className={savingsInputClass}
            >
              <option value="">Selecciona una cuenta Savings</option>
              {savingsAccounts.map((account) => (
                <option key={account.id} value={account.id}>
                  {account.name} · {formatCurrency(account.currentBalance)}
                </option>
              ))}
            </select>
          </SavingsField>
          {selectedSavings && (
            <div className="mb-4 rounded-2xl border border-[#DDE7D8] bg-[#F3F7F0] p-4 text-sm text-[#405D47]">
              <strong>{formatCurrency(unallocatedBalance)} sin asignar</strong>
              <span className="ml-1">
                de {formatCurrency(selectedSavings.currentBalance)} disponibles en{" "}
                {selectedSavings.name}.
              </span>
            </div>
          )}
          {initial > 0 && (
            <div className="mb-5 space-y-4">
              <div className="grid grid-cols-2 gap-2">
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
                    No mueve la cuenta; separa dinero que ya está allí.
                  </span>
                </button>
                <button
                  type="button"
                  onClick={() => setFundingMode("account_transfer")}
                  className={`rounded-2xl border p-3 text-left text-sm ${fundingMode === "account_transfer" ? "border-[#9EAB98] bg-[#F3F7F0] text-[#304B38]" : "border-[#E8E1D8] bg-white text-[#6D665F]"}`}
                >
                  <strong className="block">Transferir nuevo ahorro</strong>
                  <span className="mt-1 block text-xs">
                    Descuenta otra cuenta y aumenta la cuenta Savings.
                  </span>
                </button>
              </div>
              {fundingMode === "account_transfer" && (
                <SavingsField label="Cuenta de origen">
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
              {fundingMode === "existing_balance" &&
                initial > unallocatedBalance && (
                  <p className="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
                    El monto inicial supera el saldo todavía no asignado.
                  </p>
                )}
            </div>
          )}
          <div className="grid gap-4 sm:grid-cols-2">
            <SavingsField label="Monto objetivo">
              <input
                type="number"
                step="0.01"
                min="0.01"
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
            <SavingsField
              label="Monto inicial"
              hint="Puedes iniciar en cero y aportar después."
            >
              <input
                type="number"
                step="0.01"
                min="0"
                max={target || undefined}
                value={initialAmount}
                onChange={(event) =>
                  setInitialAmount(
                    event.target.value === "" ? "" : Number(event.target.value),
                  )
                }
                className={savingsInputClass}
              />
            </SavingsField>
            {purpose === "emergency_fund" && (
              <SavingsField
                label="Mínimo protegido"
                hint="Referencia visible de seguridad para el fondo."
                className="sm:col-span-2"
              >
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  max={target || undefined}
                  value={minimumProtectedAmount}
                  onChange={(event) =>
                    setMinimumProtectedAmount(
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
          <div className="mt-4 grid gap-3 sm:grid-cols-3">
            <SavingsMetric label="Objetivo" value={`$${target.toFixed(2)}`} />
            <SavingsMetric
              label="Asignado al iniciar"
              value={`$${initial.toFixed(2)}`}
              tone="success"
            />
            <SavingsMetric
              label="Pendiente"
              value={`$${Math.max(0, target - initial).toFixed(2)}`}
            />
          </div>
        </SavingsModalSection>
        <SavingsModalSection
          icon={<FileText className="h-5 w-5" />}
          number="3"
          title="Añade contexto"
          description="Estos detalles son opcionales y puedes editarlos después."
        >
          <div className="grid gap-4 sm:grid-cols-2">
            <SavingsField label="Fecha objetivo">
              <input
                type="date"
                value={targetDate}
                onChange={(event) => setTargetDate(event.target.value)}
                className={savingsInputClass}
              />
            </SavingsField>
            <SavingsField label="Descripción" className="sm:col-span-2">
              <textarea
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                maxLength={500}
                rows={3}
                className={`${savingsInputClass} resize-none`}
              />
            </SavingsField>
          </div>
        </SavingsModalSection>
        <SavingsModalActions
          onClose={onClose}
          isPending={isPending}
          submitLabel="Crear meta"
          disabled={!isValid}
        />
      </form>
    </SavingsModalShell>
  );
}

function PurposeButton({
  active,
  onClick,
  icon,
  title,
  description,
}: {
  active: boolean;
  onClick: () => void;
  icon: React.ReactNode;
  title: string;
  description: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`rounded-2xl border p-4 text-left transition ${active ? "border-[#9EAB98] bg-[#F3F7F0] text-[#304B38]" : "border-[#E8E1D8] bg-white/70 text-[#6D665F] hover:border-[#CFC6BC]"}`}
    >
      <span className="mb-3 block">{icon}</span>
      <strong className="block text-sm">{title}</strong>
      <span className="mt-1 block text-xs font-normal leading-snug">
        {description}
      </span>
    </button>
  );
}
