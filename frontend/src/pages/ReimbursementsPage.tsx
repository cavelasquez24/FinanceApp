import { monthStartDateOnly, todayDateOnly } from '../utils/dateOnly';
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { format } from 'date-fns';
import { parseDateOnly } from '../utils/dateOnly';
import { AlertCircle, ArrowLeftRight, Inbox, Plus, Trash2 } from 'lucide-react';
import { useSearchParams } from 'react-router-dom';
import { Button, Card, CardHeader, ConfirmDialog, Spinner } from '../components/ui';
import { useAccounts } from '../features/accounts/hooks/useAccounts';
import { useCreditCards } from '../features/credit-cards/hooks/useCreditCards';
import { useExpenses } from '../features/expenses/hooks/useExpenses';
import {
  useCreateReimbursement,
  useDeleteReimbursement,
  useReimbursements,
  useReimbursementSummary,
} from '../features/reimbursements/hooks/useReimbursements';
import type { ReimbursementDestinationType } from '../types/reimbursement.types';
import { formatCurrency } from '../utils/formatCurrency';

const dateValue = todayDateOnly();
const monthStart = monthStartDateOnly(new Date().getFullYear(), new Date().getMonth() + 1);

export default function ReimbursementsPage() {
  const [params] = useSearchParams();
  const [isFormOpen, setIsFormOpen] = useState(Boolean(params.get('expenseId')));
  const [expenseId, setExpenseId] = useState(params.get('expenseId') ?? '');
  const [destinationType, setDestinationType] = useState<ReimbursementDestinationType>('account');
  const [destinationId, setDestinationId] = useState('');
  const [amount, setAmount] = useState('');
  const [date, setDate] = useState(dateValue);
  const [person, setPerson] = useState('');
  const [notes, setNotes] = useState('');
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const { data: reimbursementsData, isLoading, isError } = useReimbursements();
  const reimbursements = reimbursementsData ?? [];
  const { data: summary } = useReimbursementSummary(monthStart, dateValue);
  const { data: accountsData } = useAccounts();
  const accounts = accountsData ?? [];
  const { data: cardsData } = useCreditCards();
  const cards = cardsData ?? [];
  const { data: expenseResponse } = useExpenses({ page: 1, pageSize: 200 });
  const { mutate: create, isPending: isCreating } = useCreateReimbursement();
  const { mutate: remove, isPending: isDeleting } = useDeleteReimbursement();
  const expenses = expenseResponse?.data?.data?.items ?? [];
  const selectedExpense = useMemo(() => expenses.find((item) => item.id === expenseId), [expenses, expenseId]);
  const destinations = destinationType === 'account'
    ? accounts.filter((account) => account.isActive)
    : cards.filter((card) => card.isActive);

  const reset = () => {
    setIsFormOpen(false);
    setExpenseId('');
    setDestinationId('');
    setAmount('');
    setDate(dateValue);
    setPerson('');
    setNotes('');
    setDestinationType('account');
  };

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const numericAmount = Number(amount);
    if (!destinationId || !Number.isFinite(numericAmount) || numericAmount <= 0) return;
    create({
      expenseId: expenseId || null,
      destinationType,
      accountId: destinationType === 'account' ? destinationId : null,
      creditCardId: destinationType === 'credit_card' ? destinationId : null,
      amount: numericAmount,
      date,
      person: person || null,
      notes: notes || null,
      idempotencyKey: crypto.randomUUID(),
    }, { onSuccess: reset });
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="font-serif text-2xl font-medium text-[#2C2A29]">Reembolsos</h1>
          <p className="text-sm text-[#7C756E]">Entradas que recuperan gasto, separadas de los ingresos ganados.</p>
        </div>
        <Button onClick={() => setIsFormOpen((open) => !open)} leftIcon={<Plus className="h-4 w-4" />}>
          Registrar reembolso
        </Button>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        {[
          ['Gasto bruto del mes', summary?.grossExpenses ?? 0, '#C97B63'],
          ['Reembolsos recibidos', summary?.reimbursementsReceived ?? 0, '#5C7A99'],
          ['Gasto neto personal', summary?.netPersonalExpenses ?? 0, '#2C2A29'],
        ].map(([label, value, color]) => (
          <Card key={String(label)} className="!rounded-[24px]">
            <p className="text-xs uppercase tracking-wide text-[#7C756E]">{label}</p>
            <p className="mt-2 font-serif text-2xl" style={{ color: String(color) }}>{formatCurrency(Number(value))}</p>
          </Card>
        ))}
      </div>

      {isFormOpen && (
        <Card>
          <CardHeader title="Registrar reembolso" subtitle="No se suma a salario ni a ingresos ganados." />
          <form onSubmit={submit} className="grid gap-4 md:grid-cols-2">
            <label className="text-sm text-[#2C2A29]">Gasto relacionado (opcional)
              <select value={expenseId} onChange={(event) => setExpenseId(event.target.value)}
                className="mt-1 w-full rounded-xl border border-[#EFEAE2] px-3 py-2">
                <option value="">Sin gasto relacionado</option>
                {expenses.map((expense) => (
                  <option key={expense.id} value={expense.id}>
                    {expense.date} · {expense.description || expense.categoryName} · {formatCurrency(expense.amount)}
                  </option>
                ))}
              </select>
            </label>
            <label className="text-sm text-[#2C2A29]">Monto
              <input required min="0.01" step="0.01" type="number" value={amount}
                onChange={(event) => setAmount(event.target.value)}
                className="mt-1 w-full rounded-xl border border-[#EFEAE2] px-3 py-2" />
              {selectedExpense && <span className="mt-1 block text-xs text-[#7C756E]">
                Bruto {formatCurrency(selectedExpense.amount)} · reembolsado {formatCurrency(selectedExpense.reimbursedAmount)} · neto {formatCurrency(selectedExpense.netPersonalAmount)}
              </span>}
            </label>
            <label className="text-sm text-[#2C2A29]">Fecha de recepción
              <input required type="date" value={date} onChange={(event) => setDate(event.target.value)}
                className="mt-1 w-full rounded-xl border border-[#EFEAE2] px-3 py-2" />
            </label>
            <label className="text-sm text-[#2C2A29]">Destino
              <select value={destinationType} onChange={(event) => { setDestinationType(event.target.value as ReimbursementDestinationType); setDestinationId(''); }}
                className="mt-1 w-full rounded-xl border border-[#EFEAE2] px-3 py-2">
                <option value="account">Cuenta receptora</option>
                <option value="credit_card">Tarjeta de crédito</option>
              </select>
            </label>
            <label className="text-sm text-[#2C2A29]">{
              destinationType === 'account' ? 'Cuenta receptora' : 'Tarjeta a abonar'
            }
              <select required value={destinationId} onChange={(event) => setDestinationId(event.target.value)}
                className="mt-1 w-full rounded-xl border border-[#EFEAE2] px-3 py-2">
                <option value="">Selecciona una opción</option>
                {destinations.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
              </select>
              {destinationType === 'credit_card' && <span className="mt-1 block text-xs text-[#7C756E]">Si ya está pagada, el abono queda como saldo a favor; no se convierte en ingreso.</span>}
            </label>
            <label className="text-sm text-[#2C2A29]">Persona u origen (opcional)
              <input value={person} maxLength={160} onChange={(event) => setPerson(event.target.value)}
                className="mt-1 w-full rounded-xl border border-[#EFEAE2] px-3 py-2" />
            </label>
            <label className="text-sm text-[#2C2A29] md:col-span-2">Nota (opcional)
              <textarea value={notes} maxLength={1000} onChange={(event) => setNotes(event.target.value)}
                className="mt-1 min-h-20 w-full rounded-xl border border-[#EFEAE2] px-3 py-2" />
            </label>
            <div className="flex gap-2 md:col-span-2">
              <Button type="submit" disabled={isCreating}>{isCreating ? 'Guardando...' : 'Guardar reembolso'}</Button>
              <Button type="button" variant="secondary" onClick={reset}>Cancelar</Button>
            </div>
          </form>
        </Card>
      )}

      <Card noPadding className="overflow-hidden">
        <CardHeader title="Historial" subtitle="El gasto original siempre se conserva para auditoría." className="px-6 pt-6" />
        {isLoading ? <div className="flex justify-center p-10"><Spinner /></div>
          : isError ? <div className="flex items-center gap-2 p-8 text-[#C97B63]"><AlertCircle className="h-5 w-5" />No se pudo cargar el historial.</div>
          : reimbursements.length === 0 ? <div className="flex flex-col items-center gap-2 p-10 text-[#7C756E]"><Inbox className="h-6 w-6" />Aún no tienes reembolsos registrados.</div>
          : <div className="overflow-x-auto"><table className="w-full text-left text-sm">
            <thead className="bg-[#F3F1EC] text-xs uppercase tracking-wide text-[#7C756E]"><tr>
              <th className="px-6 py-3">Fecha</th><th className="px-6 py-3">Gasto</th><th className="px-6 py-3">Destino</th><th className="px-6 py-3 text-right">Monto</th><th className="px-6 py-3" />
            </tr></thead>
            <tbody className="divide-y divide-[#EFEAE2]">{reimbursements.map((item) => <tr key={item.id}>
              <td className="px-6 py-4">{format(parseDateOnly(item.date), 'dd/MM/yyyy')}</td>
              <td className="px-6 py-4 text-[#7C756E]">{item.expenseDescription || 'Sin gasto relacionado'}</td>
              <td className="px-6 py-4"><span className="inline-flex items-center gap-1 text-[#7C756E]"><ArrowLeftRight className="h-3.5 w-3.5" />{item.accountName || item.creditCardName}</span></td>
              <td className="px-6 py-4 text-right font-medium text-[#5C7A99]">+{formatCurrency(item.amount)}</td>
              <td className="px-6 py-4 text-right"><button onClick={() => setDeletingId(item.id)} className="rounded-lg p-2 text-[#7C756E] hover:bg-[#C97B63]/10 hover:text-[#C97B63]" aria-label="Anular reembolso"><Trash2 className="h-4 w-4" /></button></td>
            </tr>)}</tbody>
          </table></div>}
      </Card>

      <ConfirmDialog isOpen={Boolean(deletingId)} title="¿Anular reembolso?"
        description="Se revertirá el movimiento de cuenta o tarjeta y se conservará la auditoría."
        isLoading={isDeleting} onConfirm={() => deletingId && remove(deletingId, { onSuccess: () => setDeletingId(null) })}
        onCancel={() => setDeletingId(null)} />
    </div>
  );
}

