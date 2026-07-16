// src/pages/DashboardPage.tsx
import { useState } from 'react';
import { AlertCircle } from 'lucide-react';
import { Card, CardHeader, Spinner } from '../components/ui';
import { FinancialChart } from '../components/dashboard/FinancialChart';
import { ExpensesByCategoryChart } from '../components/dashboard/ExpensesByCategoryChart';
import { MonthYearSelector } from '../features/dashboard/components/MonthYearSelector';
import { OverviewStats } from '../features/dashboard/components/OverviewStats';
import {
  useDashboardOverview,
  useDashboardTrend,
  useDashboardExpensesByCategory,
} from '../features/dashboard/hooks/useDashboard';

export function DashboardPage() {
  const today = new Date();
  const [month, setMonth] = useState(today.getMonth() + 1);
  const [year, setYear] = useState(today.getFullYear());

  const { data: overview, isLoading: isLoadingOverview, isError: isErrorOverview } =
    useDashboardOverview(month, year);
  const { data: trendData, isLoading: isLoadingTrend, isError: isErrorTrend } =
    useDashboardTrend(12);
  const { data: categoryData, isLoading: isLoadingCategory, isError: isErrorCategory } =
    useDashboardExpensesByCategory(month, year);

  const handleMonthChange = (newMonth: number, newYear: number) => {
    setMonth(newMonth);
    setYear(newYear);
  };

  return (
    <div className="space-y-6 bg-[#FBF9F4] p-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="font-serif text-2xl font-medium text-[#2C2A29]">Dashboard</h1>
          <p className="text-sm text-[#7C756E]">Resumen financiero de tu mes.</p>
        </div>
        <MonthYearSelector month={month} year={year} onChange={handleMonthChange} />
      </div>

      {/* KPIs */}
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
        <OverviewStats data={overview} />
      ) : null}

      {/* Bento: Tendencia (ancha) + Gastos por categoría (angosta) */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2 !rounded-[28px]">
          <CardHeader
            title="Tendencia Financiera"
            subtitle="Evolución mensual de ingresos, gastos y ahorro (últimos 12 meses)"
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

        <Card className="!rounded-[28px]">
          <CardHeader title="Gastos por Categoría" subtitle="Distribución del mes seleccionado" />
          {isLoadingCategory ? (
            <div className="flex flex-col items-center gap-3 p-12 text-[#7C756E]">
              <Spinner />
              <span className="text-sm">Cargando categorías...</span>
            </div>
          ) : isErrorCategory ? (
            <div className="flex flex-col items-center gap-2 p-12 text-center text-[#C97B63]">
              <AlertCircle className="h-6 w-6" strokeWidth={2} />
              <span className="text-sm font-medium">Error al cargar categorías.</span>
            </div>
          ) : categoryData ? (
            <ExpensesByCategoryChart data={categoryData} />
          ) : null}
        </Card>
      </div>
    </div>
  );
}