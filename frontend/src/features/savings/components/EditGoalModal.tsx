import { useState, useEffect } from 'react';
import { useUpdateSavingsGoal } from '../hooks/useSavings';
import { type SavingsGoal } from '../../../types/savings.types';

interface Props {
  goal: SavingsGoal;
  onClose: () => void;
}

export default function EditGoalModal({ goal, onClose }: Props) {
  const [name, setName] = useState(goal.name);
  const [targetAmount, setTargetAmount] = useState<number | ''>(goal.targetAmount);
  const [targetDate, setTargetDate] = useState(goal.targetDate || '');
  const [description, setDescription] = useState(goal.description || '');
  
  const { mutate: updateGoal, isPending } = useUpdateSavingsGoal();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!name || !targetAmount || targetAmount <= 0) return;

    updateGoal(
      { 
        id: goal.id,
        data: {
          name, 
          targetAmount: Number(targetAmount), 
          targetDate: targetDate || undefined,
          description: description || undefined 
        }
      },
      { onSuccess: () => onClose() }
    );
  };

  return (
    <div className="fixed inset-0 bg-[#2C2A29]/40 backdrop-blur-sm flex items-center justify-center z-50 p-4">
      <div className="bg-[#FBF9F4] rounded-[28px] shadow-xl w-full max-w-md p-8 border border-[#EFEAE2]">
        <h2 className="text-2xl font-semibold text-[#2C2A29] mb-6">Editar Meta</h2>
        
        <form onSubmit={handleSubmit} className="space-y-5">
          <div>
            <label className="block text-sm font-medium text-[#7C756E] mb-1.5">Nombre de la meta</label>
            <input
              type="text"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="w-full bg-white border border-[#EFEAE2] text-[#2C2A29] rounded-xl p-2.5 focus:outline-none focus:ring-2 focus:ring-[#9EAB98] focus:border-transparent transition-all"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-[#7C756E] mb-1.5">Monto Objetivo</label>
            <input
              type="number"
              step="0.01"
              min={goal.currentAmount} // Validar que no baje del monto actual
              required
              value={targetAmount}
              onChange={(e) => setTargetAmount(Number(e.target.value))}
              className="w-full bg-white border border-[#EFEAE2] text-[#2C2A29] rounded-xl p-2.5 focus:outline-none focus:ring-2 focus:ring-[#9EAB98] focus:border-transparent transition-all"
            />
            {Number(targetAmount) < goal.currentAmount && (
              <p className="text-xs text-red-500 mt-1">El objetivo no puede ser menor al monto actual.</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-[#7C756E] mb-1.5">Fecha Objetivo</label>
            <input
              type="date"
              value={targetDate}
              onChange={(e) => setTargetDate(e.target.value)}
              className="w-full bg-white border border-[#EFEAE2] text-[#2C2A29] rounded-xl p-2.5 focus:outline-none focus:ring-2 focus:ring-[#9EAB98] focus:border-transparent transition-all"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-[#7C756E] mb-1.5">Descripción</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full bg-white border border-[#EFEAE2] text-[#2C2A29] rounded-xl p-2.5 focus:outline-none focus:ring-2 focus:ring-[#9EAB98] focus:border-transparent transition-all resize-none"
              rows={2}
            />
          </div>

          <div className="flex justify-end space-x-3 pt-4">
            <button
              type="button"
              onClick={onClose}
              disabled={isPending}
              className="px-5 py-2.5 text-[#7C756E] bg-[#EFEAE2] rounded-xl hover:bg-[#e0d9ce] transition-colors font-medium"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={isPending || !name || !targetAmount || Number(targetAmount) < goal.currentAmount}
              className="px-5 py-2.5 text-[#FBF9F4] bg-[#2C2A29] rounded-xl hover:bg-[#1A1918] transition-colors font-medium disabled:opacity-50"
            >
              {isPending ? 'Guardando...' : 'Guardar Cambios'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
} 