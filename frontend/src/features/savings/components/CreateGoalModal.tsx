import { useState } from 'react';
import { ShieldCheck, Target } from 'lucide-react';
import { useCreateSavingsGoal } from '../hooks/useSavings';
import { useAccounts } from '../../accounts/hooks/useAccounts';
import { Modal, ModalFooter } from '../../../components/ui/Modal';
import { Button } from '../../../components/ui/Button';
import { Input } from '../../../components/ui/Input';
import { todayDateOnly } from '../../../utils/dateOnly';

interface Props { onClose: () => void; }

const selectClass = 'w-full rounded-xl border border-[#EFEAE2] bg-white/70 px-3.5 py-2.5 text-sm text-[#2C2A29] outline-none transition focus:border-[#5C7A99] focus:ring-2 focus:ring-[#5C7A99]/20';

export default function CreateGoalModal({ onClose }: Props) {
  const [name, setName] = useState('');
  const [targetAmount, setTargetAmount] = useState<number | ''>('');
  const [initialAmount, setInitialAmount] = useState<number | ''>('');
  const [initialSourceAccountId, setInitialSourceAccountId] = useState('');
  const [targetDate, setTargetDate] = useState('');
  const [description, setDescription] = useState('');
  const [purpose, setPurpose] = useState<'general' | 'emergency_fund'>('general');
  const [minimumProtectedAmount, setMinimumProtectedAmount] = useState<number | ''>('');
  const { data: accounts } = useAccounts();
  const { mutate: createGoal, isPending } = useCreateSavingsGoal();

  const liquidAccounts = (accounts ?? []).filter((account) => (account.type === 'cash' || account.type === 'savings') && account.isActive);
  const target = Number(targetAmount || 0);
  const initial = Number(initialAmount || 0);
  const minimum = Number(minimumProtectedAmount || 0);
  const selectedSource = liquidAccounts.find((account) => account.id === initialSourceAccountId);
  const isValid = Boolean(name.trim()) && target > 0 && initial >= 0 && initial <= target
    && (initial === 0 || (Boolean(initialSourceAccountId) && (selectedSource?.currentBalance ?? 0) >= initial))
    && (purpose !== 'emergency_fund' || (minimum >= 0 && minimum <= target));

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!isValid) return;
    createGoal({
      name: name.trim(),
      targetAmount: target,
      initialAmount: initial,
      initialSourceAccountId: initial > 0 ? initialSourceAccountId : undefined,
      initialFundingDate: initial > 0 ? todayDateOnly() : undefined,
      idempotencyKey: initial > 0 ? crypto.randomUUID() : undefined,
      targetDate: targetDate || undefined,
      description: description.trim() || undefined,
      purpose,
      minimumProtectedAmount: purpose === 'emergency_fund' ? minimum : undefined,
    }, { onSuccess: onClose });
  };

  return (
    <Modal isOpen onClose={onClose} title="Nueva meta de ahorro" className="max-w-xl">
      <form onSubmit={handleSubmit} className="space-y-5">
        <p className="text-sm text-[#7C756E]">Una meta reserva parte de tu dinero disponible; no mueve ni duplica el saldo de ninguna cuenta.</p>
        <Input label="Nombre de la meta" required maxLength={150} value={name} onChange={(event) => setName(event.target.value)} placeholder="Ej. Vacaciones" />

        <fieldset>
          <legend className="mb-2 text-sm font-medium text-[#2C2A29]">Propósito</legend>
          <div className="grid grid-cols-2 gap-2">
            <PurposeButton active={purpose === 'general'} onClick={() => setPurpose('general')} icon={<Target className="h-5 w-5" />} title="Meta personal" description="Puedes crear todas las que necesites." />
            <PurposeButton active={purpose === 'emergency_fund'} onClick={() => setPurpose('emergency_fund')} icon={<ShieldCheck className="h-5 w-5" />} title="Fondo de emergencia" description="Solo uno activo, con mínimo protegido." />
          </div>
        </fieldset>

        <div className="grid gap-4 sm:grid-cols-2">
          <Input label="Monto objetivo" type="number" step="0.01" min="0.01" required value={targetAmount} onChange={(event) => setTargetAmount(event.target.value === '' ? '' : Number(event.target.value))} />
          <Input label="Monto inicial reservado" type="number" step="0.01" min="0" max={target || undefined} value={initialAmount} onChange={(event) => setInitialAmount(event.target.value === '' ? '' : Number(event.target.value))} hint="No se resta de la cuenta; solo queda asignado a esta meta." />
        </div>

        {initial > 0 && (
          <label className="block text-sm font-medium text-[#2C2A29]">
            <span className="mb-1.5 block">Cuenta que respalda la reserva</span>
            <select required value={initialSourceAccountId} onChange={(event) => setInitialSourceAccountId(event.target.value)} className={selectClass}>
              <option value="">Selecciona una cuenta</option>
              {liquidAccounts.map((account) => <option key={account.id} value={account.id}>{account.name} · {account.currentBalance.toLocaleString('es-EC', { style: 'currency', currency: 'USD' })}</option>)}
            </select>
            <span className="mt-1.5 block text-xs font-normal text-[#7C756E]">La cuenta se usa para comprobar respaldo; su saldo no cambia.</span>
            {selectedSource && selectedSource.currentBalance < initial && <span className="mt-1 block text-xs text-[#B5573F]">La cuenta seleccionada no tiene saldo suficiente.</span>}
          </label>
        )}

        {purpose === 'emergency_fund' && <Input label="Mínimo protegido" type="number" step="0.01" min="0" max={target || undefined} value={minimumProtectedAmount} onChange={(event) => setMinimumProtectedAmount(event.target.value === '' ? '' : Number(event.target.value))} hint="Los retiros ordinarios no podrán dejar el fondo debajo de este valor." />}
        <Input label="Fecha objetivo (opcional)" type="date" value={targetDate} onChange={(event) => setTargetDate(event.target.value)} />
        <label className="block text-sm font-medium text-[#2C2A29]"><span className="mb-1.5 block">Descripción (opcional)</span><textarea value={description} onChange={(event) => setDescription(event.target.value)} maxLength={500} rows={3} className={selectClass + ' resize-none'} /></label>

        <ModalFooter>
          <Button type="button" variant="ghost" onClick={onClose} disabled={isPending}>Cancelar</Button>
          <Button type="submit" isLoading={isPending} disabled={!isValid}>Crear meta</Button>
        </ModalFooter>
      </form>
    </Modal>
  );
}

function PurposeButton({ active, onClick, icon, title, description }: { active: boolean; onClick: () => void; icon: React.ReactNode; title: string; description: string }) {
  return <button type="button" onClick={onClick} className={`rounded-2xl border p-3 text-left transition ${active ? 'border-[#9EAB98] bg-[#F3F7F0] text-[#304B38]' : 'border-[#EFEAE2] bg-white/70 text-[#6D665F] hover:border-[#CFC6BC]'}`}><span className="mb-2 block">{icon}</span><strong className="block text-sm">{title}</strong><span className="mt-0.5 block text-xs font-normal leading-snug">{description}</span></button>;
}