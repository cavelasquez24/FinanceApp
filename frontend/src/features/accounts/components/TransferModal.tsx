import { useState } from 'react';
import type { FormEvent } from 'react';
import { AlertTriangle, ArrowRightLeft } from 'lucide-react';
import {
  SavingsModalShell,
  SavingsModalSection,
  SavingsField,
  SavingsModalActions,
} from '../../savings/components/SavingsModalShell';
import { useCreateTransfer } from '../hooks/useTransfers';
import { todayDateOnly } from '../../../utils/dateOnly';
import type { FinancialAccount } from '../../../types/account.types';

interface Props {
  accounts: FinancialAccount[];
  onClose: () => void;
}

const money = (value: number) =>
  new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(value);

const inputClass =
  'mt-1 w-full rounded-xl border border-[#D8D0C5] bg-white px-4 py-2.5 text-finflow-dark focus:border-[#7A4B3A] focus:outline-none';

export function TransferModal({ accounts, onClose }: Props) {
  const [fromAccountId, setFromAccountId] = useState('');
  const [toAccountId, setToAccountId] = useState('');
  const [amount, setAmount] = useState('');
  const [transferDate, setTransferDate] = useState(() => todayDateOnly());
  const [description, setDescription] = useState('');

  // Se genera una sola vez por modal: si el usuario reintenta tras un
  // timeout de red, el backend reconoce el duplicado y no lo repite.
  const [transferGroupId] = useState(() => crypto.randomUUID());

  const { mutate, isPending } = useCreateTransfer(onClose);

  const activeAccounts = accounts.filter((account) => account.isActive);
  const fromAccount = activeAccounts.find((account) => account.id === fromAccountId);
  const destinationAccounts = activeAccounts.filter((account) => account.id !== fromAccountId);

  const numericAmount = Number(amount);
  const hasValidAmount = Number.isFinite(numericAmount) && numericAmount > 0;
  const exceedsBalance = !!fromAccount && hasValidAmount && numericAmount > fromAccount.currentBalance;

  const handleFromChange = (value: string) => {
    setFromAccountId(value);
    // Evita que origen y destino queden en la misma cuenta al cambiar el origen.
    if (value === toAccountId) setToAccountId('');
  };

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (!fromAccountId || !toAccountId || !hasValidAmount) return;
    mutate({
      fromAccountId,
      toAccountId,
      amount: numericAmount,
      transferDate,
      description: description.trim() || undefined,
      transferGroupId,
    });
  };

  return (
    <SavingsModalShell
      eyebrow="Movimiento entre cuentas"
      title="Transferir"
      description="Mueve dinero entre tus propias cuentas. Esta operación no se registra como ingreso ni como gasto."
      onClose={onClose}
      maxWidth="max-w-xl"
    >
      <form onSubmit={handleSubmit} className="space-y-5 p-6 sm:p-8">
        <SavingsModalSection
          icon={<ArrowRightLeft className="h-4 w-4" />}
          title="Cuentas"
          description="El destino excluye automáticamente la cuenta de origen."
        >
          <div className="space-y-4">
            <SavingsField
              label="Cuenta de origen"
              hint={fromAccount ? `Disponible: ${money(fromAccount.currentBalance)}` : undefined}
            >
              <select
                value={fromAccountId}
                onChange={(event) => handleFromChange(event.target.value)}
                className={inputClass}
                required
              >
                <option value="">Selecciona una cuenta</option>
                {activeAccounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.name} · {money(account.currentBalance)}
                  </option>
                ))}
              </select>
            </SavingsField>

            <SavingsField label="Cuenta de destino">
              <select
                value={toAccountId}
                onChange={(event) => setToAccountId(event.target.value)}
                className={inputClass}
                disabled={!fromAccountId}
                required
              >
                <option value="">
                  {fromAccountId ? 'Selecciona una cuenta' : 'Elige primero el origen'}
                </option>
                {destinationAccounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.name} · {money(account.currentBalance)}
                  </option>
                ))}
              </select>
            </SavingsField>
          </div>
        </SavingsModalSection>

        <div className="grid gap-4 sm:grid-cols-2">
          <SavingsField label="Monto">
            <input
              type="number"
              step="0.01"
              min="0.01"
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              placeholder="0.00"
              className={inputClass}
              required
            />
          </SavingsField>

          <SavingsField label="Fecha">
            <input
              type="date"
              value={transferDate}
              onChange={(event) => setTransferDate(event.target.value)}
              className={inputClass}
              required
            />
          </SavingsField>
        </div>

        {/* Advertencia informativa: no bloquea el envío. */}
        {exceedsBalance && fromAccount && (
          <div className="flex items-start gap-3 rounded-2xl border border-amber-200 bg-amber-50 p-4">
            <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-amber-200 text-amber-800">
              <AlertTriangle className="h-4 w-4" />
            </span>
            <div>
              <p className="text-sm font-semibold text-amber-900">
                El monto supera el saldo disponible
              </p>
              <p className="text-xs text-amber-800">
                {fromAccount.name} tiene {money(fromAccount.currentBalance)}. Puedes continuar, pero
                la cuenta quedará en {money(fromAccount.currentBalance - numericAmount)}.
              </p>
            </div>
          </div>
        )}

        <SavingsField label="Descripción (opcional)" hint="Máximo 300 caracteres.">
          <input
            type="text"
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            maxLength={300}
            placeholder="Ej. Fondos para gastos del mes"
            className={inputClass}
          />
        </SavingsField>

        <SavingsModalActions
          onClose={onClose}
          isPending={isPending}
          submitLabel="Confirmar transferencia"
          disabled={!fromAccountId || !toAccountId || !hasValidAmount}
        />
      </form>
    </SavingsModalShell>
  );
}
