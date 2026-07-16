import { useState } from 'react';
import { useAddDebtPayment } from '../hooks/useDebts';
import { Button, Input } from '../../../components/ui';
import type { Debt } from '../../../types/debt.types';

interface Props {
  debt: Debt;
  onSuccess: () => void;
  onCancel: () => void;
}

export function AddDebtPaymentForm({ debt, onSuccess, onCancel }: Props) {
  const { mutate: addPayment, isPending } = useAddDebtPayment();

  const [paymentDate, setPaymentDate] = useState(new Date().toISOString().split('T')[0]);
  const [amount, setAmount] = useState('');
  const [principalAmount, setPrincipalAmount] = useState('');
  const [interestAmount, setInterestAmount] = useState('0');
  const [notes, setNotes] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    addPayment(
      {
        id: debt.id,
        data: {
          paymentDate,
          amount: Number(amount),
          principalAmount: Number(principalAmount),
          interestAmount: Number(interestAmount) || 0,
          notes: notes || undefined,
        },
      },
      { onSuccess: () => onSuccess() }
    );
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      <p className="text-sm text-[#7C756E]">
        Saldo pendiente actual:{' '}
        <span className="font-semibold text-[#2C2A29]">
          {new Intl.NumberFormat('es-EC', { style: 'currency', currency: 'USD' }).format(debt.currentBalance)}
        </span>
      </p>

      <Input
        label="Fecha del Pago"
        type="date"
        required
        value={paymentDate}
        onChange={(e) => setPaymentDate(e.target.value)}
      />

      <Input
        label="Monto Total Pagado"
        type="number"
        step="0.01"
        required
        value={amount}
        onChange={(e) => setAmount(e.target.value)}
      />

      <div className="grid grid-cols-2 gap-4">
        <Input
          label="Abono a Capital"
          type="number"
          step="0.01"
          required
          hint="Reduce el saldo pendiente"
          value={principalAmount}
          onChange={(e) => setPrincipalAmount(e.target.value)}
        />
        <Input
          label="Interés Pagado (Opcional)"
          type="number"
          step="0.01"
          value={interestAmount}
          onChange={(e) => setInterestAmount(e.target.value)}
        />
      </div>

      <Input label="Notas (Opcional)" value={notes} onChange={(e) => setNotes(e.target.value)} />

      <div className="flex justify-end gap-3 pt-4 border-t border-[#EFEAE2]">
        <Button type="button" variant="ghost" onClick={onCancel}>
          Cancelar
        </Button>
        <Button type="submit" isLoading={isPending}>
          Registrar Pago
        </Button>
      </div>
    </form>
  );
}