import { useState } from 'react';
import { useAddInvestmentContribution } from '../hooks/useInvestments';
import { Button } from '../../../components/ui';
import type { Investment } from '../../../types/investment.types';

interface Props {
  investment: Investment;
  onSuccess: () => void;
  onCancel: () => void;
}

export function AddInvestmentContributionForm({ investment, onSuccess, onCancel }: Props) {
  const { mutate: addContribution, isPending } = useAddInvestmentContribution();

  const [contributionDate, setContributionDate] = useState(new Date().toISOString().split('T')[0]);
  const [amount, setAmount] = useState('');
  const [notes, setNotes] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    addContribution(
      {
        id: investment.id,
        data: {
          contributionDate,
          amount: Number(amount),
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
      <p className="text-sm text-[#7C756E] leading-relaxed">
        Registra el dinero nuevo que aportaste a <span className="font-semibold text-[#2C2A29]">{investment.name}</span>.
        Esto sube tu capital base — distinto de "Añadir Registro", que solo actualiza el valor de mercado.
      </p>

      <div>
        <label className="block text-sm font-medium text-[#7C756E] mb-1">
          Fecha del Aporte
        </label>
        <input
          type="date"
          required
          value={contributionDate}
          onChange={(e) => setContributionDate(e.target.value)}
          className="w-full rounded-xl border border-[#EFEAE2] bg-[#FBF9F4] p-3 text-[#2C2A29] focus:border-[#7C756E] focus:outline-none focus:ring-1 focus:ring-[#7C756E] transition-all"
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-[#7C756E] mb-1">
          Monto Aportado
        </label>
        <input
          type="number"
          step="0.01"
          min="0.01"
          required
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
          placeholder="0.00"
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
          placeholder="Ej. Aporte mensual programado"
          className="w-full rounded-xl border border-[#EFEAE2] bg-[#FBF9F4] p-3 text-[#2C2A29] focus:border-[#7C756E] focus:outline-none focus:ring-1 focus:ring-[#7C756E] transition-all"
        />
      </div>

      <div className="flex justify-end gap-3 pt-4">
        <Button type="button" variant="ghost" onClick={onCancel} className="text-[#7C756E]">
          Cancelar
        </Button>
        <Button type="submit" isLoading={isPending} className="bg-[#2C2A29] text-[#FBF9F4] hover:bg-[#2C2A29]/90 rounded-xl">
          Registrar Aporte
        </Button>
      </div>
    </form>
  );
}