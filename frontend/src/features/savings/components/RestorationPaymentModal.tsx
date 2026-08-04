import { useEffect, useMemo, useState } from 'react';
import { ArrowUpRight, CalendarCheck2, X } from 'lucide-react';
import { useAccounts } from '../../accounts/hooks/useAccounts';
import { useRegisterRestorationPayment } from '../hooks/useEmergencyFundRestorations';
import type { EmergencyFundRestoration } from '../../../types/savings.types';
import { formatCurrency } from '../../../utils/formatCurrency';
import { getApiErrorMessage } from '../../../utils/getApiError';
import {
  addMonthsClamped,
  calculateRestorationPlan,
  formatPlanDate,
  getLocalToday,
} from '../utils/restorationPlan';

interface Props {
  restoration: EmergencyFundRestoration;
  onClose: () => void;
}

export default function RestorationPaymentModal({ restoration, onClose }: Props) {
  const [amount, setAmount] = useState<number | ''>(restoration.nextContributionAmount);
  const [paymentDate, setPaymentDate] = useState(getLocalToday());
  const [sourceAccountId, setSourceAccountId] = useState(restoration.scheduledSourceAccountId ?? '');
  const [notes, setNotes] = useState('');
  const { data: accounts } = useAccounts();
  const paymentMutation = useRegisterRestorationPayment();
  const cashAccounts = useMemo(
    () => accounts?.filter((account) => account.type === 'cash' && account.isActive) ?? [],
    [accounts],
  );

  useEffect(() => {
    if (cashAccounts.length === 0) return;
    const scheduled = cashAccounts.find((account) => account.id === restoration.scheduledSourceAccountId);
    const preferred = scheduled ?? cashAccounts.find((account) => account.isDefault) ?? cashAccounts[0];
    setSourceAccountId((current) => current && cashAccounts.some((account) => account.id === current) ? current : preferred.id);
  }, [cashAccounts, restoration.scheduledSourceAccountId]);

  const numericAmount = Number(amount || 0);
  const remaining = Math.max(0, restoration.outstandingAmount - numericAmount);
  const selectedAccount = cashAccounts.find((account) => account.id === sourceAccountId);
  const insufficientBalance = Boolean(selectedAccount && selectedAccount.currentBalance < numericAmount);
  const nextDateAfterPayment = paymentDate >= restoration.nextScheduledDate
    ? addMonthsClamped(restoration.nextScheduledDate, 1)
    : restoration.nextScheduledDate;
  const projection = remaining > 0 ? calculateRestorationPlan({
    outstandingAmount: remaining,
    firstScheduledDate: nextDateAfterPayment,
    mode: 'monthly_amount',
    monthlyAmount: Math.min(restoration.scheduledContributionAmount, remaining),
  }) : null;
  const isValid = numericAmount > 0
    && numericAmount <= restoration.outstandingAmount
    && Boolean(sourceAccountId)
    && paymentDate <= getLocalToday()
    && !insufficientBalance;

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!isValid) return;
    paymentMutation.mutate({
      restorationId: restoration.id,
      data: {
        amount: numericAmount,
        paymentDate,
        sourceAccountId,
        notes: notes.trim() || undefined,
      },
    }, { onSuccess: onClose });
  };

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center bg-[#2C2A29]/55 p-4 backdrop-blur-sm">
      <div className="max-h-[92vh] w-full max-w-lg overflow-y-auto rounded-[28px] border border-[#E8E1D8] bg-[#FBF9F4] p-6 shadow-2xl sm:p-7">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.16em] text-[#5F8667]">Aporte extraordinario</p>
            <h2 className="mt-1 font-serif text-2xl font-semibold text-[#2C2A29]">{restoration.description}</h2>
            <p className="mt-1 text-sm text-[#7C756E]">Pendiente actual: {formatCurrency(restoration.outstandingAmount)}</p>
          </div>
          <button type="button" onClick={onClose} className="rounded-full p-2 text-[#7C756E] transition hover:bg-[#EFEAE2]" aria-label="Cerrar"><X className="h-5 w-5" /></button>
        </div>

        <form onSubmit={submit} className="mt-6 space-y-4">
          <label className="block text-sm font-medium text-[#5F5953]">Monto del aporte
            <input type="number" min="0.01" max={restoration.outstandingAmount} step="0.01" required value={amount} onChange={(event) => setAmount(event.target.value === '' ? '' : Number(event.target.value))} className="input-restoration mt-1.5" />
          </label>
          <label className="block text-sm font-medium text-[#5F5953]">Fecha del aporte
            <input type="date" required max={getLocalToday()} value={paymentDate} onChange={(event) => setPaymentDate(event.target.value)} className="input-restoration mt-1.5" />
          </label>
          <label className="block text-sm font-medium text-[#5F5953]">Cuenta de origen
            <select required value={sourceAccountId} onChange={(event) => setSourceAccountId(event.target.value)} className="input-restoration mt-1.5">
              <option value="">Selecciona una cuenta</option>
              {cashAccounts.map((account) => <option key={account.id} value={account.id}>{account.name}{account.isDefault ? ' · Predeterminada' : ''} — {formatCurrency(account.currentBalance)}</option>)}
            </select>
          </label>
          <label className="block text-sm font-medium text-[#5F5953]">Nota opcional
            <textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} className="input-restoration mt-1.5 resize-none" />
          </label>

          {insufficientBalance && selectedAccount && (
            <p className="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">La cuenta tiene {formatCurrency(selectedAccount.currentBalance)} y el aporte requiere {formatCurrency(numericAmount)}.</p>
          )}

          {numericAmount > 0 && numericAmount <= restoration.outstandingAmount && (
            <div className="rounded-2xl border border-[#DDE7D8] bg-[#F3F7F0] p-4">
              <div className="flex gap-3">
                {remaining === 0 ? <CalendarCheck2 className="h-5 w-5 shrink-0 text-[#5F8667]" /> : <ArrowUpRight className="h-5 w-5 shrink-0 text-[#5F8667]" />}
                <div>
                  <p className="font-medium text-[#304B38]">{remaining === 0 ? 'La restauración quedará completada' : `El pendiente bajará a ${formatCurrency(remaining)}`}</p>
                  <p className="mt-1 text-sm text-[#5E7162]">
                    {remaining === 0
                      ? `Fecha de finalización: ${formatPlanDate(paymentDate)}`
                      : projection
                        ? `Manteniendo ${formatCurrency(restoration.scheduledContributionAmount)} al mes, terminarías aproximadamente el ${formatPlanDate(projection.estimatedCompletionDate)}.`
                        : 'La programación mensual se mantiene sin cambios.'}
                  </p>
                </div>
              </div>
            </div>
          )}

          {paymentMutation.error && <p className="rounded-xl bg-red-50 p-3 text-sm text-red-700">{getApiErrorMessage(paymentMutation.error, 'No se pudo registrar el aporte.')}</p>}

          <div className="flex flex-col-reverse justify-end gap-3 border-t border-[#E8E1D8] pt-5 sm:flex-row">
            <button type="button" onClick={onClose} className="rounded-xl bg-[#EFEAE2] px-5 py-2.5 font-medium text-[#5F5953]">Cancelar</button>
            <button type="submit" disabled={paymentMutation.isPending || !isValid} className="rounded-xl bg-[#2C2A29] px-5 py-2.5 font-medium text-white transition hover:bg-[#1A1918] disabled:cursor-not-allowed disabled:opacity-45">
              {paymentMutation.isPending ? 'Registrando...' : 'Aplicar aporte extra'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
