import { todayDateOnly } from '../utils/dateOnly';
import { useEffect, useState, type FormEvent } from 'react';
import { CreditCard as CreditCardIcon, Landmark, Plus, ReceiptText } from 'lucide-react';
import { Button, Card, KpiCard, Modal, PageHeader, PageSpinner, Table, TableBody, TableEmpty, TableHead, Td, Th, Tr } from '../components/ui';
import { useAccounts } from '../features/accounts/hooks/useAccounts';
import { useCategories } from '../features/categories/hooks/useCategories';
import {
  useAddCreditCardCharge,
  useCreateCreditCard,
  useCreditCards,
  useCreditCardTransactions,
  useCreditCardPayments,
  usePayCreditCard,
  useVoidCreditCardPayment,
} from '../features/credit-cards/hooks/useCreditCards';
import type { CreditCard, CreditCardPayment } from '../types/credit-card.types';
import { formatCurrency } from '../utils/formatCurrency';

const today = () => todayDateOnly();
const inputClass = 'w-full rounded-xl border border-[#EFEAE2] bg-white px-3 py-2.5 text-sm text-finflow-dark focus:border-finflow-blue focus:outline-none focus:ring-2 focus:ring-finflow-blue/20';
const transactionLabels: Record<string, string> = {
  opening_balance: 'Saldo inicial', purchase: 'Compra', payment: 'Pago', payment_reversal: 'Anulación de pago',
  interest: 'Interés', fee: 'Comisión', refund: 'Reembolso', adjustment: 'Ajuste',
};

