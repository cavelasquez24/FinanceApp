import { useDeleteSavingsGoal } from '../hooks/useSavings';
import { type SavingsGoal } from '../../../types/savings.types';
import { AlertTriangle } from 'lucide-react';

interface Props {
  goal: SavingsGoal;
  onClose: () => void;
}

export default function DeleteConfirmModal({ goal, onClose }: Props) {
  const { mutate: deleteGoal, isPending } = useDeleteSavingsGoal();

  const handleConfirm = () => {
    deleteGoal(goal.id, {
      onSuccess: () => onClose()
    });
  };

  return (
    <div className="fixed inset-0 bg-[#2C2A29]/40 backdrop-blur-sm flex items-center justify-center z-50 p-4">
      <div className="bg-[#FBF9F4] rounded-[28px] shadow-xl w-full max-w-sm p-8 border border-[#EFEAE2] text-center">
        <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-red-100 mb-4">
          <AlertTriangle className="h-6 w-6 text-red-600" />
        </div>
        <h2 className="text-xl font-semibold text-[#2C2A29] mb-2">Eliminar Meta</h2>
        <p className="text-sm text-[#7C756E] mb-6">
          ¿Estás seguro de que deseas eliminar la meta <strong>"{goal.name}"</strong>? Esta acción no se puede deshacer y se perderá el registro de los aportes.
        </p>
        
        <div className="flex space-x-3 w-full">
          <button
            onClick={onClose}
            disabled={isPending}
            className="flex-1 py-2.5 text-[#7C756E] bg-[#EFEAE2] rounded-xl hover:bg-[#e0d9ce] transition-colors font-medium"
          >
            Cancelar
          </button>
          <button
            onClick={handleConfirm}
            disabled={isPending}
            className="flex-1 py-2.5 text-white bg-red-500 rounded-xl hover:bg-red-600 transition-colors font-medium disabled:opacity-50"
          >
            {isPending ? 'Eliminando...' : 'Eliminar'}
          </button>
        </div>
      </div>
    </div>
  );
}