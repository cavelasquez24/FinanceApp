// src/components/dashboard/CashFlowStatementCard.tsx
import { PiggyBank, LineChart as LineChartIcon, CreditCard, Receipt, Wallet } from 'lucide-react';
import type { CashFlowStatement } from '../../types/dashboard.types';

interface Props {
  data: CashFlowStatement;
}

const currency = (value: number) =>
  new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(value);

interface RowProps {
  label: string;
  value: number;
  total: number;
  color: string;
  icon: React.ReactNode;
}

function ContributionRow({ label, value, total, color, icon }: RowProps) {
  const pct = total > 0 ? Math.min(100, (Math.abs(value) / total) * 100) : 0;
  return (
    <div className="flex items-center gap-3">
      <div
        className="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl"
        style={{ backgroundColor: `${color}1A`, color }}
      >
        {icon}
      </div>
      <div className="min-w-0 flex-1">
        <div className="flex items-center justify-between text-sm">
          <span className="text-[#2C2A29]">{label}</span>
          <span className="text-right font-medium text-[#2C2A29]">
            {currency(value)}
            <span className="ml-2 text-xs font-normal text-[#7C756E]">{pct.toFixed(1)}%</span>
          </span>
        </div>
        <div className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-[#EFEAE2]">
          <div
            className="h-full rounded-full transition-all duration-500"
            style={{ width: `${pct}%`, backgroundColor: color }}
          />
        </div>
      </div>
    </div>
  );
}

// v2.1 — versión compacta. El bloque "Flujo Residual" que tenía antes
// este componente se retiró: esa métrica ya vive en DashboardHeroRow
// como KPI protagonista — mostrarla dos veces era la redundancia
// principal detectada en el análisis UX.
export function CashFlowStatementCard({ data }: Props) {
  const netSavings = data.savingsContributions - data.savingsWithdrawals;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <span className="text-xs font-medium uppercase tracking-wide text-[#7C756E]">
          Ingreso del ciclo
        </span>
        <span className="font-serif text-xl font-medium text-[#2C2A29]">
          {currency(data.income)}
        </span>
      </div>

      <div className="space-y-3">
        <ContributionRow
          label="Gastos"
          value={data.consumptionExpenses}
          total={data.income}
          color="#C97B63"
          icon={<Receipt className="h-4 w-4" strokeWidth={2} />}
        />
        <ContributionRow
          label="Ahorro neto"
          value={netSavings}
          total={data.income}
          color="#8FA888"
          icon={<PiggyBank className="h-4 w-4" strokeWidth={2} />}
        />
        <ContributionRow
          label="Inversión"
          value={data.investmentContributions}
          total={data.income}
          color="#5C7A99"
          icon={<LineChartIcon className="h-4 w-4" strokeWidth={2} />}
        />
        <ContributionRow
          label="Capital de deuda pagado"
          value={data.debtPrincipalPaid}
          total={data.income}
          color="#D9A46B"
          icon={<CreditCard className="h-4 w-4" strokeWidth={2} />}
        />
        <ContributionRow
          label="Disponible"
          value={data.cashFlowResidual}
          total={data.income}
          color="#2C2A29"
          icon={<Wallet className="h-4 w-4" strokeWidth={2} />}
        />

      </div>
      {data.savingsWithdrawals > 0 && (
        <p className="text-xs text-[#7C756E]">Incluye {currency(data.savingsWithdrawals)} liberados desde metas de ahorro.</p>
      )}
      <p className="text-xs text-[#7C756E]">
        Tasa de construcción patrimonial: {data.wealthBuildingRate.toFixed(1)}% del ingreso
      </p>
    </div>
  );
}