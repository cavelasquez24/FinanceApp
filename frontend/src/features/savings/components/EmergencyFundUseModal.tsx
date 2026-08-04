import { useEffect, useMemo, useState } from 'react';
import { CalendarRange, CheckCircle2, ReceiptText, ShieldAlert, X } from 'lucide-react';
import { useAccounts } from '../../accounts/hooks/useAccounts';
import { useExpenseCategories } from '../../categories/hooks/useCategories';
import { useCreateEmergencyFundUse } from '../hooks/useEmergencyFundRestorations';
import type { SavingsGoal } from '../../../types/savings.types';
import { formatCurrency } from '../../../utils/formatCurrency';
import { getApiErrorMessage } from '../../../utils/getApiError';
import {
  addMonthsClamped,
  calculateRestorationPlan,
  formatPlanDate,
  getLocalToday,
  type RestorationPlanMode,
} from '../utils/restorationPlan';

interface Props {
  goal: SavingsGoal;
  onClose: () => void;
}

const today = getLocalToday();

export default function EmergencyFundUseModal({ goal, onClose }: Props) {
  const [fundedAmount, setFundedAmount] = useState<number | ''>('');
  const [expenseAmount, setExpenseAmount] = useState<number | ''>('');
  const [description, setDescription] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [expenseAccountId, setExpenseAccountId] = useState('');
  const [scheduledSourceAccountId, setScheduledSourceAccountId] = useState('');
  const [acquisitionDate, setAcquisitionDate] = useState(today);
  const [firstScheduledDate, setFirstScheduledDate] = useState(addMonthsClamped(today, 1));
  const [planMode, setPlanMode] = useState<RestorationPlanMode>('deadline');
  const [targetRestorationDate, setTargetRestorationDate] = useState(addMonthsClamped(today, 3));
  const [scheduledContributionAmount, setScheduledContributionAmount] = useState<number | ''>('');
  const [notes, setNotes] = useState('');
  const { data: categories } = useExpenseCategories();
  const { data: accounts } = useAccounts();
  const createMutation = useCreateEmergencyFundUse();

  const cashAccounts = useMemo(
    () => accounts?.filter((account) => account.type === 'cash' && account.isActive) ?? [],
    [accounts],
  );

  useEffect(() => {
    if (cashAccounts.length === 0) return;
    const preferred = cashAccounts.find((account) => account.isDefault) ?? cashAccounts[0];
    setExpenseAccountId((current) => current || preferred.id);
    setScheduledSourceAccountId((current) => current || preferred.id);
  }, [cashAccounts]);

  const plan = useMemo(() => calculateRestorationPlan({
    outstandingAmount: Number(fundedAmount || 0),
    firstScheduledDate,
    mode: planMode,
    targetDate: planMode === 'deadline' ? targetRestorationDate : undefined,
    monthlyAmount: planMode === 'monthly_amount' ? Number(scheduledContributionAmount || 0) : undefined,
  }), [firstScheduledDate, fundedAmount, planMode, scheduledContributionAmount, targetRestorationDate]);

  const funded = Number(fundedAmount || 0);
  const expense = Number(expenseAmount || 0);
  const resultingBalance = goal.currentAmount - funded;
  const protectedMinimum = goal.minimumProtectedAmount ?? 0;
  const belowProtectedMinimum = funded > 0 && resultingBalance < protectedMinimum;
  const budgetPortion = Math.max(0, expense - funded);
  const paymentAccount = cashAccounts.find((account) => account.id === expenseAccountId);
  const insufficientPaymentBalance = Boolean(paymentAccount && paymentAccount.currentBalance < budgetPortion);
  const datesAreValid = acquisitionDate <= today
    && firstScheduledDate >= acquisitionDate
    && Boolean(plan)
    && (!plan || plan.targetDate >= firstScheduledDate);
  const formIsValid = funded > 0
    && funded <= goal.currentAmount
    && expense >= funded
    && Boolean(description.trim())
    && Boolean(categoryId)
    && Boolean(expenseAccountId)
    && Boolean(scheduledSourceAccountId)
    && datesAreValid
    && !insufficientPaymentBalance;

  const planError = funded > 0 && firstScheduledDate && !plan
    ? planMode === 'deadline'
      ? 'La fecha máxima debe incluir al menos una fecha de aporte.'
      : 'Ingresa una cuota mayor a cero y que no supere el monto por restaurar.'
    : null;

  const handleAcquisitionDate = (value: string) => {
    setAcquisitionDate(value);
    if (firstScheduledDate < value) {
      const nextMonth = addMonthsClamped(value, 1);
      setFirstScheduledDate(nextMonth);
      if (targetRestorationDate < nextMonth) setTargetRestorationDate(addMonthsClamped(nextMonth, 2));
    }
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!formIsValid || !plan) return;

    createMutation.mutate({
      goalId: goal.id,
      data: {
        fundedAmount: funded,
        expenseAmount: expense,
        categoryId,
        expenseAccountId,
        scheduledSourceAccountId,
        description: description.trim(),
        acquisitionDate,
        paymentMethod: 'cash',
        targetRestorationDate: plan.targetDate,
        scheduledContributionAmount: plan.monthlyAmount,
        firstScheduledDate,
        notes: notes.trim() || undefined,
      },
    }, { onSuccess: onClose });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-[#2C2A29]/55 p-3 backdrop-blur-sm sm:p-5">
      <div className="max-h-[94vh] w-full max-w-4xl overflow-y-auto rounded-[30px] border border-[#E8E1D8] bg-[#FBF9F4] shadow-2xl">
        <header className="sticky top-0 z-10 flex items-start justify-between gap-4 border-b border-[#E8E1D8] bg-[#FBF9F4]/95 px-6 py-5 backdrop-blur sm:px-8">
          <div>
            <p className="mb-1 text-xs font-semibold uppercase tracking-[0.18em] text-[#7A4B3A]">Protección financiera</p>
            <h2 className="font-serif text-2xl font-semibold text-[#2C2A29] sm:text-3xl">Usar el fondo de emergencia</h2>
            <p className="mt-1 max-w-2xl text-sm text-[#7C756E]">Se registra un único gasto. Los aportes posteriores son transferencias que restauran el fondo, no gastos nuevos.</p>
          </div>
          <button type="button" onClick={onClose} className="rounded-full p-2 text-[#7C756E] transition hover:bg-[#EFEAE2]" aria-label="Cerrar">
            <X className="h-5 w-5" />
          </button>
        </header>

        <form onSubmit={handleSubmit} className="space-y-6 p-6 sm:p-8">
          <Section icon={<ReceiptText className="h-5 w-5" />} number="1" title="Registra el gasto" description="Identifica qué compraste y cómo se pagó.">
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Bien, servicio o emergencia" className="sm:col-span-2">
                <input required maxLength={500} value={description} onChange={(event) => setDescription(event.target.value)} placeholder="Ej. Monitor para trabajar" className="input-restoration" />
              </Field>
              <Field label="Costo total del gasto" hint="Incluye la parte que cubrirás con tu presupuesto.">
                <MoneyInput value={expenseAmount} onChange={setExpenseAmount} />
              </Field>
              <Field label="Monto tomado del fondo" hint={`Disponible: ${formatCurrency(goal.currentAmount)}`}>
                <MoneyInput value={fundedAmount} onChange={setFundedAmount} max={goal.currentAmount} />
              </Field>
              <Field label="Categoría del gasto">
                <select required value={categoryId} onChange={(event) => setCategoryId(event.target.value)} className="input-restoration">
                  <option value="">Selecciona una categoría</option>
                  {categories?.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
                </select>
              </Field>
              <Field label="Fecha de adquisición">
                <input type="date" required max={today} value={acquisitionDate} onChange={(event) => handleAcquisitionDate(event.target.value)} className="input-restoration" />
              </Field>
              <Field label="Cuenta utilizada para pagar" hint="Recibirá el dinero retirado del fondo antes de registrar el gasto." className="sm:col-span-2">
                <AccountSelect value={expenseAccountId} onChange={setExpenseAccountId} accounts={cashAccounts} />
              </Field>
            </div>
          </Section>

          <Section icon={<ShieldAlert className="h-5 w-5" />} number="2" title="Revisa el impacto en tu protección" description="El mínimo protegido es una referencia de seguridad; puedes continuar aunque el retiro lo cruce.">
            <div className="grid gap-3 sm:grid-cols-3">
              <Metric label="Fondo actual" value={formatCurrency(goal.currentAmount)} />
              <Metric label="Después del uso" value={formatCurrency(resultingBalance)} tone={belowProtectedMinimum ? 'warning' : 'default'} />
              <Metric label="Mínimo protegido" value={formatCurrency(protectedMinimum)} />
            </div>
            <div className="mt-3 grid gap-3 sm:grid-cols-2">
              <Metric label="Cubierto por el fondo" value={formatCurrency(funded)} />
              <Metric label="Sale de tu presupuesto/cuenta" value={formatCurrency(budgetPortion)} tone={insufficientPaymentBalance ? 'warning' : 'default'} />
            </div>
            {belowProtectedMinimum && (
              <Warning>El fondo quedará {formatCurrency(protectedMinimum - resultingBalance)} por debajo del mínimo protegido.</Warning>
            )}
            {insufficientPaymentBalance && paymentAccount && (
              <Warning>La cuenta “{paymentAccount.name}” tiene {formatCurrency(paymentAccount.currentBalance)} y necesita {formatCurrency(budgetPortion)} para cubrir la parte no financiada.</Warning>
            )}
          </Section>

          <Section icon={<CalendarRange className="h-5 w-5" />} number="3" title="Programa la restauración" description="Elige la variable que conoces; FinFlow calcula la otra automáticamente.">
            <div className="mb-5 grid grid-cols-2 rounded-2xl bg-[#EFEAE2] p-1">
              <PlanModeButton active={planMode === 'deadline'} onClick={() => setPlanMode('deadline')} title="Tengo fecha máxima" subtitle="Calcula mi aporte" />
              <PlanModeButton active={planMode === 'monthly_amount'} onClick={() => setPlanMode('monthly_amount')} title="Tengo un monto mensual" subtitle="Calcula mi fecha" />
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Primera fecha programada" hint="Desde esta fecha se intentará aplicar un aporte cada mes.">
                <input type="date" required min={acquisitionDate} value={firstScheduledDate} onChange={(event) => setFirstScheduledDate(event.target.value)} className="input-restoration" />
              </Field>
              {planMode === 'deadline' ? (
                <Field label="Fecha máxima para restaurar">
                  <input type="date" required min={firstScheduledDate} value={targetRestorationDate} onChange={(event) => setTargetRestorationDate(event.target.value)} className="input-restoration" />
                </Field>
              ) : (
                <Field label="Aporte mensual que puedes sostener">
                  <MoneyInput value={scheduledContributionAmount} onChange={setScheduledContributionAmount} max={funded || undefined} />
                </Field>
              )}
              <Field label="Cuenta para aportes programados" hint="Puede ser distinta de la cuenta usada para pagar." className="sm:col-span-2">
                <AccountSelect value={scheduledSourceAccountId} onChange={setScheduledSourceAccountId} accounts={cashAccounts} />
              </Field>
            </div>

            {planError && <p className="mt-3 text-sm text-red-600">{planError}</p>}
            {plan && (
              <div className="mt-5 rounded-2xl border border-[#DDE7D8] bg-[#F3F7F0] p-4">
                <div className="flex items-start gap-3">
                  <CheckCircle2 className="mt-0.5 h-5 w-5 shrink-0 text-[#5F8667]" />
                  <div className="min-w-0 flex-1">
                    <p className="font-medium text-[#304B38]">
                      {formatCurrency(plan.monthlyAmount)} al mes · restauración estimada el {formatPlanDate(plan.estimatedCompletionDate)}
                    </p>
                    <p className="mt-1 text-sm text-[#5E7162]">
                      {plan.paymentsCount} aporte{plan.paymentsCount === 1 ? '' : 's'}; el último sería de {formatCurrency(plan.finalPayment)}.
                    </p>
                    <div className="mt-3 flex flex-wrap gap-2">
                      {plan.paymentDates.map((date, index) => (
                        <span key={date} className="rounded-full bg-white px-3 py-1 text-xs text-[#52664D]">{index + 1}. {formatPlanDate(date)}</span>
                      ))}
                      {plan.paymentsCount > plan.paymentDates.length && <span className="px-2 py-1 text-xs text-[#5E7162]">+ {plan.paymentsCount - plan.paymentDates.length} más</span>}
                    </div>
                  </div>
                </div>
              </div>
            )}

            <p className="mt-4 text-xs leading-relaxed text-[#7C756E]">
              Los aportes vencidos se aplican al abrir FinFlow. Si la cuenta no tiene saldo, no se sobregira: el aporte queda pendiente. Un aporte extra mantiene la cuota mensual y adelanta la fecha estimada de finalización.
            </p>
          </Section>

          <Field label="Notas opcionales">
            <textarea value={notes} onChange={(event) => setNotes(event.target.value)} rows={2} maxLength={1000} className="input-restoration resize-none" placeholder="Contexto o detalle que quieras conservar" />
          </Field>

          {cashAccounts.length === 0 && <Warning>No hay una cuenta de efectivo activa. Crea o activa una cuenta antes de usar el fondo.</Warning>}
          {createMutation.error && <p className="rounded-xl bg-red-50 p-3 text-sm text-red-700">{getApiErrorMessage(createMutation.error, 'No se pudo registrar el uso del fondo.')}</p>}

          <div className="flex flex-col-reverse justify-end gap-3 border-t border-[#E8E1D8] pt-5 sm:flex-row">
            <button type="button" onClick={onClose} disabled={createMutation.isPending} className="rounded-xl bg-[#EFEAE2] px-5 py-2.5 font-medium text-[#5F5953] transition hover:bg-[#E5DED5]">Cancelar</button>
            <button type="submit" disabled={createMutation.isPending || !formIsValid} className="rounded-xl bg-[#7A4B3A] px-5 py-2.5 font-medium text-white transition hover:bg-[#633C2F] disabled:cursor-not-allowed disabled:opacity-45">
              {createMutation.isPending ? 'Registrando...' : 'Registrar uso y programar restauración'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function Section({ icon, number, title, description, children }: { icon: React.ReactNode; number: string; title: string; description: string; children: React.ReactNode }) {
  return (
    <section className="rounded-[24px] border border-[#E8E1D8] bg-white/65 p-5 sm:p-6">
      <div className="mb-5 flex items-start gap-3">
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-[#F4E8E2] text-[#7A4B3A]">{icon}</span>
        <div><h3 className="font-semibold text-[#2C2A29]">{number}. {title}</h3><p className="mt-0.5 text-sm text-[#7C756E]">{description}</p></div>
      </div>
      {children}
    </section>
  );
}

function Field({ label, hint, className = '', children }: { label: string; hint?: string; className?: string; children: React.ReactNode }) {
  return <label className={`block text-sm font-medium text-[#5F5953] ${className}`}><span className="mb-1.5 block">{label}</span>{children}{hint && <span className="mt-1.5 block text-xs font-normal text-[#8B837B]">{hint}</span>}</label>;
}

function MoneyInput({ value, onChange, max }: { value: number | ''; onChange: (value: number | '') => void; max?: number }) {
  return <input type="number" min="0.01" step="0.01" max={max} required value={value} onChange={(event) => onChange(event.target.value === '' ? '' : Number(event.target.value))} className="input-restoration" placeholder="0,00" />;
}

function AccountSelect({ value, onChange, accounts }: { value: string; onChange: (value: string) => void; accounts: Array<{ id: string; name: string; currentBalance: number; isDefault: boolean }> }) {
  return (
    <select required value={value} onChange={(event) => onChange(event.target.value)} className="input-restoration">
      <option value="">Selecciona una cuenta</option>
      {accounts.map((account) => <option key={account.id} value={account.id}>{account.name}{account.isDefault ? ' · Predeterminada' : ''} — {formatCurrency(account.currentBalance)}</option>)}
    </select>
  );
}

function Metric({ label, value, tone = 'default' }: { label: string; value: string; tone?: 'default' | 'warning' }) {
  return <div className={`rounded-2xl border p-3 ${tone === 'warning' ? 'border-amber-200 bg-amber-50' : 'border-[#E8E1D8] bg-[#FBF9F4]'}`}><span className="block text-xs text-[#7C756E]">{label}</span><strong className={tone === 'warning' ? 'text-amber-900' : 'text-[#2C2A29]'}>{value}</strong></div>;
}

function Warning({ children }: { children: React.ReactNode }) {
  return <p className="mt-3 rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">{children}</p>;
}

function PlanModeButton({ active, onClick, title, subtitle }: { active: boolean; onClick: () => void; title: string; subtitle: string }) {
  return <button type="button" onClick={onClick} className={`rounded-xl px-3 py-2.5 text-left transition ${active ? 'bg-white text-[#2C2A29] shadow-sm' : 'text-[#7C756E] hover:text-[#2C2A29]'}`}><span className="block text-sm font-semibold">{title}</span><span className="block text-xs font-normal">{subtitle}</span></button>;
}
