import { useState } from 'react';
import { useUpdateSavingsGoal } from '../hooks/useSavings';
import { type SavingsGoal } from '../../../types/savings.types';
import { Modal, ModalFooter } from '../../../components/ui/Modal';
import { Button } from '../../../components/ui/Button';
import { Input } from '../../../components/ui/Input';

interface Props { goal: SavingsGoal; onClose: () => void; }
const selectClass = 'w-full rounded-xl border border-[#EFEAE2] bg-white/70 px-3.5 py-2.5 text-sm text-[#2C2A29] outline-none transition focus:border-[#5C7A99] focus:ring-2 focus:ring-[#5C7A99]/20';

export default function EditGoalModal({ goal, onClose }: Props) {
  const [name, setName] = useState(goal.name);
  const [targetAmount, setTargetAmount] = useState<number | ''>(goal.targetAmount);
  const [targetDate, setTargetDate] = useState(goal.targetDate || '');
  const [description, setDescription] = useState(goal.description || '');
  const [purpose, setPurpose] = useState<'general' | 'emergency_fund'>(goal.purpose);
  const [minimumProtectedAmount, setMinimumProtectedAmount] = useState<number | ''>(goal.minimumProtectedAmount ?? '');
  const { mutate: updateGoal, isPending } = useUpdateSavingsGoal();
  const target = Number(targetAmount || 0);
  const minimum = Number(minimumProtectedAmount || 0);
  const isValid = Boolean(name.trim()) && target >= goal.currentAmount && target > 0 && (purpose !== 'emergency_fund' || (minimum >= 0 && minimum <= target));

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!isValid) return;
    updateGoal({ id: goal.id, data: { name: name.trim(), targetAmount: target, targetDate: targetDate || undefined, description: description.trim() || undefined, purpose, minimumProtectedAmount: purpose === 'emergency_fund' ? minimum : undefined } }, { onSuccess: onClose });
  };

  return <Modal isOpen onClose={onClose} title="Editar meta">
    <form onSubmit={handleSubmit} className="space-y-5">
      <Input label="Nombre de la meta" required value={name} onChange={(event) => setName(event.target.value)} />
      <label className="block text-sm font-medium text-[#2C2A29]"><span className="mb-1.5 block">Propósito</span><select value={purpose} onChange={(event) => setPurpose(event.target.value as 'general' | 'emergency_fund')} disabled={goal.openRestorationsCount > 0} className={selectClass}><option value="general">Meta personal</option><option value="emergency_fund">Fondo de emergencia</option></select>{goal.openRestorationsCount > 0 && <span className="mt-1.5 block text-xs text-amber-700">Resuelve las restauraciones antes de cambiar el propósito.</span>}</label>
      <Input label="Monto objetivo" type="number" step="0.01" min={goal.currentAmount} required value={targetAmount} onChange={(event) => setTargetAmount(event.target.value === '' ? '' : Number(event.target.value))} error={target < goal.currentAmount ? 'El objetivo no puede ser menor que el saldo asignado.' : undefined} />
      {purpose === 'emergency_fund' && <Input label="Mínimo protegido" type="number" step="0.01" min="0" max={target || undefined} value={minimumProtectedAmount} onChange={(event) => setMinimumProtectedAmount(event.target.value === '' ? '' : Number(event.target.value))} />}
      <Input label="Fecha objetivo (opcional)" type="date" value={targetDate} onChange={(event) => setTargetDate(event.target.value)} />
      <label className="block text-sm font-medium text-[#2C2A29]"><span className="mb-1.5 block">Descripción (opcional)</span><textarea value={description} onChange={(event) => setDescription(event.target.value)} rows={3} className={selectClass + ' resize-none'} /></label>
      <ModalFooter><Button type="button" variant="ghost" onClick={onClose} disabled={isPending}>Cancelar</Button><Button type="submit" isLoading={isPending} disabled={!isValid}>Guardar cambios</Button></ModalFooter>
    </form>
  </Modal>;
}