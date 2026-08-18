import { useState } from 'react';
import { AlertTriangle } from 'lucide-react';
import { useDeleteSavingsGoal, useSavingsGoals } from '../hooks/useSavings';
import { useAccounts } from '../../accounts/hooks/useAccounts';
import { type SavingsGoal } from '../../../types/savings.types';
import { Modal, ModalFooter } from '../../../components/ui/Modal';
import { Button } from '../../../components/ui/Button';
import { todayDateOnly } from '../../../utils/dateOnly';

interface Props { goal: SavingsGoal; onClose: () => void; }
const selectClass = 'w-full rounded-xl border border-[#EFEAE2] bg-white/70 px-3.5 py-2.5 text-sm text-[#2C2A29] outline-none transition focus:border-[#5C7A99] focus:ring-2 focus:ring-[#5C7A99]/20';

export default function DeleteConfirmModal({ goal, onClose }: Props) {
  const [resolution, setResolution] = useState<'release' | 'reassign'>('release');
  const [destinationAccountId, setDestinationAccountId] = useState('');
  const [targetGoalId, setTargetGoalId] = useState('');
  const { data: accounts } = useAccounts();
  const { data: goals } = useSavingsGoals();
  const { mutate: deleteGoal, isPending } = useDeleteSavingsGoal();
  const hasBalance = goal.currentAmount > 0;
  const liquidAccounts = (accounts ?? []).filter((account) => (account.type === 'cash' || account.type === 'savings') && account.isActive);
  const targetGoals = (goals ?? []).filter((item) => item.id !== goal.id && item.targetAmount - item.currentAmount >= goal.currentAmount);
  const isValid = !hasBalance || (resolution === 'release' ? Boolean(destinationAccountId) : Boolean(targetGoalId));

  const handleConfirm = () => {
    deleteGoal({ id: goal.id, data: hasBalance ? {
      resolution,
      destinationAccountId: resolution === 'release' ? destinationAccountId : undefined,
      targetGoalId: resolution === 'reassign' ? targetGoalId : undefined,
      date: todayDateOnly(),
      idempotencyKey: crypto.randomUUID(),
    } : undefined }, { onSuccess: onClose });
  };

  return <Modal isOpen onClose={onClose} title="Archivar meta" className="max-w-md">
    <div className="space-y-5">
      <div className="flex items-start gap-3 rounded-2xl bg-[#FBEEEA] p-4"><AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-[#C97B63]" /><p className="text-sm text-[#6D4B40]">{hasBalance ? <>La meta <strong>{goal.name}</strong> tiene {goal.currentAmount.toLocaleString('es-EC', { style: 'currency', currency: 'USD' })} asignados. Debes indicar qué hacer con esa reserva.</> : <>La meta <strong>{goal.name}</strong> se archivará conservando todo su historial.</>}</p></div>
      {goal.openRestorationsCount > 0 && <p className="rounded-xl bg-amber-50 p-3 text-sm text-amber-800">No se puede archivar mientras existan restauraciones abiertas.</p>}
      {hasBalance && goal.openRestorationsCount === 0 && <>
        <div className="grid grid-cols-2 gap-2">
          <button type="button" onClick={() => setResolution('release')} className={`rounded-xl border p-3 text-sm font-medium ${resolution === 'release' ? 'border-[#9EAB98] bg-[#F3F7F0] text-[#304B38]' : 'border-[#EFEAE2] bg-white/70 text-[#7C756E]'}`}>Liberar saldo</button>
          <button type="button" onClick={() => setResolution('reassign')} className={`rounded-xl border p-3 text-sm font-medium ${resolution === 'reassign' ? 'border-[#9EAB98] bg-[#F3F7F0] text-[#304B38]' : 'border-[#EFEAE2] bg-white/70 text-[#7C756E]'}`}>Reasignar</button>
        </div>
        {resolution === 'release' ? <label className="block text-sm font-medium text-[#2C2A29]"><span className="mb-1.5 block">Cuenta donde quedará disponible</span><select value={destinationAccountId} onChange={(event) => setDestinationAccountId(event.target.value)} className={selectClass}><option value="">Selecciona una cuenta</option>{liquidAccounts.map((account) => <option key={account.id} value={account.id}>{account.name}</option>)}</select></label> : <label className="block text-sm font-medium text-[#2C2A29]"><span className="mb-1.5 block">Meta destino</span><select value={targetGoalId} onChange={(event) => setTargetGoalId(event.target.value)} className={selectClass}><option value="">Selecciona otra meta</option>{targetGoals.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>}
      </>}
      <ModalFooter><Button type="button" variant="ghost" onClick={onClose} disabled={isPending}>Cancelar</Button><Button type="button" variant="danger" onClick={handleConfirm} isLoading={isPending} disabled={!isValid || goal.openRestorationsCount > 0}>Archivar</Button></ModalFooter>
    </div>
  </Modal>;
}