export default function CreditCardsPage() {
  const { data: cards, isLoading } = useCreditCards();
  const [selectedId, setSelectedId] = useState<string>();
  const [createOpen, setCreateOpen] = useState(false);
  const [paymentCard, setPaymentCard] = useState<CreditCard | null>(null);
  const [chargeCard, setChargeCard] = useState<CreditCard | null>(null);
  const [voidingPayment, setVoidingPayment] = useState<CreditCardPayment | null>(null);

  useEffect(() => {
    if (!selectedId && cards?.length) setSelectedId(cards[0].id);
    if (selectedId && cards && !cards.some((card) => card.id === selectedId)) {
      setSelectedId(cards[0]?.id);
    }
  }, [cards, selectedId]);

  const { data: transactions, isLoading: loadingTransactions } = useCreditCardTransactions(selectedId);
  const { data: payments } = useCreditCardPayments(selectedId);
  const activeCards = cards?.filter((card) => card.isActive) ?? [];
  const totalBalance = cards?.reduce((sum, card) => sum + card.currentBalance, 0) ?? 0;
  const totalAvailable = activeCards.reduce(
    (sum, card) => sum + (card.availableCredit ?? 0), 0
  );
  const hasKnownLimits = activeCards.some((card) => card.creditLimit != null);

  if (isLoading) return <PageSpinner label="Cargando tarjetas..." />;

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Patrimonio"
        title="Tarjetas de crédito"
        description="Compras, pasivo y pagos sin contabilizar el gasto dos veces"
        action={<Button onClick={() => setCreateOpen(true)} leftIcon={<Plus className="h-4 w-4" />}>Nueva tarjeta</Button>}
      />

      <div className="grid gap-5 md:grid-cols-3">
        <KpiCard label="Pasivo de tarjetas" value={formatCurrency(totalBalance)} icon={<CreditCardIcon className="h-5 w-5" />} />
        <KpiCard label="Crédito disponible conocido" value={hasKnownLimits ? formatCurrency(totalAvailable) : 'Sin límites registrados'} icon={<Landmark className="h-5 w-5" />} />
        <KpiCard label="Tarjetas activas" value={String(activeCards.length)} icon={<ReceiptText className="h-5 w-5" />} />
      </div>

      <section className="grid gap-5 lg:grid-cols-2 xl:grid-cols-3">
        {cards?.map((card) => (
          <Card
            key={card.id}
            className={`cursor-pointer transition-shadow hover:shadow-md ${selectedId === card.id ? 'ring-2 ring-finflow-blue/30' : ''}`}
            onClick={() => setSelectedId(card.id)}
          >
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="font-semibold text-finflow-dark">{card.name}</p>
                <p className="mt-1 text-xs text-finflow-muted">Corte día {card.closingDay} · Vence día {card.dueDay}</p>
              </div>
              <span className={`rounded-full px-2.5 py-1 text-xs font-medium ${card.isActive ? 'bg-finflow-green/15 text-[#66805F]' : 'bg-[#EFEAE2] text-finflow-muted'}`}>
                {card.isActive ? 'Activa' : 'Inactiva'}
              </span>
            </div>
            <div className="mt-6">
              <p className="text-xs uppercase tracking-wide text-finflow-muted">Saldo pendiente</p>
              <p className="mt-1 text-3xl font-semibold text-finflow-dark">{formatCurrency(card.currentBalance)}</p>
              {card.creditLimit != null && (
                <p className="mt-2 text-xs text-finflow-muted">Disponible {formatCurrency(card.availableCredit ?? 0)} de {formatCurrency(card.creditLimit)}</p>
              )}
            </div>
            <div className="mt-6 flex gap-2" onClick={(event) => event.stopPropagation()}>
              <Button size="sm" disabled={!card.isActive || card.currentBalance <= 0} onClick={() => setPaymentCard(card)}>Pagar</Button>
              <Button size="sm" variant="secondary" disabled={!card.isActive} onClick={() => setChargeCard(card)}>Interés / comisión</Button>
            </div>
          </Card>
        ))}
        {!cards?.length && (
          <Card className="lg:col-span-2 xl:col-span-3">
            <p className="text-center text-sm text-finflow-muted">Registra tu primera tarjeta para asociarla a compras y controlar su pasivo.</p>
          </Card>
        )}
      </section>

      <Card noPadding>
        <div className="border-b border-[#EFEAE2] p-6">
          <h2 className="text-lg font-semibold text-finflow-dark">Historial auditable</h2>
          <p className="mt-1 text-xs text-finflow-muted">El corte organiza compras; no aparece como movimiento económico.</p>
        </div>
        <Table>
          <TableHead><Th>Fecha</Th><Th>Tipo</Th><Th>Descripción</Th><Th className="text-right">Movimiento del pasivo</Th></TableHead>
          <TableBody>
            {transactions?.map((transaction) => (
              <Tr key={transaction.id}>
                <Td>{transaction.date}</Td>
                <Td>{transactionLabels[transaction.type] ?? transaction.type}</Td>
                <Td>{transaction.description}</Td>
                <Td className={`text-right font-semibold ${transaction.amount < 0 ? 'text-finflow-green' : 'text-finflow-rust'}`}>
                  {transaction.amount > 0 ? '+' : ''}{formatCurrency(transaction.amount)}
                </Td>
              </Tr>
            ))}
            {!loadingTransactions && (!transactions || transactions.length === 0) && (
              <TableEmpty colSpan={4} message={selectedId ? 'Esta tarjeta aún no tiene movimientos.' : 'Selecciona una tarjeta.'} />
            )}
          </TableBody>
        </Table>
      </Card>

      <Card noPadding>
        <div className="border-b border-[#EFEAE2] p-6">
          <h2 className="text-lg font-semibold text-finflow-dark">Pagos registrados</h2>
          <p className="mt-1 text-xs text-finflow-muted">Una corrección crea una reversión; el pago original permanece visible.</p>
        </div>
        <Table>
          <TableHead><Th>Fecha</Th><Th>Cuenta</Th><Th>Principal</Th><Th>Comisión</Th><Th>Estado</Th><Th className="text-right">Acción</Th></TableHead>
          <TableBody>
            {payments?.map((payment) => (
              <Tr key={payment.id}>
                <Td>{payment.paymentDate}</Td>
                <Td>{payment.sourceAccountName || 'Cuenta origen'}</Td>
                <Td>{formatCurrency(payment.principalAmount)}</Td>
                <Td>{formatCurrency(payment.commissionAmount)}</Td>
                <Td>{payment.isVoided ? `Anulado · ${payment.voidReason}` : 'Aplicado'}</Td>
                <Td className="text-right">
                  <Button size="sm" variant="ghost" disabled={payment.isVoided} onClick={() => setVoidingPayment(payment)}>
                    {payment.isVoided ? 'Anulado' : 'Anular'}
                  </Button>
                </Td>
              </Tr>
            ))}
            {(!payments || payments.length === 0) && (
              <TableEmpty colSpan={6} message={selectedId ? 'Esta tarjeta aún no tiene pagos.' : 'Selecciona una tarjeta.'} />
            )}
          </TableBody>
        </Table>
      </Card>

      <Modal isOpen={createOpen} onClose={() => setCreateOpen(false)} title="Nueva tarjeta de crédito">
        <CreateCardForm onDone={() => setCreateOpen(false)} />
      </Modal>
      <Modal isOpen={Boolean(paymentCard)} onClose={() => setPaymentCard(null)} title={`Pagar ${paymentCard?.name ?? ''}`}>
        {paymentCard && <PaymentForm card={paymentCard} onDone={() => setPaymentCard(null)} />}
      </Modal>
      <Modal isOpen={Boolean(chargeCard)} onClose={() => setChargeCard(null)} title={`Cargo en ${chargeCard?.name ?? ''}`}>
        {chargeCard && <ChargeForm card={chargeCard} onDone={() => setChargeCard(null)} />}
      </Modal>
      <Modal isOpen={Boolean(voidingPayment)} onClose={() => setVoidingPayment(null)} title="Anular pago">
        {voidingPayment && <VoidPaymentForm payment={voidingPayment} onDone={() => setVoidingPayment(null)} />}
      </Modal>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return <label className="flex flex-col gap-1.5 text-sm font-medium text-finflow-dark"><span>{label}</span>{children}</label>;
}

