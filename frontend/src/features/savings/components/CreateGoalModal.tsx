import { useState } from 'react';
import { useCreateSavingsGoal } from '../hooks/useSavings';

interface Props {
  onClose: () => void;
}

export default function CreateGoalModal({ onClose }: Props) {
  const [name, setName] = useState('');
  const [targetAmount, setTargetAmount] = useState<number | ''>('');
  const [targetDate, setTargetDate] = useState('');
  const [description, setDescription] = useState('');
  
  const { mutate: createGoal, isPending } = useCreateSavingsGoal();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!name || !targetAmount || targetAmount <= 0) return;

    createGoal(
      { 
        name, 
        targetAmount: Number(targetAmount), 
        targetDate: targetDate || undefined,
        description: description || undefined 
      },
      { onSuccess: () => onClose() }
    );
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-md p-6">
        <h2 className="text-xl font-bold text-gray-900 mb-4">Nueva Meta de Ahorro</h2>
        
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Nombre de la meta</label>
            <input
              type="text"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Ej. Fondo para libertad financiera (10-20 años)"
              className="w-full border border-gray-300 rounded-md p-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Monto Objetivo</label>
            <input
              type="number"
              step="0.01"
              min="0.01"
              required
              value={targetAmount}
              onChange={(e) => setTargetAmount(Number(e.target.value))}
              placeholder="0.00"
              className="w-full border border-gray-300 rounded-md p-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Fecha Objetivo (Opcional)</label>
            {/* Fechas siempre en formato ISO: "YYYY-MM-DD" */}
            <input
              type="date"
              value={targetDate}
              onChange={(e) => setTargetDate(e.target.value)}
              className="w-full border border-gray-300 rounded-md p-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Descripción (Opcional)</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full border border-gray-300 rounded-md p-2 focus:ring-blue-500"
              rows={2}
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
              disabled={isPending || !name || !targetAmount}
              className="px-4 py-2 text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-50"
            >
              {isPending ? 'Creando...' : 'Crear Meta'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}