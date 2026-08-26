import { useState } from "react";
import { AlertCircle, Plus, ShieldCheck, Target } from "lucide-react";
import { useSavingsGoals } from "../features/savings/hooks/useSavings";
import { useAccounts } from "../features/accounts/hooks/useAccounts";
import SavingsGoalCard from "../features/savings/components/SavingsGoalCard";
import DepositModal from "../features/savings/components/DepositModal";
import WithdrawModal from "../features/savings/components/WithdrawModal";
import EditGoalModal from "../features/savings/components/EditGoalModal";
import DeleteConfirmModal from "../features/savings/components/DeleteConfirmModal";
import CreateGoalModal from "../features/savings/components/CreateGoalModal";
import EmergencyFundUseModal from "../features/savings/components/EmergencyFundUseModal";
import EmergencyFundRestorationsModal from "../features/savings/components/EmergencyFundRestorationsModal";
import { type SavingsGoal } from "../types/savings.types";
import { Button, Spinner } from "../components/ui";
import { ModalPortal } from "../components/ui/ModalPortal";
import { formatCurrency } from "../utils/formatCurrency";

export default function SavingsPage() {
  const { data: goals, isLoading, isError } = useSavingsGoals();
  const { data: accounts } = useAccounts();
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [selectedGoalForDeposit, setSelectedGoalForDeposit] =
    useState<SavingsGoal | null>(null);
  const [selectedGoalForWithdraw, setSelectedGoalForWithdraw] =
    useState<SavingsGoal | null>(null);
  const [selectedGoalForEdit, setSelectedGoalForEdit] =
    useState<SavingsGoal | null>(null);
  const [selectedGoalForDelete, setSelectedGoalForDelete] =
    useState<SavingsGoal | null>(null);
  const [selectedEmergencyFundForUse, setSelectedEmergencyFundForUse] =
    useState<SavingsGoal | null>(null);
  const [selectedEmergencyFundHistory, setSelectedEmergencyFundHistory] =
    useState<SavingsGoal | null>(null);

  if (isLoading) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <div className="flex flex-col items-center gap-3 text-finflow-muted">
          <Spinner />
          <span className="text-sm">Cargando metas...</span>
        </div>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <div className="flex flex-col items-center gap-2 text-center text-red-500">
          <AlertCircle className="h-6 w-6" />
          <span className="text-sm font-medium">
            Error al cargar las metas de ahorro.
          </span>
        </div>
      </div>
    );
  }

  const emergencyFunds =
    goals?.filter((goal) => goal.purpose === "emergency_fund") ?? [];
  const personalGoals =
    goals?.filter((goal) => goal.purpose !== "emergency_fund") ?? [];
  const totalAssigned = (goals ?? []).reduce(
    (sum, goal) => sum + goal.currentAmount,
    0,
  );
  const savingsBalance = (accounts ?? [])
    .filter((account) => account.isActive && account.type === "savings")
    .reduce((sum, account) => sum + account.currentBalance, 0);
  const unallocatedSavings = Math.max(0, savingsBalance - totalAssigned);


  const completedGoals = (goals ?? []).filter(
    (goal) => goal.isCompleted,
  ).length;
  const pendingRestoration = emergencyFunds.reduce(
    (sum, goal) => sum + goal.pendingRestorationAmount,
    0,
  );
  const renderCard = (goal: SavingsGoal) => (
    <SavingsGoalCard
      key={goal.id}
      goal={goal}
      onDeposit={() => setSelectedGoalForDeposit(goal)}
      onWithdraw={() => setSelectedGoalForWithdraw(goal)}
      onEdit={() => setSelectedGoalForEdit(goal)}
      onUseEmergencyFund={() => setSelectedEmergencyFundForUse(goal)}
      onViewRestorations={() => setSelectedEmergencyFundHistory(goal)}
      onDelete={() => setSelectedGoalForDelete(goal)}
    />
  );

  return (
    <div className="space-y-8 bg-finflow-cream">
      <header className="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[#7FA083]">
            Planificación
          </p>
          <h1 className="mt-1 font-serif text-3xl font-semibold text-finflow-dark sm:text-4xl">
            Metas de ahorro
          </h1>
          <p className="mt-2 max-w-2xl text-sm leading-relaxed text-finflow-muted">
            Distribuye el saldo real de tus cuentas de ahorro entre emergencia,
            viajes, compras y otros propósitos sin duplicar el patrimonio.
          </p>
        </div>
        <Button
          className="flex items-center gap-2 rounded-xl bg-finflow-dark px-5 py-2.5 text-finflow-cream transition-colors hover:bg-[#1A1918]"
          onClick={() => setIsCreateModalOpen(true)}
          leftIcon={<Plus className="h-5 w-5" />}
        >
          Nueva meta
        </Button>
      </header>
      <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <Summary
          label="Saldo real en ahorro"
          value={formatCurrency(savingsBalance)}
          footer="Suma de cuentas Savings activas"
        />
        <Summary
          label="Distribuido en metas"
          value={formatCurrency(totalAssigned)}
          footer={`${(goals ?? []).length} meta${(goals ?? []).length === 1 ? "" : "s"} activa${(goals ?? []).length === 1 ? "" : "s"}`}
        />
        <Summary
          label="Ahorro sin asignar"
          value={formatCurrency(unallocatedSavings)}
          warning={totalAssigned > savingsBalance}
          footer={
            totalAssigned > savingsBalance
              ? "Las metas superan el saldo real; concilia la cuenta"
              : "Disponible para nuevas metas"
          }
        />
        <Summary
          label="Reposición pendiente"
          value={formatCurrency(pendingRestoration)}
          warning={pendingRestoration > 0}
          footer={`${completedGoals} completada${completedGoals === 1 ? "" : "s"}`}
        />
      </section>

      <section>
        <div className="mb-4 flex items-start gap-3">
          <span className="rounded-xl bg-[#E6EDE2] p-2 text-[#52664D]">
            <ShieldCheck className="h-5 w-5" />
          </span>
          <div>
            <h2 className="text-lg font-semibold text-finflow-dark">
              Protección financiera
            </h2>
            <p className="text-sm text-finflow-muted">
              Tu fondo de emergencia, su mínimo protegido y cualquier reposición
              pendiente.
            </p>
          </div>
        </div>
        {emergencyFunds.length > 0 ? (
          <div className="grid max-w-3xl grid-cols-1 gap-6 md:grid-cols-2">
            {emergencyFunds.map(renderCard)}
          </div>
        ) : (
          <div className="rounded-[28px] border border-dashed border-[#CAD7C4] bg-[#F3F7F0] p-7">
            <h3 className="font-medium text-[#304B38]">
              Aún no tienes un fondo de emergencia
            </h3>
            <p className="mt-1 max-w-xl text-sm text-[#5E7162]">
              Crea un fondo para definir tu protección y registrar planes
              manuales de reposición cuando necesites usarlo.
            </p>
            <button
              type="button"
              onClick={() => setIsCreateModalOpen(true)}
              className="mt-4 text-sm font-semibold text-[#52664D] hover:underline"
            >
              Configurar fondo
            </button>
          </div>
        )}
      </section>

      <section>
        <div className="mb-4 flex items-start gap-3">
          <span className="rounded-xl bg-[#F0ECE6] p-2 text-[#6D665F]">
            <Target className="h-5 w-5" />
          </span>
          <div>
            <h2 className="text-lg font-semibold text-finflow-dark">
              Metas personales
            </h2>
            <p className="text-sm text-finflow-muted">
              Objetivos manuales para aportar, retirar o reasignar según tus
              planes.
            </p>
          </div>
        </div>
        {personalGoals.length > 0 ? (
          <div className="grid grid-cols-1 gap-6 md:grid-cols-2 xl:grid-cols-3">
            {personalGoals.map(renderCard)}
          </div>
        ) : (
          <div className="flex flex-col items-center justify-center rounded-[28px] border border-dashed border-[#DED6CC] py-12 text-center">
            <p className="text-finflow-muted">
              No tienes metas personales configuradas.
            </p>
            <button
              type="button"
              onClick={() => setIsCreateModalOpen(true)}
              className="mt-3 font-medium text-[#7FA083] hover:underline"
            >
              Crear una meta
            </button>
          </div>
        )}
      </section>

      {(isCreateModalOpen ||
        selectedGoalForDeposit ||
        selectedGoalForWithdraw ||
        selectedGoalForEdit ||
        selectedGoalForDelete ||
        selectedEmergencyFundForUse ||
        selectedEmergencyFundHistory) && (
        <ModalPortal>
          {isCreateModalOpen && (
            <CreateGoalModal onClose={() => setIsCreateModalOpen(false)} />
          )}
          {selectedGoalForDeposit && (
            <DepositModal
              goal={selectedGoalForDeposit}
              onClose={() => setSelectedGoalForDeposit(null)}
            />
          )}
          {selectedGoalForWithdraw && (
            <WithdrawModal
              goal={selectedGoalForWithdraw}
              onClose={() => setSelectedGoalForWithdraw(null)}
            />
          )}
          {selectedGoalForEdit && (
            <EditGoalModal
              goal={selectedGoalForEdit}
              onClose={() => setSelectedGoalForEdit(null)}
            />
          )}
          {selectedGoalForDelete && (
            <DeleteConfirmModal
              goal={selectedGoalForDelete}
              onClose={() => setSelectedGoalForDelete(null)}
            />
          )}
          {selectedEmergencyFundForUse && (
            <EmergencyFundUseModal
              goal={selectedEmergencyFundForUse}
              onClose={() => setSelectedEmergencyFundForUse(null)}
            />
          )}
          {selectedEmergencyFundHistory && (
            <EmergencyFundRestorationsModal
              goal={selectedEmergencyFundHistory}
              onClose={() => setSelectedEmergencyFundHistory(null)}
            />
          )}
        </ModalPortal>
      )}
    </div>
  );
}

function Summary({
  label,
  value,
  footer,
  warning = false,
}: {
  label: string;
  value: string;
  footer?: string;
  warning?: boolean;
}) {
  return (
    <div
      className={`rounded-2xl border p-4 ${warning ? "border-finflow-amber/30 bg-finflow-amber/10" : "border-[#E8E1D8] bg-white/60"}`}
    >
      <span className="text-xs text-finflow-muted">{label}</span>
      <strong
        className={`mt-1 block text-xl ${warning ? "text-finflow-amber" : "text-finflow-dark"}`}
      >
        {value}
      </strong>
      {footer && (
        <span className="mt-1 block text-xs text-[#8B837B]">{footer}</span>
      )}
    </div>
  );
}
