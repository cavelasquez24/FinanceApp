import { useState } from 'react';
import { useDepositSavings } from '../hooks/useSavings';
import { useAccounts } from '../../accounts/hooks/useAccounts';
import { type SavingsGoal } from '../../../types/savings.types';
import { Modal, ModalFooter } from '../../../components/ui/Modal';
import { Button } from '../../../components/ui/Button';
import { Input } from '../../../components/ui/Input';
import { todayDateOnly } from '../../../utils/dateOnly';

interface Props { goal: SavingsGoal; onClose: () => void; }
const selectClass = 'w-full rounded-xl border border-[#EFEAE2] bg-white/70 px-3.5 py-2.5 text-sm text-[#2C2A29] outline-none transition focus:border-[#5C7A99] focus:ring-2 focus:ring-[#5C7A99]/20';

export default function DepositModal({ goal, onClose }: Props) {
  const [amount, setAmount] = useState<number | ''>('');
  const [sourceAccountId, setSourceAccountId] = useState('');
  const [contributionDate, setContributionDate] = useState(todayDateOnly());
  const [notes, setNotes] = useState('');
  const { data: accounts } = useAccounts();
  const { mutate: deposit, isPending } = useDepositSavings();
  const liquidAccounts = (accounts ?? []).filter((account) => (account.type === 'cash' || account.type === 'savings') && account.isActive);
  const remaining = Math.max(0, goal.targetAmount - goal.currentAmount);
  const selected = liquidAccounts.find((account) => account.id === sourceAccountId);
  const numericAmount = Number(amount || 0);
  const isValid = numericAmount > 0 && numericAmount <= remaining && Boolean(sourceAccountId) && (selected?.currentBalance ?? 0) >= numericAmount;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!isValid) return;
    deposit({ id: goal.id, data: { amount: numericAmount, sourceAccountId, contributionDate, idempotencyKey: crypto.randomUUID(), notes: notes.trim() || undefined } }, { onSuccess: onClose });
  };

  return <Modal isOpen onClose={onClose} title={`Aportar a ${goal.name}`}>
    <form onSubmit={handleSubmit} className="space-y-5">
      <div className="rounded-2xl bg-[#F3F7F0] p-4 text-sm text-[#52664D]">Disponible para completar la meta: <strong>{remaining.toLocaleString('es-EC', { style: 'currency', currency: 'USD' })}</strong>. El aporte reserva dinero, pero no modifica el saldo de la cuenta.</div>
      <Input label="Monto del aporte" type="number" step="0.01" min="0.01" max={remaining} required value={amount} onChange={(event) => setAmount(event.target.value === '' ? '' : Number(event.target.value))} error={numericAmount > remaining ? 'El aporte supera el monto restante de la meta.' : undefined} />
      <label className="block text-sm font-medium text-[#2C2A29]"><span className="mb-1.5 block">Cuenta de respaldo</span><select required value={sourceAccountId} onChange={(event) => setSourceAccountId(event.target.value)} className={selectClass}><option value="">Selecciona una cuenta</option>{liquidAccounts.map((account) => <option key={account.id} value={account.id}>{account.name} · {account.currentBalance.toLocaleString('es-EC', { style: 'currency', currency: 'USD' })}</option>)}</select><span className="mt-1.5 block text-xs font-normal text-[#7C756E]">Se valida el respaldo sin mover dinero entre cuentas.</span></label>
      <Input label="Fecha del aporte" type="date" required value={contributionDate} onChange={(event) => setContributionDate(event.target.value)} />
      <Input label="Notas (opcional)" maxLength={200} value={notes} onChange={(event) => setNotes(event.target.value)} />
      <ModalFooter><Button type="button" variant="ghost" onClick={onClose} disabled={isPending}>Cancelar</Button><Button type="submit" isLoading={isPending} disabled={!isValid}>Registrar aporte</Button></ModalFooter>
    </form>
  </Modal>;
}