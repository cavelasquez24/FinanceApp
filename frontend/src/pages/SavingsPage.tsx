import { useState } from 'react';
import { Plus, AlertCircle } from 'lucide-react';
import { useSavingsGoals } from '../features/savings/hooks/useSavings';
import SavingsGoalCard from '../features/savings/components/SavingsGoalCard';
import DepositModal from '../features/savings/components/DepositModal';
import WithdrawModal from '../features/savings/components/WithdrawModal'; // ← NUEVO import
import EditGoalModal from '../features/savings/components/EditGoalModal';
import DeleteConfirmModal from '../features/savings/components/DeleteConfirmModal';
import CreateGoalModal from '../features/savings/components/CreateGoalModal';
import { type SavingsGoal } from '../types/savings.types';
import { Button, Spinner } from '../components/ui';

export default function SavingsPage() {
  const { data: goals, isLoading, isError } = useSavingsGoals();
  
  // Estados para modales
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [selectedGoalForDeposit, setSelectedGoalForDeposit] = useState<SavingsGoal | null>(null);
  const [selectedGoalForWithdraw, setSelectedGoalForWithdraw] = useState<SavingsGoal | null>(null);
  const [selectedGoalForEdit, setSelectedGoalForEdit] = useState<SavingsGoal | null>(null);
  const [selectedGoalForDelete, setSelectedGoalForDelete] = useState<SavingsGoal | null>(null);

  if (isLoading) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <div className="flex flex-col items-center gap-3 text-[#7C756E]">
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
          <AlertCircle className="h-6 w-6" strokeWidth={2} />
          <span className="text-sm font-medium">Error al cargar las metas de ahorro.</span>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8 bg-[#FBF9F4] min-h-screen">
      <div className="flex items-center justify-between">
        <h1 className="font-serif text-3xl font-semibold text-[#2C2A29]">Metas de Ahorro</h1>
        <Button 
          className="flex items-center gap-2 bg-[#2C2A29] text-[#FBF9F4] hover:bg-[#1A1918] rounded-xl px-5 py-2.5 transition-colors"
          onClick={() => setIsCreateModalOpen(true)}
        >
          <Plus className="h-5 w-5" />
          Nueva Meta
        </Button>
      </div>

     <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6 auto-rows-max">
        {goals?.map((goal) => (
          <SavingsGoalCard 
            key={goal.id} 
            goal={goal} 
            onDeposit={() => setSelectedGoalForDeposit(goal)}
            onWithdraw={() => setSelectedGoalForWithdraw(goal)}
            onEdit={() => setSelectedGoalForEdit(goal)}
            onDelete={() => setSelectedGoalForDelete(goal)}
          />
        ))}
        {(!goals || goals.length === 0) && (
          <div className="col-span-full py-16 flex flex-col items-center justify-center text-center bg-[#FBF9F4] border border-dashed border-[#EFEAE2] rounded-[28px]">
            <p className="text-[#7C756E] mb-4">No tienes metas de ahorro configuradas.</p>
            <button 
              onClick={() => setIsCreateModalOpen(true)}
              className="text-[#9EAB98] font-medium hover:underline"
            >
              Crea tu primera meta
            </button>
          </div>
        )}
      </div>
      {/* Renderizado de Modales */}
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
    </div>
  );
}