function CreateCardForm({ onDone }: { onDone: () => void }) {
  const { mutate, isPending } = useCreateCreditCard();
  const [form, setForm] = useState({ name: '', openingBalance: '0', openingDate: today(), creditLimit: '', closingDay: '1', dueDay: '15', notes: '' });
  const set = (key: keyof typeof form, value: string) => setForm((current) => ({ ...current, [key]: value }));
  const submit = (event: FormEvent) => {
    event.preventDefault();
    mutate({
      name: form.name, openingBalance: Number(form.openingBalance), openingDate: form.openingDate,
      creditLimit: form.creditLimit ? Number(form.creditLimit) : null,
      closingDay: Number(form.closingDay), dueDay: Number(form.dueDay), notes: form.notes || undefined,
    }, { onSuccess: onDone });
  };
  return (
    <form onSubmit={submit} className="space-y-4">
      <Field label="Nombre"><input required maxLength={120} className={inputClass} value={form.name} onChange={(e) => set('name', e.target.value)} /></Field>
      <div className="grid grid-cols-2 gap-4">
        <Field label="Saldo inicial"><input required min="0" step="0.01" type="number" className={inputClass} value={form.openingBalance} onChange={(e) => set('openingBalance', e.target.value)} /></Field>
        <Field label="Fecha del saldo"><input required max={today()} type="date" className={inputClass} value={form.openingDate} onChange={(e) => set('openingDate', e.target.value)} /></Field>
      </div>
      <Field label="Límite (opcional)"><input min="0.01" step="0.01" type="number" className={inputClass} value={form.creditLimit} onChange={(e) => set('creditLimit', e.target.value)} /></Field>
      <div className="grid grid-cols-2 gap-4">
        <Field label="Día de corte"><input required min="1" max="31" type="number" className={inputClass} value={form.closingDay} onChange={(e) => set('closingDay', e.target.value)} /></Field>
        <Field label="Día de vencimiento"><input required min="1" max="31" type="number" className={inputClass} value={form.dueDay} onChange={(e) => set('dueDay', e.target.value)} /></Field>
      </div>
      <Field label="Notas"><input maxLength={1000} className={inputClass} value={form.notes} onChange={(e) => set('notes', e.target.value)} /></Field>
      <p className="text-xs text-finflow-muted">El saldo inicial crea pasivo, no un gasto histórico.</p>
      <div className="flex justify-end"><Button type="submit" isLoading={isPending}>Registrar tarjeta</Button></div>
    </form>
  );
}

function PaymentForm({ card, onDone }: { card: CreditCard; onDone: () => void }) {
  const { data: accounts } = useAccounts();
  const { data: categories } = useCategories('expense');
  const { mutate, isPending } = usePayCreditCard();
  const cashAccounts = accounts?.filter((account) => account.type === 'cash' && account.isActive) ?? [];
  const [key] = useState(() => crypto.randomUUID());
  const [form, setForm] = useState({ sourceAccountId: '', principal: String(card.currentBalance), commission: '0', categoryId: '', date: today(), notes: '' });
  const commission = Number(form.commission);
  const submit = (event: FormEvent) => {
    event.preventDefault();
    mutate({ id: card.id, dto: {
      sourceAccountId: form.sourceAccountId, principalAmount: Number(form.principal), commissionAmount: commission,
      commissionCategoryId: commission > 0 ? form.categoryId : null, paymentDate: form.date,
      notes: form.notes || undefined, idempotencyKey: key,
    } }, { onSuccess: onDone });
  };
  return (
    <form onSubmit={submit} className="space-y-4">
      <div className="rounded-xl bg-[#F3F1EC] p-3 text-sm text-finflow-muted">Saldo actual: <strong className="text-finflow-dark">{formatCurrency(card.currentBalance)}</strong></div>
      <Field label="Cuenta bancaria origen"><select required className={inputClass} value={form.sourceAccountId} onChange={(e) => setForm({ ...form, sourceAccountId: e.target.value })}><option value="">Selecciona una cuenta...</option>{cashAccounts.map((account) => <option key={account.id} value={account.id}>{account.name} · {formatCurrency(account.currentBalance)}</option>)}</select></Field>
      <div className="grid grid-cols-2 gap-4">
        <Field label="Principal"><input required min="0.01" max={card.currentBalance} step="0.01" type="number" className={inputClass} value={form.principal} onChange={(e) => setForm({ ...form, principal: e.target.value })} /></Field>
        <Field label="Comisión"><input required min="0" step="0.01" type="number" className={inputClass} value={form.commission} onChange={(e) => setForm({ ...form, commission: e.target.value })} /></Field>
      </div>
      {commission > 0 && <Field label="Categoría de la comisión"><select required className={inputClass} value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })}><option value="">Selecciona una categoría...</option>{categories?.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></Field>}
      <Field label="Fecha"><input required max={today()} type="date" className={inputClass} value={form.date} onChange={(e) => setForm({ ...form, date: e.target.value })} /></Field>
      <Field label="Nota"><input maxLength={1000} className={inputClass} value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} /></Field>
      <p className="text-xs text-finflow-muted">El principal reduce banco y pasivo. Solo la comisión crea un gasto nuevo.</p>
      <div className="flex justify-end"><Button type="submit" isLoading={isPending}>Registrar pago</Button></div>
    </form>
  );
}

