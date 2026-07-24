import { useState, useEffect } from 'react';
import { AlertCircle, CalendarClock } from 'lucide-react';
import { Card, CardHeader, Spinner } from '../components/ui';
import { FinancialChart } from '../components/dashboard/FinancialChart';
import { CashFlowStatementCard } from '../components/dashboard/CashFlowStatementCard';
import { MonthYearSelector } from '../features/dashboard/components/MonthYearSelector';
import { OverviewStats } from '../features/dashboard/components/OverviewStats';
import { DashboardHeroRow } from '../features/dashboard/components/DashboardHeroRow';
import { SavingsGoalsMiniWidget } from '../features/dashboard/components/SavingsGoalsMiniWidget';
import { useProfile } from '../features/profile/hooks/useProfile';
import { getCycleRange, formatCycleLabel } from '../utils/cycleUtils';
import {
  useDashboardOverview,
  useDashboardTrend,
  useDashboardCashFlow,
} from '../features/dashboard/hooks/useDashboard';

// v2.1 — ExpensesByCategoryChart se movió a ExpensesPage.tsx (era vista
// de análisis, no de resumen ejecutivo). NetWorthTrendChart sigue sin
// conectar, depende de endpoint pendiente en backend — no se simula.

export function DashboardPage() {
  const today = new Date();
  const [month, setMonth] = useState(today.getMonth() + 1);
  const [year, setYear] = useState(today.getFullYear());
  const [adjustedForPayday, setAdjustedForPayday] = useState(false);

  const { data: profile } = useProfile();

  useEffect(() => {
    if (adjustedForPayday || profile?.paydayDay == null) return;

    if (today.getDate() < profile.paydayDay) {
      setMonth((m) => (m === 1 ? 12 : m - 1));
      setYear((y) => (month === 1 ? y - 1 : y));
    }
    setAdjustedForPayday(true);
  }, [profile, adjustedForPayday]);

  const { data: overview, isLoading: isLoadingOverview, isError: isErrorOverview } =
    useDashboardOverview(month, year);
  const { data: trendData, isLoading: isLoadingTrend, isError: isErrorTrend } =
    useDashboardTrend(12);
  const { data: cashFlowData, isLoading: isLoadingCashFlow, isError: isErrorCashFlow } =
    useDashboardCashFlow(month, year);

  const cycleRange = getCycleRange(month, year, profile?.paydayDay);
  const cycleLabel = cycleRange ? formatCycleLabel(cycleRange) : null;

  const handleMonthChange = (newMonth: number, newYear: number) => {
    setMonth(newMonth);
    setYear(newYear);
  };

  return (
    <div className="space-y-6 bg-[#FBF9F4] p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="font-serif text-2xl font-medium text-[#2C2A29]">An?lisis financiero</h1>
          <p className="text-sm text-[#7C756E]">Reportes y tendencias por ciclo.</p>
          {cycleLabel && (
            <span className="mt-2 inline-flex w-fit items-center gap-1.5 rounded-full border border-[#EFEAE2] bg-white/70 px-3 py-1 text-xs font-medium text-[#2C2A29] backdrop-blur-sm">
              <CalendarClock className="h-3.5 w-3.5 text-[#5C7A99]" strokeWidth={2} aria-hidden="true" />
              Tu ciclo: {cycleLabel}
            </span>
          )}
        </div>
        <MonthYearSelector month={month} year={year} onChange={handleMonthChange} paydayDay={profile?.paydayDay} />

      </div>

      {isLoadingOverview ? (
        <div className="flex flex-col items-center gap-3 p-12 text-[#7C756E]">
          <Spinner />
          <span className="text-sm">Cargando métricas...</span>
        </div>
      ) : isErrorOverview ? (
        <div className="flex flex-col items-center gap-2 p-12 text-center text-[#C97B63]">
          <AlertCircle className="h-6 w-6" strokeWidth={2} />
          <span className="text-sm font-medium">Error al cargar el resumen.</span>
        </div>
      ) : overview ? (
        <>
          <DashboardHeroRow
            netWorth={overview.netWorth}
            cashFlowResidual={isErrorCashFlow ? null : cashFlowData?.cashFlowResidual ?? null}
          />
          <OverviewStats data={overview} />
        </>
      ) : null}

      <Card className="!rounded-[28px]">
        <CardHeader
          title="Distribución del ingreso"
          subtitle="Gastos, ahorro, inversión, deuda y disponible del ciclo seleccionado"
        />
        {isLoadingCashFlow ? (
          <div className="flex flex-col items-center gap-3 p-12 text-[#7C756E]">
            <Spinner />
            <span className="text-sm">Cargando flujo de caja...</span>
          </div>
        ) : isErrorCashFlow ? (
          <div className="flex flex-col items-center gap-2 p-12 text-center text-[#C97B63]">
            <AlertCircle className="h-6 w-6" strokeWidth={2} />
            <span className="text-sm font-medium">Error al cargar el flujo de caja.</span>
          </div>
        ) : cashFlowData ? (
          <CashFlowStatementCard data={cashFlowData} />
        ) : null}
      </Card>
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2 !rounded-[28px]">
          <CardHeader
            title="Tendencia del disponible real"
            subtitle="Ingreso, gasto y caja restante después de todas las asignaciones (últimos 12 ciclos)"
          />
          {isLoadingTrend ? (
            <div className="flex flex-col items-center gap-3 p-12 text-[#7C756E]">
              <Spinner />
              <span className="text-sm">Cargando tendencia...</span>
            </div>
          ) : isErrorTrend ? (
            <div className="flex flex-col items-center gap-2 p-12 text-center text-[#C97B63]">
              <AlertCircle className="h-6 w-6" strokeWidth={2} />
              <span className="text-sm font-medium">Error al cargar la tendencia.</span>
            </div>
          ) : trendData ? (
            <FinancialChart data={trendData} />
          ) : null}
        </Card>

        <SavingsGoalsMiniWidget />
      </div>

    </div>
  );
}