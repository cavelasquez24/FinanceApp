import { todayDateOnly } from '../utils/dateOnly';
import { useState } from 'react';
import type { FormEvent } from 'react';
import { ArrowRightLeft, Landmark, Plus, Scale, Wallet } from 'lucide-react';
import { Button, Card, Input, Modal, ModalFooter, PageHeader, Spinner } from '../components/ui';
import {
  useAccounts,
  useAccountTransactions,
  useCreateAccount,
  useUpdateAccount,
  useCreateAccountTransfer,
} from '../features/accounts/hooks/useAccounts';
import { ReconciliationModal } from '../features/accounts/components/ReconciliationModal';
import type { FinancialAccount, FinancialAccountType } from '../types/account.types';

const money = (value: number) =>
  new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(value);

export function AccountsPage() {
  const { data: accounts, isLoading } = useAccounts();
  const { data: transactions } = useAccountTransactions(12);
  const createAccount = useCreateAccount();
  const updateAccount = useUpdateAccount();
  const createTransfer = useCreateAccountTransfer();
  const [showCreate, setShowCreate] = useState(false);
  const [name, setName] = useState('');
  const [type, setType] = useState<FinancialAccountType>('cash');
  const [openingBalance, setOpeningBalance] = useState(0);
  const [openingDate, setOpeningDate] = useState(() => todayDateOnly());
  const [reconcileAccount, setReconcileAccount] = useState<FinancialAccount | null>(null);
  const [showTransfer, setShowTransfer] = useState(false);
  const [fromAccountId, setFromAccountId] = useState('');
  const [toAccountId, setToAccountId] = useState('');
  const [transferAmount, setTransferAmount] = useState('');
  const [transferDate, setTransferDate] = useState(() => todayDateOnly());
  const [transferDescription, setTransferDescription] = useState('');
  const [transferKey, setTransferKey] = useState<string | null>(null);

  const create = (event: FormEvent) => {
    event.preventDefault();
    createAccount.mutate(
      { name, type, openingBalance, openingDate, isDefault: false },
      {
        onSuccess: () => {
          setShowCreate(false);
          setName('');
          setType('cash');
          setOpeningBalance(0);
          setOpeningDate(todayDateOnly());
        },
      }
    );
  };

  const transfer = (event: FormEvent) => {
    event.preventDefault();
    const idempotencyKey = transferKey ?? crypto.randomUUID();
    setTransferKey(idempotencyKey);
    createTransfer.mutate({
      fromAccountId,
      toAccountId,
      amount: Number(transferAmount),
      date: transferDate,
      description: transferDescription || undefined,
      idempotencyKey,
    }, {
      onSuccess: () => {
        setShowTransfer(false);
        setFromAccountId('');
        setToAccountId('');
        setTransferDate(todayDateOnly());
        setTransferAmount('');
        setTransferDescription('');
        setTransferKey(null);
      },
    });

  };
  const closeCreate = () => {
    if (createAccount.isPending) return;
    setShowCreate(false);
    setName('');
    setType('cash');
    setOpeningBalance(0);
    setOpeningDate(todayDateOnly());
  };

  const closeTransfer = () => {
    if (createTransfer.isPending) return;
    setShowTransfer(false);
    setFromAccountId('');
    setToAccountId('');
    setTransferAmount('');
    setTransferDate(todayDateOnly());
    setTransferDescription('');
    setTransferKey(null);
  };
  if (isLoading) {
    return <div className="flex h-64 items-center justify-center"><Spinner /></div>;
  }
  const activeAccounts = accounts?.filter((account) => account.isActive) ?? [];
  const fromAccount = activeAccounts.find((account) => account.id === fromAccountId);
  const toAccount = activeAccounts.find((account) => account.id === toAccountId);


  return (
    <div className="space-y-6 p-6">
      <PageHeader
        eyebrow="Patrimonio"
        title="Cuentas"
        description="Tus módulos usan la cuenta predeterminada automáticamente."
        action={
          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => setShowTransfer(true)} leftIcon={<ArrowRightLeft className="h-4 w-4" />}>
              Transferir
            </Button>
            <Button onClick={() => setShowCreate(true)} leftIcon={<Plus className="h-4 w-4" />}>
              Nueva cuenta
            </Button>
          </div>
        }
      />


      <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
        {accounts?.map((account) => {
          return (
            <Card key={account.id} className="!rounded-[24px] !p-5">
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-3">
                  <span className="rounded-xl bg-finflow-blue/10 p-2 text-finflow-blue">
                    {account.type === 'cash' ? <Wallet className="h-5 w-5" /> : <Landmark className="h-5 w-5" />}
                  </span>
                  <div>
                    <h2 className="font-semibold text-finflow-dark">{account.name}</h2>
                    <p className="text-xs capitalize text-finflow-muted">{account.type}</p>
                  </div>
                </div>
                {account.isDefault && (
                  <span className="rounded-full bg-finflow-green/10 px-2.5 py-1 text-xs text-[#6F8B67]">
                    Predeterminada
                  </span>
                )}
              </div>
              <p className="mt-5 text-2xl font-semibold text-finflow-dark">
                {money(account.currentBalance)}
              </p>
              <div className="mt-4">
                <button
                  type="button"
                  onClick={() => setReconcileAccount(account)}
                  className="flex w-full items-center justify-center gap-2 rounded-xl border border-[#EFEAE2] px-3 py-2 text-sm text-finflow-blue hover:bg-finflow-blue/5 transition-colors"
                >
                  <Scale className="h-4 w-4" />
                  Conciliar saldo
                </button>
              </div>
              {!account.isDefault && (
                <button
                  type="button"
                  onClick={() =>
                    updateAccount.mutate({
                      id: account.id,
                      dto: {
                        name: account.name,
                        currentBalance: account.currentBalance,
                        isDefault: true,
                        isActive: account.isActive,
                      },
                    })
                  }
                  className="mt-3 text-xs font-medium text-finflow-blue hover:underline"
                >
                  Usar como cuenta predeterminada
                </button>
              )}
            </Card>
          );
        })}
      </div>

      <Card className="!rounded-[24px]">
        <h2 className="font-serif text-lg font-medium text-finflow-dark">Movimientos recientes</h2>
        <div className="mt-4 divide-y divide-[#EFEAE2]">
          {transactions?.map((transaction) => (
            <div key={transaction.id} className="flex items-center justify-between py-3 text-sm">
              <div>
                <p className="font-medium text-finflow-dark">{transaction.description}</p>
                <p className="text-xs text-finflow-muted">
                  {transaction.accountName} · {transaction.date}
                </p>
              </div>
              <span className={transaction.amount >= 0 ? 'text-[#6F8B67]' : 'text-finflow-rust'}>
                {transaction.amount >= 0 ? '+' : ''}{money(transaction.amount)}
              </span>
            </div>
          ))}
          {!transactions?.length && (
            <p className="py-6 text-center text-sm text-finflow-muted">Aún no hay movimientos.</p>
          )}
        </div>
      </Card>

      {reconcileAccount && (
        <ReconciliationModal
          account={reconcileAccount}
          onClose={() => setReconcileAccount(null)}
        />
      )}

      <Modal isOpen={showCreate} onClose={closeCreate} title="Nueva cuenta">
        <form onSubmit={create} className="space-y-5">
          <div className="flex items-start gap-3 rounded-2xl border border-[#E5E0D8] bg-white/60 p-4">
            <span className="rounded-xl bg-finflow-blue/10 p-2.5 text-finflow-blue">
              <Wallet className="h-5 w-5" />
            </span>
            <div>
              <p className="font-medium text-finflow-dark">Registra dónde administras tu dinero</p>
              <p className="mt-1 text-sm leading-relaxed text-finflow-muted">
                Podrás conciliar el saldo y usar esta cuenta como origen o destino de transferencias.
              </p>
            </div>
          </div>

          <Input
            label="Nombre de la cuenta"
            placeholder="Ej. Banco principal"
            value={name}
            onChange={(event) => setName(event.target.value)}
            autoFocus
            required
          />

          <div className="grid gap-4 sm:grid-cols-2">
            <label className="flex flex-col gap-1.5 text-sm font-medium text-finflow-dark">
              Tipo de cuenta
              <select
                value={type}
                onChange={(event) => setType(event.target.value as FinancialAccountType)}
                className="w-full rounded-xl border border-[#EFEAE2] bg-white/70 px-3.5 py-2.5 text-sm text-finflow-dark outline-none transition focus:border-finflow-blue focus:ring-2 focus:ring-finflow-blue/20"
              >
                <option value="cash">Efectivo o banco</option>
                <option value="savings">Ahorro</option>
                <option value="investment">Inversión</option>
              </select>
            </label>
            <div className="space-y-3">
              <Input
                label="Saldo de apertura"
                type="number"
                step="0.01"
                value={openingBalance}
                onChange={(event) => setOpeningBalance(Number(event.target.value))}
                hint="Aumenta el saldo y el patrimonio inicial; no es un ingreso ni disponible presupuestario."
              />
              <Input
                label="Fecha de apertura"
                type="date"
                value={openingDate}
                max={todayDateOnly()}
                onChange={(event) => setOpeningDate(event.target.value)}
                required
              />
            </div>
          </div>

          <ModalFooter>
            <Button type="button" variant="secondary" onClick={closeCreate} disabled={createAccount.isPending}>
              Cancelar
            </Button>
            <Button type="submit" isLoading={createAccount.isPending} leftIcon={<Plus className="h-4 w-4" />}>
              Crear cuenta
            </Button>
          </ModalFooter>
        </form>
      </Modal>

      <Modal isOpen={showTransfer} onClose={closeTransfer} title="Transferir entre cuentas" className="!max-w-2xl">
        <form onSubmit={transfer} className="space-y-5">
          <p className="text-sm leading-relaxed text-finflow-muted">
            Mueve dinero entre tus cuentas. Esta operación no se registra como ingreso ni como gasto.
          </p>

          <div className="grid gap-3 sm:grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] sm:items-end">
            <AccountSelect label="Cuenta de origen" value={fromAccountId} onChange={setFromAccountId} accounts={activeAccounts} required />
            <span className="mx-auto hidden rounded-full border border-[#E4DED5] bg-white p-2.5 text-finflow-blue sm:block">
              <ArrowRightLeft className="h-4 w-4" />
            </span>
            <AccountSelect label="Cuenta de destino" value={toAccountId} onChange={setToAccountId} accounts={activeAccounts} required />
          </div>

          {fromAccount && toAccount && fromAccount.id !== toAccount.id && (
            <div className="grid grid-cols-[1fr_auto_1fr] items-center gap-3 rounded-2xl border border-[#E5E0D8] bg-white/60 p-4">
              <div className="min-w-0">
                <p className="truncate text-sm font-medium text-finflow-dark">{fromAccount.name}</p>
                <p className="mt-1 text-xs text-finflow-muted">Disponible: {money(fromAccount.currentBalance)}</p>
              </div>
              <ArrowRightLeft className="h-4 w-4 text-finflow-blue" />
              <div className="min-w-0 text-right">
                <p className="truncate text-sm font-medium text-finflow-dark">{toAccount.name}</p>
                <p className="mt-1 text-xs text-finflow-muted">Saldo: {money(toAccount.currentBalance)}</p>
              </div>
            </div>
          )}

          <div className="grid gap-4 sm:grid-cols-2">
            <Input
              label="Monto"
              type="number"
              step="0.01"
              min="0.01"
              value={transferAmount}
              onChange={(event) => setTransferAmount(event.target.value)}
              hint={fromAccount ? `Máximo disponible: ${money(fromAccount.currentBalance)}` : 'Selecciona una cuenta de origen.'}
              required
            />
            <Input
              label="Fecha"
              type="date"
              value={transferDate}
              onChange={(event) => setTransferDate(event.target.value)}
              required
            />
          </div>

          <Input
            label="Nota"
            placeholder="Ej. Fondos para gastos del mes"
            value={transferDescription}
            maxLength={300}
            onChange={(event) => setTransferDescription(event.target.value)}
            hint="Opcional · máximo 300 caracteres."
          />

          {fromAccountId === toAccountId && fromAccountId && (
            <p className="rounded-xl bg-[#FBEEEA] px-3.5 py-2.5 text-sm text-[#B5573F]">
              La cuenta de origen y la cuenta de destino deben ser distintas.
            </p>
          )}

          <ModalFooter>
            <Button type="button" variant="secondary" onClick={closeTransfer} disabled={createTransfer.isPending}>
              Cancelar
            </Button>
            <Button
              type="submit"
              isLoading={createTransfer.isPending}
              disabled={!fromAccountId || !toAccountId || fromAccountId === toAccountId || Number(transferAmount) <= 0}
              leftIcon={<ArrowRightLeft className="h-4 w-4" />}
            >
              Confirmar transferencia
            </Button>
          </ModalFooter>
        </form>
      </Modal>
    </div>
  );
}

function AccountSelect({ label, value, onChange, accounts, required }: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  accounts: { id: string; name: string; currentBalance: number; isActive: boolean }[];
  required?: boolean;
}) {
  return (
    <label className="flex min-w-0 flex-col gap-1.5 text-sm font-medium text-finflow-dark">
      <span>{label}</span>
      <select
        value={value}
        required={required}
        onChange={(event) => onChange(event.target.value)}
        className="w-full rounded-xl border border-[#EFEAE2] bg-white/70 px-3.5 py-2.5 text-sm text-finflow-dark outline-none transition focus:border-finflow-blue focus:ring-2 focus:ring-finflow-blue/20"
      >
        <option value="">Selecciona una cuenta</option>
        {accounts.map((account) => (
          <option key={account.id} value={account.id}>
            {account.name} · {money(account.currentBalance)}
          </option>
        ))}
      </select>
    </label>
  );
}
