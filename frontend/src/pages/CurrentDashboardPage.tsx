import { Link } from 'react-router-dom';
import { formatDistanceToNow, format } from 'date-fns';
import { es } from 'date-fns/locale';
import {
  AlertCircle,
  CalendarClock,
  CreditCard,
  PiggyBank,
  TrendingUp,
} from 'lucide-react';
import { Card, PageHeader, Spinner } from '../components/ui';
import { useCurrentDashboard } from '../features/dashboard/hooks/useCurrentDashboard';
import { useExpenses } from '../features/expenses/hooks/useExpenses';
import { useDebtSummary } from '../features/debts/hooks/useDebts';
import { FinancialPositionSummary } from '../features/dashboard/components/FinancialPositionSummary';
import { parseDateOnly } from '../utils/dateOnly';

const money = (value: number) =>
  new Intl.NumberFormat('es-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(value);

// Próximo día del mes en que cae un `dueDay` recurrente (1-31), ajustado a
// meses cortos. dueDay no es una fecha absoluta — es el día de pago fijo de
// la deuda (DebtSummaryDto.upcomingPayments) — por eso se deriva la próxima
// ocurrencia real a partir de la fecha de hoy.
function nextOccurrence(dueDay: number, today: Date): Date {
  const daysInCurrentMonth = new Date(today.getFullYear(), today.getMonth() + 1, 0).getDate();
  const thisMonthDay = Math.min(dueDay, daysInCurrentMonth);
  const candidate = new Date(today.getFullYear(), today.getMonth(), thisMonthDay);
  if (candidate >= today) return candidate;

  const nextMonth = today.getMonth() === 11 ? 0 : today.getMonth() + 1;
  const nextYear = today.getMonth() === 11 ? today.getFullYear() + 1 : today.getFullYear();
  const daysInNextMonth = new Date(nextYear, nextMonth + 1, 0).getDate();
  return new Date(nextYear, nextMonth, Math.min(dueDay, daysInNextMonth));
}

export function CurrentDashboardPage() {
  const { data, isLoading, isError } = useCurrentDashboard();

  // Último gasto registrado — reutiliza /api/expenses con paginación de 1 y
  // el orden por defecto del backend (SortBy=date, SortDirection=desc), sin
  // necesidad de un endpoint nuevo.
  const { data: lastExpenseResponse, isLoading: isLoadingLastExpense } = useExpenses({
    page: 1,
    pageSize: 1,
    sortBy: 'date',
    sortDirection: 'desc',
  });
  const lastExpense = lastExpenseResponse?.data?.data?.items?.[0];

  // Próximos vencimientos — DebtSummaryDto.upcomingPayments ya existe
  // (GET /api/debts/summary) con dueDay (día de pago recurrente). Se deriva
  // la próxima fecha real y se muestran las 2 más cercanas.
  const { data: debtSummary } = useDebtSummary();
  const upcomingDebts = (debtSummary?.upcomingPayments ?? [])
    .map((payment) => ({ ...payment, nextDate: nextOccurrence(payment.dueDay, new Date()) }))
    .sort((a, b) => a.nextDate.getTime() - b.nextDate.getTime())
    .slice(0, 2);

  if (isLoading) {
    return (
      <div className="flex min-h-[420px] items-center justify-center">
        <Spinner />
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="flex min-h-[420px] flex-col items-center justify-center gap-2 text-finflow-rust">
        <AlertCircle className="h-6 w-6" />
        <p className="text-sm font-medium">No se pudo cargar tu resumen actual.</p>
      </div>
    );
  }

  const hasBudget = data.percentageUsed != null;
  const used = hasBudget ? Math.min(1, Math.max(0, data.percentageUsed! / 100)) : 0;

  return (
    <div className="space-y-6 bg-finflow-cream">
      <PageHeader
        eyebrow="Ciclo actual"
        title="Inicio"
        action={
          <div className="inline-flex items-center gap-1.5 rounded-full border border-[#EFEAE2] bg-white/70 px-3 py-1 text-xs text-finflow-blue">
            <CalendarClock className="h-3.5 w-3.5" />
            Ciclo actual: {data.cycleLabel}
          </div>
        }
      />

      {/* Estado del ciclo — ingreso vs. gasto neto, las dos cifras de "hoy" */}
      <Card className="!rounded-card !p-7 !bg-finflow-cream dark:!bg-finflow-dark">
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
          <div className="flex items-start gap-3">
            <span className="rounded-xl bg-finflow-green/10 p-2 text-finflow-green">
              <TrendingUp className="h-5 w-5" />
            </span>
            <div>
              <p className="text-sm text-finflow-muted">Ingreso del ciclo</p>
              <p className="mt-1 text-3xl font-semibold text-finflow-green">
                {money(data.cycleIncome)}
              </p>
            </div>
          </div>
          <div className="flex items-start gap-3">
            <span
              className={`rounded-xl p-2 ${
                data.cycleNetExpenses > 0
                  ? 'bg-finflow-rust/10 text-finflow-rust'
                  : 'bg-finflow-muted/10 text-finflow-muted'
              }`}
            >
              <CreditCard className="h-5 w-5" />
            </span>
            <div>
              <p className="text-sm text-finflow-muted">Gasto neto</p>
              <p
                className={`mt-1 text-3xl font-semibold ${
                  data.cycleNetExpenses > 0 ? 'text-finflow-rust' : 'text-finflow-dark'
                }`}
              >
                {money(data.cycleNetExpenses)}
              </p>
            </div>
          </div>
        </div>
      </Card>

      {/* Barra de presupuesto — sin cambios respecto al sprint anterior */}
      <Card className="!rounded-[28px] !bg-finflow-dark !p-7 text-white">
        <p className="text-sm text-white/65">Presupuesto disponible</p>
        <p className={`mt-2 text-4xl font-semibold ${data.cycleAvailable < 0 ? 'text-[#E7A38E]' : ''}`}>
          {money(data.budgetAvailable)}
        </p>
        <p className="mt-2 text-sm text-white/65">
          autorizado para gastar en este ciclo; no es el saldo de tus cuentas
        </p>

        {hasBudget ? (
          <div className="mt-6 h-2.5 overflow-hidden rounded-full bg-white/15">
            <div
              className="h-full rounded-full bg-[#AFC1A8] transition-all"
              style={{ width: `${used * 100}%` }}
            />
          </div>
        ) : (
          <div className="mt-6 flex items-center gap-3 rounded-2xl bg-white/10 p-4">
            <PiggyBank className="h-5 w-5 shrink-0 text-white/70" />
            <div className="flex-1">
              <p className="text-sm text-white/85">No tienes un presupuesto configurado</p>
              <Link to="/budget" className="text-xs font-medium text-white underline underline-offset-2">
                Configurar presupuesto
              </Link>
            </div>
          </div>
        )}

        <div className="mt-6 grid grid-cols-2 gap-4 border-t border-white/10 pt-5">
          <div>
            <p className="text-xs text-white/55">Sugerencia diaria</p>
            <p className="mt-1 text-xl font-medium">{money(data.suggestedDailyAvailable)}</p>
          </div>
          <div>
            <p className="text-xs text-white/55">Días restantes</p>
            <p className="mt-1 text-xl font-medium">{data.daysRemaining}</p>
          </div>
        </div>
      </Card>

      {/* Último gasto registrado */}
      <Card className="!rounded-card !p-6">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="font-serif text-lg font-medium text-finflow-dark">Último gasto registrado</h2>
          <Link to="/expenses" className="text-xs font-medium text-finflow-blue hover:underline">
            Ver todos
          </Link>
        </div>
        {isLoadingLastExpense ? (
          <div className="flex justify-center py-4">
            <Spinner />
          </div>
        ) : lastExpense ? (
          <div className="flex items-center justify-between gap-3">
            <div className="flex min-w-0 items-center gap-3">
              <span
                className="h-3 w-3 shrink-0 rounded-full"
                style={{ backgroundColor: lastExpense.categoryColor }}
              />
              <div className="min-w-0">
                <p className="truncate text-sm font-medium text-finflow-dark">
                  {lastExpense.description || lastExpense.merchant || lastExpense.categoryName}
                </p>
                <p className="truncate text-xs text-finflow-muted">
                  {lastExpense.categoryName} ·{' '}
                  {formatDistanceToNow(parseDateOnly(lastExpense.date), { addSuffix: true, locale: es })}
                </p>
              </div>
            </div>
            <p className="shrink-0 text-lg font-semibold text-finflow-rust">
              -{money(lastExpense.amount)}
            </p>
          </div>
        ) : (
          <p className="text-sm text-finflow-muted">Sin gastos registrados aún</p>
        )}
      </Card>

      {/* Próximos vencimientos — solo se renderiza si hay deudas con día de pago */}
      {upcomingDebts.length > 0 && (
        <Card className="!rounded-card !p-6">
          <div className="mb-4 flex items-center justify-between">
            <h2 className="font-serif text-lg font-medium text-finflow-dark">Próximos vencimientos</h2>
            <Link to="/debts" className="text-xs font-medium text-finflow-blue hover:underline">
              Ver deudas
            </Link>
          </div>
          <div className="space-y-3">
            {upcomingDebts.map((debt) => (
              <div key={debt.debtId} className="flex items-center justify-between text-sm">
                <span className="text-finflow-dark">{debt.debtName}</span>
                <div className="text-right">
                  {debt.minimumPayment != null && (
                    <p className="font-medium text-finflow-dark">{money(debt.minimumPayment)}</p>
                  )}
                  <p className="text-xs text-finflow-muted">
                    Vence {format(debt.nextDate, 'dd MMM', { locale: es })}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </Card>
      )}

      <Card className="!rounded-[28px] !p-7">
        <p className="text-sm text-finflow-muted">Saldo físico en cuentas</p>
        <p className="mt-2 text-3xl font-semibold text-finflow-dark">{money(data.cashBalance)}</p>
        <p className="mt-3 text-xs leading-relaxed text-finflow-muted">
          Este saldo sí acumula sobrantes de ciclos anteriores. El disponible mensual se renueva
          por separado para mantener tu límite.
        </p>
        <div className="mt-5 border-t border-[#EFEAE2] pt-4">
          <div className="flex items-center justify-between text-sm">
            <span className="text-finflow-muted">Patrimonio neto</span>
            <span className="font-semibold text-finflow-dark">{money(data.netWorth)}</span>
          </div>
        </div>
      </Card>

      <FinancialPositionSummary position={data.financialPosition} />
    </div>
  );
}
