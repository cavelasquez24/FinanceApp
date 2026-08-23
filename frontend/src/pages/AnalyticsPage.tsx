import { useState } from 'react';
import { ChevronLeft, ChevronRight, CreditCard, PiggyBank, TrendingUp, Wallet } from 'lucide-react';
import { useAnalytics } from '../features/analytics/hooks/useAnalytics';
import { useCurrentDashboard } from '../features/dashboard/hooks/useCurrentDashboard';
import { AnalyticsSectionShell } from '../features/analytics/components/AnalyticsSectionShell';
import { Card } from '../components/ui';
import { formatCurrency } from '../utils/formatCurrency';
import { FinancialHealthScore } from '../features/analytics/components/FinancialHealthScore';
import { HealthScoreBreakdown } from '../features/analytics/components/HealthScoreBreakdown';
import { NetWorthChart } from '../features/analytics/components/NetWorthChart';
import { ExpenseIntelligencePanel } from '../features/analytics/components/ExpenseIntelligencePanel';
import { DebtProjectionPanel } from '../features/analytics/components/DebtProjectionPanel';
import { SavingsGoalEtaList } from '../features/analytics/components/SavingsGoalEtaList';
import { YearOverYearTable } from '../features/analytics/components/YearOverYearTable';
import { BudgetVsActualChart } from '../features/analytics/components/BudgetVsActualChart';

const MESES = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre',
];

function MonthYearPicker({
  month, year, onChange,
}: {
  month: number;
  year: number;
  onChange: (m: number, y: number) => void;
}) {
  const today = new Date();
  const isCurrent = month === today.getMonth() + 1 && year === today.getFullYear();

  const prev = () => month === 1 ? onChange(12, year - 1) : onChange(month - 1, year);
  const next = () => month === 12 ? onChange(1, year + 1) : onChange(month + 1, year);

  return (
    <div className="flex items-center gap-1 rounded-2xl border border-[#EFEAE2] bg-white/70 p-1 backdrop-blur-sm">
      <button
        type="button"
        onClick={prev}
        className="rounded-xl p-2 text-finflow-muted transition-colors hover:bg-[#F3F1EC] hover:text-finflow-dark"
        aria-label="Mes anterior"
      >
        <ChevronLeft className="h-4 w-4" strokeWidth={2} />
      </button>
      <span className="min-w-[150px] text-center text-sm font-medium text-finflow-dark">
        {MESES[month - 1]} {year}
      </span>
      <button
        type="button"
        onClick={next}
        disabled={isCurrent}
        className="rounded-xl p-2 text-finflow-muted transition-colors hover:bg-[#F3F1EC] hover:text-finflow-dark disabled:cursor-not-allowed disabled:opacity-30 disabled:hover:bg-transparent"
        aria-label="Mes siguiente"
      >
        <ChevronRight className="h-4 w-4" strokeWidth={2} />
      </button>
    </div>
  );
}

