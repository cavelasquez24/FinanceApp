import {
  AlertCircle,
  BarChart3,
  CalendarClock,
  Landmark,
  PiggyBank,
  Wallet,
} from 'lucide-react';
import { Card, Spinner } from '../components/ui';
import { useCurrentDashboard } from '../features/dashboard/hooks/useCurrentDashboard';

const money = (value: number) =>
  new Intl.NumberFormat('es-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(value);

export function CurrentDashboardPage() {
  const { data, isLoading, isError } = useCurrentDashboard();

  if (isLoading) {
    return (
      <div className="flex min-h-[420px] items-center justify-center">
        <Spinner />
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="flex min-h-[420px] flex-col items-center justify-center gap-2 text-[#C97B63]">
        <AlertCircle className="h-6 w-6" />
        <p className="text-sm font-medium">No se pudo cargar tu resumen actual.</p>
      </div>
    );
  }

  const used = data.cycleIncome > 0
    ? Math.max(0, Math.min(100, ((data.cycleIncome - data.cycleAvailable) / data.cycleIncome) * 100))
    : 0;

  return (
    <div className="space-y-6 bg-[#FBF9F4] p-6">
      <div>
        <h1 className="font-serif text-2xl font-medium text-[#2C2A29]">Tu resumen de hoy</h1>
        <div className="mt-2 inline-flex items-center gap-1.5 rounded-full border border-[#EFEAE2] bg-white/70 px-3 py-1 text-xs text-[#5C7A99]">
          <CalendarClock className="h-3.5 w-3.5" />
          Ciclo actual: {data.cycleLabel}
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-[1.45fr_1fr]">
        <Card className="!rounded-[28px] !bg-[#2C2A29] !p-7 text-white">
          <p className="text-sm text-white/65">Disponible del ciclo</p>
          <p className={`mt-2 text-4xl font-semibold ${data.cycleAvailable < 0 ? 'text-[#E7A38E]' : ''}`}>
            {money(data.cycleAvailable)}
          </p>
          <p className="mt-2 text-sm text-white/65">
            de {money(data.cycleIncome)} recibidos para este ciclo
          </p>

          <div className="mt-6 h-2.5 overflow-hidden rounded-full bg-white/15">
            <div
              className="h-full rounded-full bg-[#AFC1A8] transition-all"
              style={{ width: `${used}%` }}
            />
          </div>

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

        <Card className="!rounded-[28px] !p-7">
          <p className="text-sm text-[#7C756E]">Saldo disponible en cuentas</p>
          <p className="mt-2 text-3xl font-semibold text-[#2C2A29]">{money(data.cashBalance)}</p>
          <p className="mt-3 text-xs leading-relaxed text-[#7C756E]">
            Este saldo sí acumula sobrantes de ciclos anteriores. El disponible mensual se renueva
            por separado para mantener tu límite.
          </p>
          <div className="mt-5 border-t border-[#EFEAE2] pt-4">
            <div className="flex items-center justify-between text-sm">
              <span className="text-[#7C756E]">Patrimonio neto</span>
              <span className="font-semibold text-[#2C2A29]">{money(data.netWorth)}</span>
            </div>
          </div>
        </Card>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {[
          { label: 'Gastos del ciclo', value: data.cycleExpenses, icon: Wallet, color: '#C97B63' },
          { label: 'Ahorro reservado', value: data.savingsBalance, icon: PiggyBank, color: '#8FA888' },
          { label: 'Inversiones', value: data.investmentBalance, icon: BarChart3, color: '#5C7A99' },
          { label: 'Deuda pendiente', value: data.debtBalance, icon: Landmark, color: '#D9A46B' },
        ].map(({ label, value, icon: Icon, color }) => (
          <Card key={label} className="!rounded-[22px] !p-5">
            <div className="flex items-center justify-between">
              <span className="text-sm text-[#7C756E]">{label}</span>
              <span className="rounded-xl p-2" style={{ backgroundColor: `${color}18`, color }}>
                <Icon className="h-4 w-4" />
              </span>
            </div>
            <p className="mt-4 text-xl font-semibold text-[#2C2A29]">{money(value)}</p>
          </Card>
        ))}
      </div>

      <Card className="!rounded-[28px] !p-6">
        <h2 className="font-serif text-lg font-medium text-[#2C2A29]">Asignación del ingreso</h2>
        <p className="mt-1 text-sm text-[#7C756E]">
          Lo que salió del fondo mensual durante el ciclo actual.
        </p>
        <div className="mt-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <CycleItem label="Consumo" value={data.cycleExpenses} />
          <CycleItem label="Ahorro" value={data.cycleSavings} />
          <CycleItem label="Inversión" value={data.cycleInvestments} />
          <CycleItem label="Pagos de deuda" value={data.cycleDebtPayments} />
        </div>
      </Card>
    </div>
  );
}

function CycleItem({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-2xl bg-[#F3F1EC]/70 p-4">
      <p className="text-xs text-[#7C756E]">{label}</p>
      <p className="mt-1 text-lg font-semibold text-[#2C2A29]">{money(value)}</p>
    </div>
  );
}
