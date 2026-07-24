import { useState } from 'react';
import type { FormEvent } from 'react';
import { Landmark, Plus, RefreshCw, Wallet } from 'lucide-react';
import { Button, Card, Input, Spinner } from '../components/ui';
import {
  useAccounts,
  useAccountTransactions,
  useCreateAccount,
  useUpdateAccount,
} from '../features/accounts/hooks/useAccounts';
import type { FinancialAccountType } from '../types/account.types';

const money = (value: number) =>
  new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(value);

export function AccountsPage() {
  const { data: accounts, isLoading } = useAccounts();
  const { data: transactions } = useAccountTransactions(12);
  const createAccount = useCreateAccount();
  const updateAccount = useUpdateAccount();
  const [showCreate, setShowCreate] = useState(false);
  const [name, setName] = useState('');
  const [type, setType] = useState<FinancialAccountType>('cash');
  const [openingBalance, setOpeningBalance] = useState(0);
  const [balances, setBalances] = useState<Record<string, string>>({});

  const create = (event: FormEvent) => {
    event.preventDefault();
    createAccount.mutate(
      { name, type, openingBalance, isDefault: false },
      {
        onSuccess: () => {
          setShowCreate(false);
          setName('');
          setOpeningBalance(0);
        },
      }
    );
  };

  if (isLoading) {
    return <div className="flex h-64 items-center justify-center"><Spinner /></div>;
  }

  return (
    <div className="space-y-6 p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="font-serif text-2xl font-medium text-[#2C2A29]">Cuentas</h1>
          <p className="mt-1 text-sm text-[#7C756E]">
            Tus módulos usan la cuenta predeterminada automáticamente.
          </p>
        </div>
        <Button onClick={() => setShowCreate((value) => !value)}>
          <Plus className="mr-2 h-4 w-4" /> Nueva cuenta
        </Button>
      </div>

      {showCreate && (
        <Card className="!rounded-[24px]">
          <form onSubmit={create} className="grid gap-4 md:grid-cols-4 md:items-end">
            <Input label="Nombre" value={name} onChange={(e) => setName(e.target.value)} required />
            <div>
              <label className="mb-1.5 block text-sm font-medium text-[#2C2A29]">Tipo</label>
              <select
                value={type}
                onChange={(e) => setType(e.target.value as FinancialAccountType)}
                className="w-full rounded-xl border border-[#EFEAE2] bg-white px-3 py-2.5 text-sm"
              >
                <option value="cash">Efectivo o banco</option>
                <option value="savings">Ahorro</option>
                <option value="investment">Inversión</option>
              </select>
            </div>
            <Input
              label="Saldo inicial"
              type="number"
              step="0.01"
              value={openingBalance}
              onChange={(e) => setOpeningBalance(Number(e.target.value))}
            />
            <Button type="submit" isLoading={createAccount.isPending}>Crear cuenta</Button>
          </form>
        </Card>
      )}

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
    </div>
  );
}
