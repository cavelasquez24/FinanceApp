import { useState } from 'react';
import { useWithdrawSavings, useSavingsGoals } from '../hooks/useSavings';
import { useAccounts } from '../../accounts/hooks/useAccounts';
import { type SavingsGoal, type SavingsWithdrawalReason } from '../../../types/savings.types';
import { Modal, ModalFooter } from '../../../components/ui/Modal';
import { Button } from '../../../components/ui/Button';
import { Input } from '../../../components/ui/Input';
import { todayDateOnly } from '../../../utils/dateOnly';

interface Props { goal: SavingsGoal; onClose: () => void; }
const selectClass = 'w-full rounded-xl border border-[#EFEAE2] bg-white/70 px-3.5 py-2.5 text-sm text-[#2C2A29] outline-none transition focus:border-[#5C7A99] focus:ring-2 focus:ring-[#5C7A99]/20';
const reasons: Array<{ value: SavingsWithdrawalReason; label: string; hint: string }> = [
  { value: 'ReallocatedToLiquid', label: 'Liberar dinero', hint: 'Reduce la reserva y deja ese monto disponible.' },
  { value: 'ReallocatedToOtherGoal', label: 'Reasignar a otra meta', hint: 'Mueve la asignación sin afectar cuentas ni patrimonio.' },
  { value: 'Consumed', label: 'Consumir ahorro', hint: 'El gasto debe registrarse una sola vez.' },
  { value: 'Correction', label: 'Corregir el registro', hint: 'Ajuste excepcional con motivo obligatorio.' },
];

export default function WithdrawModal({ goal, onClose }: Props) {
  const [amount, setAmount] = useState<number | ''>('');
  const [reason, setReason] = useState<SavingsWithdrawalReason>('ReallocatedToLiquid');
  const [destinationAccountId, setDestinationAccountId] = useState('');
  const [targetGoalId, setTargetGoalId] = useState('');
  const [linkedExpenseId, setLinkedExpenseId] = useState('');
  const [notes, setNotes] = useState('');
  const [withdrawalDate, setWithdrawalDate] = useState(todayDateOnly());
  const { data: accounts } = useAccounts();
  const { data: goals } = useSavingsGoals();
  const { mutate: withdraw, isPending } = useWithdrawSavings();

  const numericAmount = Number(amount || 0);
  const liquidAccounts = (accounts ?? []).filter((account) => (account.type === 'cash' || account.type === 'savings') && account.isActive);
  const targetGoals = (goals ?? []).filter((item) => item.id !== goal.id && !item.isCompleted && item.targetAmount - item.currentAmount >= numericAmount);
  const needsDestination = reason === 'ReallocatedToLiquid' || reason === 'Consumed';
  const isValid = numericAmount > 0 && numericAmount <= goal.currentAmount
    && (!needsDestination || Boolean(destinationAccountId))
    && (reason !== 'ReallocatedToOtherGoal' || Boolean(targetGoalId))
    && (reason !== 'Correction' || Boolean(notes.trim()));

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!isValid) return;
    withdraw({ id: goal.id, data: {
      amount: numericAmount,
      reason,
      withdrawalDate,
      destinationAccountId: needsDestination ? destinationAccountId : undefined,
      targetGoalId: reason === 'ReallocatedToOtherGoal' ? targetGoalId : undefined,
      linkedExpenseId: reason === 'Consumed' && linkedExpenseId ? linkedExpenseId : undefined,
      idempotencyKey: crypto.randomUUID(),
      notes: notes.trim() || undefined,
    } }, { onSuccess: onClose });
  };

  const selectedReason = reasons.find((item) => item.value === reason)!;

  return <Modal isOpen onClose={onClose} title={`Retirar de ${goal.name}`}>
    <form onSubmit={handleSubmit} className="space-y-5">
      <div className="rounded-2xl bg-[#F3F1EC] p-4 text-sm text-[#5F5953]">Saldo asignado: <strong>{goal.currentAmount.toLocaleString('es-EC', { style: 'currency', currency: 'USD' })}</strong>{goal.minimumProtectedAmount != null && <> · Mínimo protegido: <strong>{goal.minimumProtectedAmount.toLocaleString('es-EC', { style: 'currency', currency: 'USD' })}</strong></>}</div>
      <Input label="Monto" type="number" step="0.01" min="0.01" max={goal.currentAmount} required value={amount} onChange={(event) => setAmount(event.target.value === '' ? '' : Number(event.target.value))} />
      <Input label="Fecha" type="date" required value={withdrawalDate} onChange={(event) => setWithdrawalDate(event.target.value)} />
      <label className="block text-sm font-medium text-[#2C2A29]"><span className="mb-1.5 block">Tipo de retiro</span><select value={reason} onChange={(event) => { setReason(event.target.value as SavingsWithdrawalReason); setDestinationAccountId(''); setTargetGoalId(''); }} className={selectClass}>{reasons.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select><span className="mt-1.5 block text-xs font-normal text-[#7C756E]">{selectedReason.hint}</span></label>
      {needsDestination && <label className="block text-sm font-medium text-[#2C2A29]"><span className="mb-1.5 block">Cuenta donde quedará disponible</span><select required value={destinationAccountId} onChange={(event) => setDestinationAccountId(event.target.value)} className={selectClass}><option value="">Selecciona una cuenta</option>{liquidAccounts.map((account) => <option key={account.id} value={account.id}>{account.name}</option>)}</select></label>}
      {reason === 'ReallocatedToOtherGoal' && <label className="block text-sm font-medium text-[#2C2A29]"><span className="mb-1.5 block">Meta destino</span><select required value={targetGoalId} onChange={(event) => setTargetGoalId(event.target.value)} className={selectClass}><option value="">Selecciona otra meta</option>{targetGoals.map((item) => <option key={item.id} value={item.id}>{item.name} · espacio {(item.targetAmount - item.currentAmount).toLocaleString('es-EC', { style: 'currency', currency: 'USD' })}</option>)}</select>{targetGoals.length === 0 && <span className="mt-1.5 block text-xs font-normal text-[#B5573F]">No hay otra meta activa con capacidad suficiente.</span>}</label>}
      {reason === 'Consumed' && <Input label="ID del gasto existente (opcional)" value={linkedExpenseId} onChange={(event) => setLinkedExpenseId(event.target.value)} hint="Si ya registraste el gasto, vincúlalo aquí para evitar duplicarlo." />}
      <Input label={reason === 'Correction' ? 'Motivo de la corrección' : 'Notas (opcional)'} required={reason === 'Correction'} maxLength={200} value={notes} onChange={(event) => setNotes(event.target.value)} />
      <ModalFooter><Button type="button" variant="ghost" onClick={onClose} disabled={isPending}>Cancelar</Button><Button type="submit" isLoading={isPending} disabled={!isValid}>Confirmar retiro</Button></ModalFooter>
    </form>
  </Modal>;
}