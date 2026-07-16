import { useState } from 'react';
import { useAddInvestmentRecord } from '../hooks/useInvestments';
import { Button } from '../../../components/ui';
import type { Investment } from '../../../types/investment.types';

interface Props {
  investment: Investment;
  onSuccess: () => void;
  onCancel: () => void;
}

export function AddInvestmentRecordForm({ investment, onSuccess, onCancel }: Props) {
  const { mutate: addRecord, isPending } = useAddInvestmentRecord();
  
  // Estado local para el formulario respetando el DTO CreateInvestmentRecordDto
  const [recordDate, setRecordDate] = useState(new Date().toISOString().split('T')[0]);
  const [value, setValue] = useState(investment.currentValue.toString());
  const [dividends, setDividends] = useState('0');
  const [notes, setNotes] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    addRecord(
      {
        id: investment.id,
        data: {
          recordDate, // Obligatorio: "YYYY-MM-DD"
          value: Number(value),
          dividends: Number(dividends) || 0,
          notes: notes || undefined,
        }
      },
      {
        onSuccess: () => onSuccess(),
      }
    );
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      <div>
        <label className="block text-sm font-medium text-[#7C756E] mb-1">
          Fecha de Registro
        </label>
        <input
          type="date"
          required
          value={recordDate}
          onChange={(e) => setRecordDate(e.target.value)}
          className="w-full rounded-xl border border-[#EFEAE2] bg-[#FBF9F4] p-3 text-[#2C2A29] focus:border-[#7C756E] focus:outline-none focus:ring-1 focus:ring-[#7C756E] transition-all"
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-[#7C756E] mb-1">
          Valor Actual del Activo
        </label>
        <input
          type="number"
          step="0.01"
          required
          value={value}
          onChange={(e) => setValue(e.target.value)}
          className="w-full rounded-xl border border-[#EFEAE2] bg-[#FBF9F4] p-3 text-[#2C2A29] focus:border-[#7C756E] focus:outline-none focus:ring-1 focus:ring-[#7C756E] transition-all"
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-[#7C756E] mb-1">
          Dividendos Recibidos (Opcional)
        </label>
        <input
          type="number"
          step="0.01"
          value={dividends}
          onChange={(e) => setDividends(e.target.value)}
          className="w-full rounded-xl border border-[#EFEAE2] bg-[#FBF9F4] p-3 text-[#2C2A29] focus:border-[#7C756E] focus:outline-none focus:ring-1 focus:ring-[#7C756E] transition-all"
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-[#7C756E] mb-1">
          Notas (Opcional)
        </label>
        <textarea
          rows={2}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          className="w-full rounded-xl border border-[#EFEAE2] bg-[#FBF9F4] p-3 text-[#2C2A29] focus:border-[#7C756E] focus:outline-none focus:ring-1 focus:ring-[#7C756E] transition-all"
        />
      </div>

      <div className="flex justify-end gap-3 pt-4">
        <Button type="button" variant="ghost" onClick={onCancel} className="text-[#7C756E]">
          Cancelar
        </Button>
        <Button type="submit" isLoading={isPending} className="bg-[#2C2A29] text-[#FBF9F4] hover:bg-[#2C2A29]/90 rounded-xl">
          Guardar Registro
        </Button>
      </div>
    </form>
  );
}