export function AnalyticsPage() {
  const today = new Date();
  const [month, setMonth] = useState(today.getMonth() + 1);
  const [year, setYear] = useState(today.getFullYear());

  const {
    healthScore,
    netWorthTimeline,
    expenseIntelligence,
    debtProjection,
    savingsGoalEta,
    yoy,
    budgetVsActual,
  } = useAnalytics(month, year);

  // Movido desde CurrentDashboardPage — es análisis retrospectivo del ciclo
  // (breakdown de porcentajes), no acción diaria. Reutiliza el mismo hook
  // /api/currentdashboard que ya consumía /inicio, en paralelo a useAnalytics.
  const currentDashboard = useCurrentDashboard();

  return (
    <div className="space-y-6 bg-finflow-cream p-6">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="font-serif text-2xl font-medium text-finflow-dark">Analytics</h1>
          <p className="mt-0.5 text-sm text-finflow-muted">
            Inteligencia financiera derivada de tu ledger
          </p>
        </div>
        <MonthYearPicker month={month} year={year} onChange={(m, y) => { setMonth(m); setYear(y); }} />
      </div>

      {/* Métricas del ciclo actual — movido desde /inicio */}
      <AnalyticsSectionShell
        title="Métricas del ciclo actual"
        subtitle={currentDashboard.data?.cycleLabel}
        isLoading={currentDashboard.isLoading}
      >
        {currentDashboard.data && (
          <>
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              {(
                [
                  { label: 'Flujo de caja del ciclo', value: currentDashboard.data.cycleCashFlow, icon: Wallet, color: '#5C7A99' },
                  { label: 'Ingreso del ciclo', value: currentDashboard.data.cycleIncome, icon: TrendingUp, badgeClassName: 'bg-finflow-green/10 text-finflow-green' },
                  { label: 'Gasto bruto del ciclo', value: currentDashboard.data.cycleExpenses, icon: Wallet, color: '#C97B63' },
                  { label: 'Reembolsos recibidos', value: currentDashboard.data.cycleReimbursements, icon: Wallet, color: '#5C7A99' },
                  { label: 'Gasto neto personal', value: currentDashboard.data.cycleNetExpenses, icon: Wallet, color: '#2C2A29' },
                  { label: 'Saldo en cuentas de ahorro', value: currentDashboard.data.savingsBalance, icon: PiggyBank, color: '#8FA888' },
                  { label: 'Inversiones', value: currentDashboard.data.investmentBalance, icon: TrendingUp, color: '#5C7A99' },
                  { label: 'Deuda pendiente', value: currentDashboard.data.debtBalance, icon: CreditCard, badgeClassName: 'bg-finflow-amber/10 text-finflow-amber' },
                ] satisfies {
                  label: string;
                  value: number;
                  icon: typeof Wallet;
                  color?: string;
                  badgeClassName?: string;
                }[]
              ).map(({ label, value, icon: Icon, color, badgeClassName }) => (
                <Card key={label} className="!rounded-[22px] !p-5">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-finflow-muted">{label}</span>
                    <span
                      className={`rounded-xl p-2 ${badgeClassName ?? ''}`}
                      style={badgeClassName ? undefined : { backgroundColor: `${color}18`, color }}
                    >
                      <Icon className="h-4 w-4" />
                    </span>
                  </div>
                  <p className="mt-4 text-xl font-semibold text-finflow-dark">{formatCurrency(value)}</p>
                </Card>
              ))}
            </div>

            <div className="mt-6">
              <h3 className="mb-3 text-xs font-semibold uppercase tracking-wide text-finflow-muted">
                Asignación del ingreso
              </h3>
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                <CycleItem label="Gasto bruto" value={currentDashboard.data.cycleExpenses} />
                <CycleItem label="Reembolsos" value={-currentDashboard.data.cycleReimbursements} />
                <CycleItem label="Consumo neto" value={currentDashboard.data.cycleNetExpenses} />
                <CycleItem label="Ahorro" value={currentDashboard.data.cycleSavings} inDevelopment />
                <CycleItem label="Inversión" value={currentDashboard.data.cycleInvestments} />
                <CycleItem label="Pagos de deuda" value={currentDashboard.data.cycleDebtPayments} />
              </div>
            </div>
          </>
        )}
        {currentDashboard.isError && (
          <p className="py-4 text-center text-xs text-finflow-rust">
            No se pudo cargar el resumen del ciclo actual.
          </p>
        )}
      </AnalyticsSectionShell>

      {/* Score de salud financiera */}
      <AnalyticsSectionShell
        title="Score de Salud Financiera"
        subtitle={`${MESES[month - 1]} ${year}`}
        isLoading={healthScore.isLoading}
      >
        {healthScore.data && (
          <div className="space-y-6">
            <FinancialHealthScore data={healthScore.data} />
            <div className="border-t border-[#EFEAE2] pt-4">
              <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-finflow-muted">
                Desglose por componente
              </p>
              <HealthScoreBreakdown components={healthScore.data.components} />
            </div>
          </div>
        )}
        {healthScore.isError && (
          <p className="py-4 text-center text-xs text-finflow-rust">No se pudo calcular el score.</p>
        )}
      </AnalyticsSectionShell>

      {/* Patrimonio + YoY — dos columnas */}
      <div className="grid gap-6 lg:grid-cols-[1.4fr_1fr]">
        <AnalyticsSectionShell
          title="Patrimonio Neto"
          subtitle="Últimos 12 meses"
          isLoading={netWorthTimeline.isLoading}
        >
          {netWorthTimeline.data && <NetWorthChart data={netWorthTimeline.data} />}
          {netWorthTimeline.isError && (
            <p className="py-4 text-center text-xs text-finflow-rust">No hay snapshots disponibles.</p>
          )}
        </AnalyticsSectionShell>

        <AnalyticsSectionShell
          title="Comparativa Año vs. Año"
          subtitle={`${year - 1} → ${year}`}
          isLoading={yoy.isLoading}
        >
          {yoy.data && <YearOverYearTable data={yoy.data} />}
          {yoy.isError && (
            <p className="py-4 text-center text-xs text-finflow-rust">Sin datos históricos YoY.</p>
          )}
        </AnalyticsSectionShell>
      </div>

      {/* Inteligencia de gastos */}
      <AnalyticsSectionShell
        title="Inteligencia de Gastos"
        subtitle={`${MESES[month - 1]} ${year}`}
        isLoading={expenseIntelligence.isLoading}
      >
        {expenseIntelligence.data && (
          <ExpenseIntelligencePanel data={expenseIntelligence.data} />
        )}
        {expenseIntelligence.isError && (
          <p className="py-4 text-center text-xs text-finflow-rust">No se pudo cargar la inteligencia de gastos.</p>
        )}
      </AnalyticsSectionShell>

      {/* Proyección deudas + ETA metas — dos columnas */}
      <div className="grid gap-6 lg:grid-cols-2">
        <AnalyticsSectionShell
          title="Proyección de Deudas"
          subtitle="Basado en promedio últimos 3 meses"
          isLoading={debtProjection.isLoading}
        >
          {debtProjection.data && <DebtProjectionPanel data={debtProjection.data} />}
          {debtProjection.isError && (
            <p className="py-4 text-center text-xs text-finflow-rust">No se pudo calcular la proyección.</p>
          )}
        </AnalyticsSectionShell>

        <AnalyticsSectionShell
          title="ETA de Metas de Ahorro"
          subtitle="Estimado por ritmo de aportes"
          isLoading={savingsGoalEta.isLoading}
        >
          {savingsGoalEta.data && <SavingsGoalEtaList goals={savingsGoalEta.data} />}
          {savingsGoalEta.isError && (
            <p className="py-4 text-center text-xs text-finflow-rust">No se pudo cargar las metas.</p>
          )}
        </AnalyticsSectionShell>
      </div>

      {/* Presupuesto vs Real */}
      <AnalyticsSectionShell
        title="Presupuesto vs. Real"
        subtitle="Últimos 6 meses"
        isLoading={budgetVsActual.isLoading}
      >
        {budgetVsActual.data && <BudgetVsActualChart data={budgetVsActual.data} />}
        {budgetVsActual.isError && (
          <p className="py-4 text-center text-xs text-finflow-rust">Sin períodos presupuestados.</p>
        )}
      </AnalyticsSectionShell>
    </div>
  );
}

// Movido desde CurrentDashboardPage junto con la sección "Asignación del ingreso".
function CycleItem({
  label,
  value,
  inDevelopment = false,
}: {
  label: string;
  value: number;
  inDevelopment?: boolean;
}) {
  return (
    <div className="rounded-2xl bg-[#F3F1EC]/70 p-4">
      <p className="text-xs text-finflow-muted">{label}</p>
      <p
        className={`mt-1 text-lg font-semibold text-finflow-dark ${inDevelopment ? 'opacity-50' : ''}`}
        title={inDevelopment ? 'En desarrollo — todavía no se calcula por ciclo' : undefined}
      >
        {formatCurrency(value)}
      </p>
      {inDevelopment && <p className="mt-0.5 text-[10px] text-finflow-muted">En desarrollo</p>}
    </div>
  );
}
