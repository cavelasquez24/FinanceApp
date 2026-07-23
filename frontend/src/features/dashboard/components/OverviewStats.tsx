// src/features/dashboard/components/OverviewStats.tsx
import { TrendingUp, TrendingDown, Wallet, PiggyBank, LineChart as LineChartIcon, CreditCard } from 'lucide-react';
import { Card } from '../../../components/ui';
import type { DashboardOverview } from '../../../types/dashboard.types';

interface Props {
  data: DashboardOverview;
}

const currency = (value: number) =>
  new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(value);

function ChangeBadge({ value, invert = false }: { value: number; invert?: boolean }) {
  const isPositive = invert ? value <= 0 : value >= 0;
  const Icon = value >= 0 ? TrendingUp : TrendingDown;
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ${
        isPositive ? 'bg-[#8FA888]/15 text-[#5F7A58]' : 'bg-[#C97B63]/15 text-[#C97B63]'
      }`}
    >
      <Icon className="h-3 w-3" strokeWidth={2.5} />
      {Math.abs(value).toFixed(1)}%
    </span>
  );
}

interface KpiProps {
  label: string;
  value: string;
  change?: number;
  changeInvert?: boolean;
  icon: React.ReactNode;
  accent: string;
}

function Kpi({ label, value, change, changeInvert, icon, accent }: KpiProps) {
  return (
    <Card className="!rounded-[28px]">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-xs font-medium uppercase tracking-wide text-[#7C756E]">{label}</p>
          <p className="mt-2 font-serif text-2xl font-medium text-[#2C2A29]">{value}</p>
          {change !== undefined && (
            <div className="mt-2">
              <ChangeBadge value={change} invert={changeInvert} />
            </div>
          )}
        </div>
        <div
          className="flex h-10 w-10 items-center justify-center rounded-2xl"
          style={{ backgroundColor: `${accent}1A`, color: accent }}
        >
          {icon}
        </div>
      </div>
    </Card>
  );
}

// v2.1 — Patrimonio neto y Flujo Residual salieron de acá: viven ahora en
// DashboardHeroRow como los dos KPI protagonistas. Esto cubre flujo
// mensual (nivel 2) y ratios/contexto (nivel 3). El desglose de deuda por
// tipo (consumo vs largo plazo) se retiró: pertenece al módulo Deudas,
// donde el usuario puede actuar sobre esa composición — no se toca acá.
export function OverviewStats({ data }: Props) {
  return (
    <div className="space-y-4">
      {/* Flujo mensual */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Kpi
          label="Ingresos del mes"
          value={currency(data.totalIncome)}
          change={data.changes.incomeChange}
          icon={<Wallet className="h-5 w-5" strokeWidth={2} />}
          accent="#5C7A99"
        />
        <Kpi
          label="Gastos del mes"
          value={currency(data.totalExpenses)}
          change={data.changes.expensesChange}
          icon={<TrendingDown className="h-5 w-5" strokeWidth={2} />}
          accent="#C97B63"
        />
        <Kpi
          label="Ahorro en metas"
          value={currency(data.totalSavingsGoals)}
          icon={<PiggyBank className="h-5 w-5" strokeWidth={2} />}
          accent="#8FA888"
        />
        <Kpi
          label="Deuda pendiente"
          value={currency(data.totalDebt)}
          icon={<CreditCard className="h-5 w-5" strokeWidth={2} />}
          accent="#D9A46B"
        />
      </div>

      {/* Ratios y contexto */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <Card className="!rounded-[28px]">
          <p className="text-xs font-medium uppercase tracking-wide text-[#7C756E]">Disponible / ingreso</p>
          <p className="mt-2 font-serif text-xl font-medium text-[#2C2A29]">
            {data.savingsRate.toFixed(1)}%
          </p>
        </Card>
        <Card className="!rounded-[28px]">
          <div className="flex items-center gap-2">
            <LineChartIcon className="h-4 w-4 text-[#5C7A99]" strokeWidth={2} />
            <p className="text-xs font-medium uppercase tracking-wide text-[#7C756E]">Inversiones</p>
          </div>
          <p className="mt-2 font-serif text-xl font-medium text-[#2C2A29]">
            {currency(data.totalInvestments)}
          </p>
        </Card>
        <Card className="!rounded-[28px]">
          <div className="flex items-center gap-2">
            <CreditCard className="h-4 w-4 text-[#D9A46B]" strokeWidth={2} />
            <p className="text-xs font-medium uppercase tracking-wide text-[#7C756E]">Pagos de deuda</p>
          </div>
          <p className="mt-2 font-serif text-xl font-medium text-[#2C2A29]">
            {currency(data.totalDebtPayments)}
          </p>
          {data.changes.debtPaymentsChange !== undefined && (
            <div className="mt-1">
              <ChangeBadge value={data.changes.debtPaymentsChange} invert />
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}