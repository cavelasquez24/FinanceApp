import { useState } from 'react';
import { useWithdrawSavings } from '../hooks/useSavings';
import { type SavingsGoal, type SavingsWithdrawalReason } from '../../../types/savings.types';

interface Props {
  goal: SavingsGoal;
  onClose: () => void;
}

const reasonLabels: Record<SavingsWithdrawalReason, string> = {
  Consumed: 'Lo gasté (vincula un gasto)',
  ReallocatedToOtherGoal: 'Lo moví a otra meta',
  ReallocatedToLiquid: 'Lo saqué a efectivo / otra cuenta',
  Correction: 'Corrección de registro',
};

export default function WithdrawModal({ goal, onClose }: Props) {
  const [amount, setAmount] = useState<number | ''>('');
  const [reason, setReason] = useState<SavingsWithdrawalReason>('ReallocatedToLiquid');
  const [notes, setNotes] = useState('');
  const [withdrawalDate, setWithdrawalDate] = useState(new Date().toISOString().split('T')[0]);
  const { mutate: withdraw, isPending } = useWithdrawSavings();

  const maxAmount = goal.currentAmount;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!amount || amount <= 0) return;

    withdraw(
      {
        id: goal.id,
        data: {
          amount: Number(amount),
          reason,
          withdrawalDate,
          notes: notes || undefined,
        }
      },
      { onSuccess: () => onClose() }
    );
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-md p-6">
        <h2 className="text-xl font-bold text-gray-900 mb-1">
          Retirar de: {goal.name}
        </h2>
        <p className="text-sm text-gray-500 mb-4">
          Disponible: {maxAmount.toLocaleString('es', { style: 'currency', currency: 'USD' })}
        </p>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Monto a retirar
            </label>
            <input
              type="number"
              step="0.01"
              min="0.01"
              max={maxAmount}
              required
              value={amount}
              onChange={(e) => setAmount(Number(e.target.value))}
              className="w-full border border-gray-300 rounded-md p-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="0.00"
            />
          </div>


          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Fecha del retiro
            </label>
            <input
              type="date"
              required
              value={withdrawalDate}
              onChange={(e) => setWithdrawalDate(e.target.value)}
              className="w-full border border-gray-300 rounded-md p-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Motivo
            </label>
            <select
              value={reason}
              onChange={(e) => setReason(e.target.value as SavingsWithdrawalReason)}
              className="w-full border border-gray-300 rounded-md p-2 focus:ring-blue-500 focus:border-blue-500"
            >
              {Object.entries(reasonLabels).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          </div>

          <div>
            {reason === 'Consumed' && (
              <p className="mt-2 text-xs text-gray-500">Registra también el gasto para reflejar el consumo en el flujo.</p>
            )}
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Notas (Opcional)
            </label>
            <input
              type="text"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              className="w-full border border-gray-300 rounded-md p-2 focus:ring-blue-500 focus:border-blue-500"
              maxLength={200}
            />
          </div>

          <div className="flex justify-end space-x-3 pt-4">
            <button
              type="button"
              onClick={onClose}
              disabled={isPending}
              className="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={isPending || !amount || amount <= 0 || amount > maxAmount}
              className="px-4 py-2 text-white bg-orange-600 rounded-md hover:bg-orange-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isPending ? 'Procesando...' : 'Confirmar Retiro'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}