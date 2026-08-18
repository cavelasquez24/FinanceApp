import { AlertTriangle, Landmark, LineChart, WalletCards } from 'lucide-react';
import { Card } from '../../../components/ui';
import type { FinancialPosition } from '../../../types/dashboard.types';

interface Props {
  position: FinancialPosition;
}

const currency = (value: number) =>
  new Intl.NumberFormat('es-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(value);

const formatDate = (value: string) => {
  const [year, month, day] = value.split('-').map(Number);
  return new Intl.DateTimeFormat('es-EC', {
    dateStyle: 'medium',
  }).format(new Date(year, month - 1, day));
};

export function FinancialPositionSummary({ position }: Props) {
  const actionableWarnings = position.warnings.filter(
    (warning) => warning.code !== 'CURRENT_SNAPSHOT_ONLY',
  );

  return (
    <Card className="!rounded-[28px] !p-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="font-serif text-lg font-medium text-[#2C2A29]">Posición patrimonial</h2>
          <p className="mt-1 text-sm text-[#7C756E]">
            Activos reales menos pasivos externos · corte {formatDate(position.asOf)}
          </p>
        </div>
        <div className="text-right">
          <p className="text-xs uppercase tracking-wide text-[#7C756E]">Patrimonio neto</p>
          <p className="mt-1 text-2xl font-semibold text-[#2C2A29]">{currency(position.netWorth)}</p>
        </div>
      </div>

      <div className="mt-6 grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        <PositionItem
          label="Efectivo y bancos"
          value={position.assets.cashAccounts}
          icon={<WalletCards className="h-4 w-4" />}
        />
        <PositionItem
          label="Cuentas de ahorro"
          value={position.assets.savingsAccounts}
          icon={<Landmark className="h-4 w-4" />}
        />
        <PositionItem
          label="Inversiones"
          value={position.assets.investments}
          detail={`${currency(position.assets.investmentCostBasis)} aportado · ${currency(position.assets.investmentUnrealizedGainLoss)} no realizado${
            position.assets.uninvestedInvestmentCash > 0
              ? ` · ${currency(position.assets.uninvestedInvestmentCash)} sin invertir` : ''
          }`}
          icon={<LineChart className="h-4 w-4" />}
        />
        <PositionItem label="Saldo a favor en tarjetas" value={position.assets.creditCardCredits} />
        <PositionItem label="Deudas externas" value={-position.liabilities.debts} />
        <PositionItem label="Tarjetas por pagar" value={-position.liabilities.creditCards} />
      </div>

      <div className="mt-5 rounded-2xl bg-[#F3F1EC]/70 px-4 py-3 text-sm text-[#5F5A55]">
        Metas asignadas: <strong>{currency(position.savingsGoalAllocations)}</strong>.
        <span className="ml-1 text-xs text-[#7C756E]">
          Son una distribución del ahorro existente y no se suman otra vez al patrimonio.
        </span>
        {(position.assets.accountOpeningBalances !== 0 || position.assets.accountAdjustments !== 0) && (
          <p className="mt-2 border-t border-[#DED9D1] pt-2 text-xs text-[#7C756E]">
            Aperturas incluidas: <strong>{currency(position.assets.accountOpeningBalances)}</strong>
            <span className="mx-1">·</span>
            Ajustes explícitos incluidos: <strong>{currency(position.assets.accountAdjustments)}</strong>
          </p>
        )}
      </div>

      {actionableWarnings.length > 0 && (
        <div className="mt-4 space-y-2 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3">
          {actionableWarnings.map((warning) => (
            <div key={warning.code} className="flex gap-2 text-xs text-amber-900">
              <AlertTriangle className="mt-0.5 h-3.5 w-3.5 shrink-0" />
              <span>{warning.message}</span>
            </div>
          ))}
        </div>
      )}
    </Card>
  );
}

function PositionItem({
  label,
  value,
  detail,
  icon,
}: {
  label: string;
  value: number;
  detail?: string;
  icon?: React.ReactNode;
}) {
  return (
    <div className="rounded-2xl border border-[#EFEAE2] bg-white/70 p-4">
      <div className="flex items-center gap-2 text-xs text-[#7C756E]">
        {icon}
        <span>{label}</span>
      </div>
      <p className={`mt-2 text-lg font-semibold ${value < 0 ? 'text-[#C97B63]' : 'text-[#2C2A29]'}`}>
        {currency(value)}
      </p>
      {detail && <p className="mt-1 text-xs text-[#7C756E]">{detail}</p>}
    </div>
  );
}
