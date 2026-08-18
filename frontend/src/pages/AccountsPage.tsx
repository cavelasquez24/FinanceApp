import { todayDateOnly } from '../utils/dateOnly';
import { useState } from 'react';
import type { FormEvent } from 'react';
import { ArrowRightLeft, Landmark, Plus, RefreshCw, Wallet } from 'lucide-react';
import { Button, Card, Input, Modal, ModalFooter, Spinner } from '../components/ui';
import {
  useAccounts,
  useAccountTransactions,
  useCreateAccount,
  useUpdateAccount,
  useCreateAccountTransfer,
} from '../features/accounts/hooks/useAccounts';
import type { FinancialAccountType } from '../types/account.types';

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
  const [balances, setBalances] = useState<Record<string, string>>({});
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
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="font-serif text-2xl font-medium text-[#2C2A29]">Cuentas</h1>
          <p className="mt-1 text-sm text-[#7C756E]">
            Tus módulos usan la cuenta predeterminada automáticamente.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" onClick={() => setShowTransfer(true)} leftIcon={<ArrowRightLeft className="h-4 w-4" />}>
            Transferir
          </Button>
          <Button onClick={() => setShowCreate(true)} leftIcon={<Plus className="h-4 w-4" />}>
            Nueva cuenta
          </Button>
        </div>
      </div>


      <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
        {accounts?.map((account) => {
          const currentInput = balances[account.id] ?? String(account.currentBalance);
          return (
            <Card key={account.id} className="!rounded-[24px] !p-5">
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-3">
                  <span className="rounded-xl bg-[#5C7A99]/10 p-2 text-[#5C7A99]">
                    {account.type === 'cash' ? <Wallet className="h-5 w-5" /> : <Landmark className="h-5 w-5" />}
                  </span>
                  <div>
                    <h2 className="font-semibold text-[#2C2A29]">{account.name}</h2>
                    <p className="text-xs capitalize text-[#7C756E]">{account.type}</p>
                  </div>
                </div>
                {account.isDefault && (
                  <span className="rounded-full bg-[#8FA888]/10 px-2.5 py-1 text-xs text-[#6F8B67]">
                    Predeterminada
                  </span>
                )}
              </div>
              <p className="mt-5 text-2xl font-semibold text-[#2C2A29]">
                {money(account.currentBalance)}
              </p>
              <div className="mt-4 flex gap-2">
                <input
                  type="number"
                  step="0.01"
                  value={currentInput}
                  onChange={(e) => setBalances((old) => ({ ...old, [account.id]: e.target.value }))}
                  className="min-w-0 flex-1 rounded-xl border border-[#EFEAE2] px-3 py-2 text-sm"
                  aria-label={`Saldo actual de ${account.name}`}
                />
                <button
                  type="button"
                  onClick={() =>
                    updateAccount.mutate({
                      id: account.id,
                      dto: {
                        name: account.name,
                        currentBalance: Number(currentInput),
                        isDefault: account.isDefault,
                        isActive: account.isActive,
                      },
                    })
                  }
                  className="rounded-xl border border-[#EFEAE2] p-2 text-[#5C7A99] hover:bg-[#5C7A99]/5"
                  title="Conciliar saldo"
                >
                  <RefreshCw className="h-4 w-4" />
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
                        currentBalance: Number(currentInput),
                        isDefault: true,
                        isActive: account.isActive,
                      },
                    })
                  }
                  className="mt-3 text-xs font-medium text-[#5C7A99] hover:underline"
                >
                  Usar como cuenta predeterminada
                </button>
              )}
            </Card>
          );
        })}
      </div>

      <Card className="!rounded-[24px]">
        <h2 className="font-serif text-lg font-medium text-[#2C2A29]">Movimientos recientes</h2>
        <div className="mt-4 divide-y divide-[#EFEAE2]">
          {transactions?.map((transaction) => (
            <div key={transaction.id} className="flex items-center justify-between py-3 text-sm">
              <div>
                <p className="font-medium text-[#2C2A29]">{transaction.description}</p>
                <p className="text-xs text-[#7C756E]">
                  {transaction.accountName} · {transaction.date}
                </p>
              </div>
              <span className={transaction.amount >= 0 ? 'text-[#6F8B67]' : 'text-[#C97B63]'}>
                {transaction.amount >= 0 ? '+' : ''}{money(transaction.amount)}
              </span>
            </div>
          ))}
          {!transactions?.length && (
            <p className="py-6 text-center text-sm text-[#7C756E]">Aún no hay movimientos.</p>
          )}
        </div>
      </Card>

      <Modal isOpen={showCreate} onClose={closeCreate} title="Nueva cuenta">
        <form onSubmit={create} className="space-y-5">
          <div className="flex items-start gap-3 rounded-2xl border border-[#E5E0D8] bg-white/60 p-4">
            <span className="rounded-xl bg-[#5C7A99]/10 p-2.5 text-[#5C7A99]">
              <Wallet className="h-5 w-5" />
            </span>
            <div>
              <p className="font-medium text-[#2C2A29]">Registra dónde administras tu dinero</p>
              <p className="mt-1 text-sm leading-relaxed text-[#7C756E]">
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
            <label className="flex flex-col gap-1.5 text-sm font-medium text-[#2C2A29]">
              Tipo de cuenta
              <select
                value={type}
                onChange={(event) => setType(event.target.value as FinancialAccountType)}
                className="w-full rounded-xl border border-[#EFEAE2] bg-white/70 px-3.5 py-2.5 text-sm text-[#2C2A29] outline-none transition focus:border-[#5C7A99] focus:ring-2 focus:ring-[#5C7A99]/20"
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
          <p className="text-sm leading-relaxed text-[#7C756E]">
            Mueve dinero entre tus cuentas. Esta operación no se registra como ingreso ni como gasto.
          </p>

          <div className="grid gap-3 sm:grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] sm:items-end">
            <AccountSelect label="Cuenta de origen" value={fromAccountId} onChange={setFromAccountId} accounts={activeAccounts} required />
            <span className="mx-auto hidden rounded-full border border-[#E4DED5] bg-white p-2.5 text-[#5C7A99] sm:block">
              <ArrowRightLeft className="h-4 w-4" />
            </span>
            <AccountSelect label="Cuenta de destino" value={toAccountId} onChange={setToAccountId} accounts={activeAccounts} required />
          </div>

          {fromAccount && toAccount && fromAccount.id !== toAccount.id && (
            <div className="grid grid-cols-[1fr_auto_1fr] items-center gap-3 rounded-2xl border border-[#E5E0D8] bg-white/60 p-4">
              <div className="min-w-0">
                <p className="truncate text-sm font-medium text-[#2C2A29]">{fromAccount.name}</p>
                <p className="mt-1 text-xs text-[#7C756E]">Disponible: {money(fromAccount.currentBalance)}</p>
              </div>
              <ArrowRightLeft className="h-4 w-4 text-[#5C7A99]" />
              <div className="min-w-0 text-right">
                <p className="truncate text-sm font-medium text-[#2C2A29]">{toAccount.name}</p>
                <p className="mt-1 text-xs text-[#7C756E]">Saldo: {money(toAccount.currentBalance)}</p>
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
    <label className="flex min-w-0 flex-col gap-1.5 text-sm font-medium text-[#2C2A29]">
      <span>{label}</span>
      <select
        value={value}
        required={required}
        onChange={(event) => onChange(event.target.value)}
        className="w-full rounded-xl border border-[#EFEAE2] bg-white/70 px-3.5 py-2.5 text-sm text-[#2C2A29] outline-none transition focus:border-[#5C7A99] focus:ring-2 focus:ring-[#5C7A99]/20"
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
