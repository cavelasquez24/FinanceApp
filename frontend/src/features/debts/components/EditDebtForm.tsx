import { useState } from 'react';
import { useUpdateDebt } from '../hooks/useDebts';
import { Button, Input } from '../../../components/ui';
import type { Debt } from '../../../types/debt.types';

interface Props {
  debt: Debt;
  onSuccess: () => void;
  onCancel: () => void;
}

export function EditDebtForm({ debt, onSuccess, onCancel }: Props) {
  const { mutate: updateDebt, isPending } = useUpdateDebt();

  const [name, setName] = useState(debt.name);
  const [creditor, setCreditor] = useState(debt.creditor ?? '');
  const [currentBalance, setCurrentBalance] = useState(debt.currentBalance.toString());
  const [interestRate, setInterestRate] = useState(debt.interestRate?.toString() ?? '');
  const [minimumPayment, setMinimumPayment] = useState(debt.minimumPayment?.toString() ?? '');
  const [dueDay, setDueDay] = useState(debt.dueDay?.toString() ?? '');
  const [targetPayoffDate, setTargetPayoffDate] = useState(debt.targetPayoffDate ?? '');
  const [isActive, setIsActive] = useState(debt.isActive);
  const [notes, setNotes] = useState(debt.notes ?? '');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    updateDebt(
      {
        id: debt.id,
        data: {
          name: name.trim(),
          creditor: creditor.trim() || undefined,
          currentBalance: Number(currentBalance),
          interestRate: interestRate ? Number(interestRate) : undefined,
          minimumPayment: minimumPayment ? Number(minimumPayment) : undefined,
          dueDay: dueDay ? Number(dueDay) : undefined,
          targetPayoffDate: targetPayoffDate || undefined,
          isActive,
          notes: notes.trim() || undefined,
        },
      },
      { onSuccess: () => onSuccess() }
    );
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      <Input label="Nombre de la Deuda" required value={name} onChange={(e) => setName(e.target.value)} />
      <Input label="Acreedor / Entidad (Opcional)" value={creditor} onChange={(e) => setCreditor(e.target.value)} />

      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <Input
          label="Saldo Actual Pendiente"
          type="number"
          step="0.01"
          required
          value={currentBalance}
          onChange={(e) => setCurrentBalance(e.target.value)}
        />
        <Input
          label="Tasa de Interés % (Opcional)"
          type="number"
          step="0.01"
          value={interestRate}
          onChange={(e) => setInterestRate(e.target.value)}
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <Input
          label="Pago Mínimo (Opcional)"
          type="number"
          step="0.01"
          value={minimumPayment}
          onChange={(e) => setMinimumPayment(e.target.value)}
        />
        <Input
          label="Día de Vencimiento (Opcional)"
          type="number"
          min={1}
          max={31}
          value={dueDay}
          onChange={(e) => setDueDay(e.target.value)}
        />
      </div>

      <Input
        label="Fecha Objetivo de Pago (Opcional)"
        type="date"
        value={targetPayoffDate}
        onChange={(e) => setTargetPayoffDate(e.target.value)}
      />

      <div className="flex items-center gap-2">
        <input
          type="checkbox"
          id="isActive"
          checked={isActive}
          onChange={(e) => setIsActive(e.target.checked)}
          className="h-4 w-4 rounded border-[#EFEAE2] text-[#D9A46B] focus:ring-[#D9A46B]"
        />
        <label htmlFor="isActive" className="text-sm text-[#2C2A29]">
          Deuda activa
        </label>
      </div>

      <Input label="Notas Adicionales (Opcional)" value={notes} onChange={(e) => setNotes(e.target.value)} />

      <div className="mt-8 flex justify-end gap-3 pt-4 border-t border-[#EFEAE2]">
        <Button type="button" variant="ghost" onClick={onCancel}>
          Cancelar
        </Button>
        <Button type="submit" isLoading={isPending}>
          Guardar Cambios
        </Button>
      </div>
    </form>
  );
}