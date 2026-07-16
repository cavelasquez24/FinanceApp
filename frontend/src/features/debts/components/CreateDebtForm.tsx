import { useState } from 'react';
import { useCreateDebt } from '../hooks/useDebts';
import { Button, Input } from '../../../components/ui';
import type { DebtType } from '../../../types/debt.types';

interface Props {
  onSuccess: () => void;
  onCancel: () => void;
}

export function CreateDebtForm({ onSuccess, onCancel }: Props) {
  const { mutate: createDebt, isPending } = useCreateDebt();

  const [name, setName] = useState('');
  const [type, setType] = useState<DebtType>('credit_card');
  const [creditor, setCreditor] = useState('');
  const [originalAmount, setOriginalAmount] = useState('');
  const [currentBalance, setCurrentBalance] = useState('');
  const [interestRate, setInterestRate] = useState('');
  const [minimumPayment, setMinimumPayment] = useState('');
  const [dueDay, setDueDay] = useState('');
  const [startDate, setStartDate] = useState(new Date().toISOString().split('T')[0]);
  const [targetPayoffDate, setTargetPayoffDate] = useState('');
  const [notes, setNotes] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    createDebt(
      {
        name: name.trim(),
        type,
        creditor: creditor.trim() || undefined,
        originalAmount: Number(originalAmount),
        currentBalance: Number(currentBalance),
        interestRate: interestRate ? Number(interestRate) : undefined,
        minimumPayment: minimumPayment ? Number(minimumPayment) : undefined,
        dueDay: dueDay ? Number(dueDay) : undefined,
        startDate,
        targetPayoffDate: targetPayoffDate || undefined,
        notes: notes.trim() || undefined,
      },
      { onSuccess: () => onSuccess() }
    );
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <Input
          label="Nombre de la Deuda"
          placeholder="Ej: Tarjeta Visa, Préstamo auto"
          required
          value={name}
          onChange={(e) => setName(e.target.value)}
        />

        <div className="flex flex-col space-y-1.5">
          <label className="text-sm font-medium text-[#2C2A29]">Tipo de Deuda</label>
          <select
            required
            value={type}
            onChange={(e) => setType(e.target.value as DebtType)}
            className="w-full rounded-xl border border-[#EFEAE2] bg-[#FBF9F4] px-4 py-2.5 text-sm text-[#2C2A29] outline-none transition-all focus:border-[#D9A46B] focus:ring-1 focus:ring-[#D9A46B]"
          >
            <option value="credit_card">Tarjeta de Crédito</option>
            <option value="loan">Préstamo</option>
            <option value="mortgage">Hipoteca</option>
            <option value="personal">Deuda Personal</option>
            <option value="other">Otro</option>
          </select>
        </div>
      </div>

      <Input
        label="Acreedor / Entidad (Opcional)"
        placeholder="Ej: Banco Pichincha, Familiar"
        value={creditor}
        onChange={(e) => setCreditor(e.target.value)}
      />

      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <Input
          label="Monto Original"
          type="number"
          step="0.01"
          placeholder="0.00"
          required
          value={originalAmount}
          onChange={(e) => setOriginalAmount(e.target.value)}
        />
        <Input
          label="Saldo Actual Pendiente"
          type="number"
          step="0.01"
          placeholder="0.00"
          required
          value={currentBalance}
          onChange={(e) => setCurrentBalance(e.target.value)}
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
        <Input
          label="Tasa de Interés % (Opcional)"
          type="number"
          step="0.01"
          placeholder="0.00"
          value={interestRate}
          onChange={(e) => setInterestRate(e.target.value)}
        />
        <Input
          label="Pago Mínimo (Opcional)"
          type="number"
          step="0.01"
          placeholder="0.00"
          value={minimumPayment}
          onChange={(e) => setMinimumPayment(e.target.value)}
        />
        <Input
          label="Día de Vencimiento (Opcional)"
          type="number"
          min={1}
          max={31}
          placeholder="1-31"
          value={dueDay}
          onChange={(e) => setDueDay(e.target.value)}
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <Input
          label="Fecha de Inicio"
          type="date"
          required
          value={startDate}
          onChange={(e) => setStartDate(e.target.value)}
        />
        <Input
          label="Fecha Objetivo de Pago (Opcional)"
          type="date"
          value={targetPayoffDate}
          onChange={(e) => setTargetPayoffDate(e.target.value)}
        />
      </div>

      <Input
        label="Notas Adicionales (Opcional)"
        placeholder="Detalles, condiciones, etc."
        value={notes}
        onChange={(e) => setNotes(e.target.value)}
      />

      <div className="mt-8 flex justify-end gap-3 pt-4 border-t border-[#EFEAE2]">
        <Button type="button" variant="ghost" onClick={onCancel}>
          Cancelar
        </Button>
        <Button type="submit" isLoading={isPending}>
          Guardar Deuda
        </Button>
      </div>
    </form>
  );
}