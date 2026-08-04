import { useState } from 'react';
import { ShieldCheck, Target, X } from 'lucide-react';
import { useCreateSavingsGoal } from '../hooks/useSavings';

interface Props {
  onClose: () => void;
}

export default function CreateGoalModal({ onClose }: Props) {
  const [name, setName] = useState('');
  const [targetAmount, setTargetAmount] = useState<number | ''>('');
  const [initialAmount, setInitialAmount] = useState<number | ''>('');
  const [targetDate, setTargetDate] = useState('');
  const [description, setDescription] = useState('');
  const [purpose, setPurpose] = useState<'general' | 'emergency_fund'>('general');
  const [minimumProtectedAmount, setMinimumProtectedAmount] = useState<number | ''>('');
  const { mutate: createGoal, isPending } = useCreateSavingsGoal();

  const target = Number(targetAmount || 0);
  const initial = Number(initialAmount || 0);
  const minimum = Number(minimumProtectedAmount || 0);
  const isValid = Boolean(name.trim())
    && target > 0
    && initial >= 0
    && initial <= target
    && (purpose !== 'emergency_fund' || (minimum >= 0 && minimum <= target));

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!isValid) return;
    createGoal({
      name: name.trim(),
      targetAmount: target,
      initialAmount: initial,
      targetDate: targetDate || undefined,
      description: description.trim() || undefined,
      purpose,
      minimumProtectedAmount: purpose === 'emergency_fund' ? minimum : undefined,
    }, { onSuccess: onClose });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-[#2C2A29]/55 p-4 backdrop-blur-sm">
      <div className="max-h-[92vh] w-full max-w-lg overflow-y-auto rounded-[28px] border border-[#E8E1D8] bg-[#FBF9F4] p-6 shadow-2xl sm:p-8">
        <div className="flex items-start justify-between gap-4">
          <div><p className="text-xs font-semibold uppercase tracking-[0.16em] text-[#7FA083]">Planificación</p><h2 className="mt-1 font-serif text-2xl font-semibold text-[#2C2A29]">Nueva meta de ahorro</h2><p className="mt-1 text-sm text-[#7C756E]">Define el propósito para que FinFlow aplique la lógica correcta.</p></div>
          <button type="button" onClick={onClose} className="rounded-full p-2 text-[#7C756E] transition hover:bg-[#EFEAE2]" aria-label="Cerrar"><X className="h-5 w-5" /></button>
        </div>

        <form onSubmit={handleSubmit} className="mt-6 space-y-5">
          <Field label="Nombre de la meta">
            <input type="text" required maxLength={150} value={name} onChange={(event) => setName(event.target.value)} placeholder="Ej. Fondo de emergencia" className="input-restoration" />
          </Field>

          <fieldset>
            <legend className="mb-2 text-sm font-medium text-[#5F5953]">Propósito</legend>
            <div className="grid grid-cols-2 gap-2">
              <PurposeButton active={purpose === 'general'} onClick={() => setPurpose('general')} icon={<Target className="h-5 w-5" />} title="Meta personal" description="Se completa y luego puede utilizarse." />
              <PurposeButton active={purpose === 'emergency_fund'} onClick={() => setPurpose('emergency_fund')} icon={<ShieldCheck className="h-5 w-5" />} title="Fondo de emergencia" description="Permite usos con restauración." />
            </div>
          </fieldset>

          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Monto objetivo">
              <MoneyInput value={targetAmount} onChange={setTargetAmount} min="0.01" />
            </Field>
            <Field label="Monto inicial" hint="Ahorro que ya tenías; no cuenta como aporte del mes.">
              <MoneyInput value={initialAmount} onChange={setInitialAmount} min="0" max={target || undefined} />
            </Field>
          </div>
          {initial > target && target > 0 && <ErrorText>El monto inicial no puede superar el objetivo.</ErrorText>}

          {purpose === 'emergency_fund' && (
            <div className="rounded-2xl border border-[#DDE7D8] bg-[#F3F7F0] p-4">
              <Field label="Mínimo protegido" hint="FinFlow advertirá si un uso deja el fondo por debajo de este nivel.">
                <MoneyInput value={minimumProtectedAmount} onChange={setMinimumProtectedAmount} min="0" max={target || undefined} />
              </Field>
              {minimum > target && target > 0 && <ErrorText>El mínimo protegido no puede superar el objetivo.</ErrorText>}
            </div>
          )}

          <Field label="Fecha objetivo opcional">
            <input type="date" value={targetDate} onChange={(event) => setTargetDate(event.target.value)} className="input-restoration" />
          </Field>
          <Field label="Descripción opcional">
            <textarea value={description} onChange={(event) => setDescription(event.target.value)} maxLength={500} rows={2} className="input-restoration resize-none" />
          </Field>

          <div className="flex flex-col-reverse justify-end gap-3 border-t border-[#E8E1D8] pt-5 sm:flex-row">
            <button type="button" onClick={onClose} disabled={isPending} className="rounded-xl bg-[#EFEAE2] px-5 py-2.5 font-medium text-[#5F5953] transition hover:bg-[#E5DED5]">Cancelar</button>
            <button type="submit" disabled={isPending || !isValid} className="rounded-xl bg-[#2C2A29] px-5 py-2.5 font-medium text-white transition hover:bg-[#1A1918] disabled:cursor-not-allowed disabled:opacity-45">{isPending ? 'Creando...' : 'Crear meta'}</button>
          </div>
        </form>
      </div>
    </div>
  );
}

function Field({ label, hint, children }: { label: string; hint?: string; children: React.ReactNode }) {
  return <label className="block text-sm font-medium text-[#5F5953]"><span className="mb-1.5 block">{label}</span>{children}{hint && <span className="mt-1.5 block text-xs font-normal text-[#8B837B]">{hint}</span>}</label>;
}

function MoneyInput({ value, onChange, min, max }: { value: number | ''; onChange: (value: number | '') => void; min: string; max?: number }) {
  return <input type="number" step="0.01" min={min} max={max} value={value} onChange={(event) => onChange(event.target.value === '' ? '' : Number(event.target.value))} placeholder="0,00" className="input-restoration" />;
}

function PurposeButton({ active, onClick, icon, title, description }: { active: boolean; onClick: () => void; icon: React.ReactNode; title: string; description: string }) {
  return <button type="button" onClick={onClick} className={`rounded-2xl border p-3 text-left transition ${active ? 'border-[#9EAB98] bg-[#F3F7F0] text-[#304B38]' : 'border-[#E8E1D8] bg-white text-[#6D665F] hover:border-[#CFC6BC]'}`}><span className="mb-2 block">{icon}</span><strong className="block text-sm">{title}</strong><span className="mt-0.5 block text-xs font-normal leading-snug">{description}</span></button>;
}

function ErrorText({ children }: { children: React.ReactNode }) {
  return <p className="text-xs text-red-600">{children}</p>;
}
