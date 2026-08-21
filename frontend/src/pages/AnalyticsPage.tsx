import { useState } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useAnalytics } from '../features/analytics/hooks/useAnalytics';
import { AnalyticsSectionShell } from '../features/analytics/components/AnalyticsSectionShell';
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
        className="rounded-xl p-2 text-[#7C756E] transition-colors hover:bg-[#F3F1EC] hover:text-[#2C2A29]"
        aria-label="Mes anterior"
      >
        <ChevronLeft className="h-4 w-4" strokeWidth={2} />
      </button>
      <span className="min-w-[150px] text-center text-sm font-medium text-[#2C2A29]">
        {MESES[month - 1]} {year}
      </span>
      <button
        type="button"
        onClick={next}
        disabled={isCurrent}
        className="rounded-xl p-2 text-[#7C756E] transition-colors hover:bg-[#F3F1EC] hover:text-[#2C2A29] disabled:cursor-not-allowed disabled:opacity-30 disabled:hover:bg-transparent"
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

  return (
    <div className="space-y-6 bg-[#FBF9F4] p-6">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="font-serif text-2xl font-medium text-[#2C2A29]">Analytics</h1>
          <p className="mt-0.5 text-sm text-[#7C756E]">
            Inteligencia financiera derivada de tu ledger
          </p>
        </div>
        <MonthYearPicker month={month} year={year} onChange={(m, y) => { setMonth(m); setYear(y); }} />
      </div>

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
              <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-[#7C756E]">
                Desglose por componente
              </p>
              <HealthScoreBreakdown components={healthScore.data.components} />
            </div>
          </div>
        )}
        {healthScore.isError && (
          <p className="py-4 text-center text-xs text-[#C97B63]">No se pudo calcular el score.</p>
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
            <p className="py-4 text-center text-xs text-[#C97B63]">No hay snapshots disponibles.</p>
          )}
        </AnalyticsSectionShell>

        <AnalyticsSectionShell
          title="Comparativa Año vs. Año"
          subtitle={`${year - 1} → ${year}`}
          isLoading={yoy.isLoading}
        >
          {yoy.data && <YearOverYearTable data={yoy.data} />}
          {yoy.isError && (
            <p className="py-4 text-center text-xs text-[#C97B63]">Sin datos históricos YoY.</p>
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
          <p className="py-4 text-center text-xs text-[#C97B63]">No se pudo cargar la inteligencia de gastos.</p>
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
            <p className="py-4 text-center text-xs text-[#C97B63]">No se pudo calcular la proyección.</p>
          )}
        </AnalyticsSectionShell>

        <AnalyticsSectionShell
          title="ETA de Metas de Ahorro"
          subtitle="Estimado por ritmo de aportes"
          isLoading={savingsGoalEta.isLoading}
        >
          {savingsGoalEta.data && <SavingsGoalEtaList goals={savingsGoalEta.data} />}
          {savingsGoalEta.isError && (
            <p className="py-4 text-center text-xs text-[#C97B63]">No se pudo cargar las metas.</p>
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
          <p className="py-4 text-center text-xs text-[#C97B63]">Sin períodos presupuestados.</p>
        )}
      </AnalyticsSectionShell>
    </div>
  );
}
