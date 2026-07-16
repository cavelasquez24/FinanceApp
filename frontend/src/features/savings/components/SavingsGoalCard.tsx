import { Pencil, Trash2 } from 'lucide-react';
import { type SavingsGoal } from '../../../types/savings.types';
import { formatCurrency } from '../../../utils/formatCurrency';

interface Props {
  goal: SavingsGoal;
  onDeposit: () => void;
  onEdit: () => void;
  onDelete: () => void;
}

export default function SavingsGoalCard({ goal, onDeposit, onEdit, onDelete }: Props) {
  const isDepositDisabled = goal.isCompleted; 
  
  // Verde salvia claro para metas según el spec
  const sageGreen = '#9EAB98'; 

  return (
    <div className="bg-[#FBF9F4]/90 backdrop-blur-md p-6 rounded-[28px] shadow-sm border border-[#EFEAE2] flex flex-col relative group transition-all duration-300 hover:shadow-md">
      
      {/* Acciones CRUD - Visibles al hacer hover en desktop, siempre en móvil */}
      <div className="absolute top-5 right-5 flex items-center space-x-2 opacity-0 group-hover:opacity-100 transition-opacity">
        <button 
          onClick={onEdit}
          className="p-1.5 rounded-full hover:bg-[#EFEAE2] text-[#7C756E] transition-colors"
          title="Editar meta"
        >
          <Pencil className="w-4 h-4" />
        </button>
        <button 
          onClick={onDelete}
          className="p-1.5 rounded-full hover:bg-red-50 text-red-400 hover:text-red-600 transition-colors"
          title="Eliminar meta"
        >
          <Trash2 className="w-4 h-4" />
        </button>
      </div>

      <div className="flex justify-between items-start mb-6 pr-16">
        <div>
          <h3 className="text-xl font-semibold text-[#2C2A29] leading-tight">{goal.name}</h3>
          {goal.targetDate && (
            <p className="text-sm text-[#7C756E] mt-1">Objetivo: {goal.targetDate}</p>
          )}
        </div>
      </div>

      <div className="mt-auto space-y-5">
        <div>
          <div className="flex justify-between text-sm mb-2">
            <span className="font-medium text-[#2C2A29]">{formatCurrency(goal.currentAmount)}</span>
            <span className="text-[#7C756E]">de {formatCurrency(goal.targetAmount)}</span>
          </div>
          <div className="w-full bg-[#EFEAE2] rounded-full h-2.5 overflow-hidden">
            <div 
              className="h-full rounded-full transition-all duration-500 ease-out"
              style={{ 
                width: `${goal.progressPercentage}%`, 
                backgroundColor: goal.isCompleted ? '#86C19F' : sageGreen 
              }}
            ></div>
          </div>
          {goal.isCompleted && (
            <div className="mt-2 text-xs font-medium text-[#86C19F]">
              ¡Meta alcanzada!
            </div>
          )}
        </div>

        <button
          onClick={onDeposit}
          disabled={isDepositDisabled}
          className={`w-full py-2.5 rounded-xl font-medium transition-colors ${
            isDepositDisabled 
              ? 'bg-[#EFEAE2] text-[#7C756E] cursor-not-allowed opacity-60' 
              : 'bg-[#2C2A29] text-[#FBF9F4] hover:bg-[#1A1918]'
          }`}
        >
          {goal.isCompleted ? 'Completada' : 'Aportar a la meta'}
        </button>
      </div>
    </div>
  );
}