function ChargeForm({ card, onDone }: { card: CreditCard; onDone: () => void }) {
  const { data: categories } = useCategories('expense');
  const { mutate, isPending } = useAddCreditCardCharge();
  const [key] = useState(() => crypto.randomUUID());
  const [form, setForm] = useState({ type: 'interest' as 'interest' | 'fee', categoryId: '', amount: '', date: today(), description: '' });
  const submit = (event: FormEvent) => {
    event.preventDefault();
    mutate({ id: card.id, dto: { ...form, amount: Number(form.amount), description: form.description || undefined, idempotencyKey: key } }, { onSuccess: onDone });
  };
  return (
    <form onSubmit={submit} className="space-y-4">
      <Field label="Tipo"><select className={inputClass} value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value as 'interest' | 'fee' })}><option value="interest">Interés</option><option value="fee">Comisión o cargo</option></select></Field>
      <Field label="Categoría"><select required className={inputClass} value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })}><option value="">Selecciona una categoría...</option>{categories?.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></Field>
      <div className="grid grid-cols-2 gap-4">
        <Field label="Monto"><input required min="0.01" step="0.01" type="number" className={inputClass} value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} /></Field>
        <Field label="Fecha"><input required max={today()} type="date" className={inputClass} value={form.date} onChange={(e) => setForm({ ...form, date: e.target.value })} /></Field>
      </div>
      <Field label="Descripción"><input maxLength={300} className={inputClass} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></Field>
      <p className="text-xs text-finflow-muted">Este cargo crea un gasto explícito y aumenta el pasivo de la tarjeta.</p>
      <div className="flex justify-end"><Button type="submit" isLoading={isPending}>Registrar cargo</Button></div>
    </form>
  );
}

function VoidPaymentForm({ payment, onDone }: { payment: CreditCardPayment; onDone: () => void }) {
  const { mutate, isPending } = useVoidCreditCardPayment();
  const [key] = useState(() => crypto.randomUUID());
  const [date, setDate] = useState(today());
  const [reason, setReason] = useState('');
  const submit = (event: FormEvent) => {
    event.preventDefault();
    mutate({
      id: payment.creditCardId, paymentId: payment.id,
      dto: { date, reason, idempotencyKey: key },
    }, { onSuccess: onDone });
  };
  return (
    <form onSubmit={submit} className="space-y-4">
      <div className="rounded-xl bg-finflow-rust/10 p-3 text-sm text-finflow-muted">
        Se restaurarán {formatCurrency(payment.principalAmount + payment.commissionAmount)} en la cuenta origen,
        el principal volverá al pasivo y la comisión dejará de contar como gasto.
      </div>
      <Field label="Fecha de corrección"><input required max={today()} type="date" className={inputClass} value={date} onChange={(event) => setDate(event.target.value)} /></Field>
      <Field label="Motivo"><input required maxLength={500} className={inputClass} value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Ej: cuenta origen equivocada" /></Field>
      <p className="text-xs text-finflow-muted">El pago original no se elimina; quedará marcado como anulado junto con su reversión.</p>
      <div className="flex justify-end"><Button type="submit" variant="danger" isLoading={isPending}>Anular y reversar</Button></div>
    </form>
  );
}
