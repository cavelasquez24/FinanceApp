// src/features/dashboard/components/DashboardHeroRow.tsx
import { Landmark, Wallet } from 'lucide-react';
import { Card } from '../../../components/ui';

interface Props {
  netWorth: number;
  cashFlowResidual: number | null; // null mientras carga o si falla el fetch de cashflow
}

const currency = (value: number) =>
  new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(value);

// Las dos preguntas reales de un resumen ejecutivo: "¿cuánto tengo?"
// (patrimonio, foto) y "¿cómo voy este mes?" (flujo, película). Todo lo
// demás en OverviewStats es apoyo a estas dos.
export function DashboardHeroRow({ netWorth, cashFlowResidual }: Props) {
  const isResidualNegative = (cashFlowResidual ?? 0) < 0;
  const hasResidual = cashFlowResidual !== null;

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
      <Card className="!rounded-[28px] !p-7">
        <div className="flex items-start justify-between">
          <div>
            <p className="text-xs font-medium uppercase tracking-wide text-finflow-muted">
              Patrimonio registrado
            </p>
            <p className="mt-2 font-serif text-4xl font-medium text-finflow-dark">
              {currency(netWorth)}
            </p>
            <p className="mt-2 text-xs text-finflow-muted">Inversiones y metas menos deuda; no presume efectivo no registrado.</p>
            {/* TODO: sparkline cuando exista GET /dashboard/net-worth-trend
                (MonthlySnapshot.NetWorthAtClose, pendiente en backend —
                resumen técnico sección 3.5). No se simula con mock data. */}
          </div>
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-finflow-dark/10 text-finflow-dark">
            <Landmark className="h-5 w-5" strokeWidth={2} />
          </div>
        </div>
      </Card>

      <Card
        className={`!rounded-[28px] !p-7 ${
          isResidualNegative ? '!border-finflow-rust/40 !bg-finflow-rust/5' : ''
        }`}
      >
        <div className="flex items-start justify-between">
          <div>
            <p className="text-xs font-medium uppercase tracking-wide text-finflow-muted">
              Disponible del ciclo
            </p>
            <p
              className={`mt-2 font-serif text-4xl font-medium ${
                isResidualNegative ? 'text-finflow-rust' : 'text-finflow-dark'
              }`}
            >
              {hasResidual ? currency(cashFlowResidual!) : '—'}
            </p>
            <p className={`mt-2 text-xs ${isResidualNegative ? 'text-finflow-rust' : 'text-finflow-muted'}`}>
              {isResidualNegative
                ? 'Gastando/asignando más de lo que ingresa este mes.'
                : 'Caja tras gastos, aportes, inversión y capital de deuda.'}
            </p>
          </div>
          <div
            className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl ${
              isResidualNegative ? 'bg-finflow-rust/15 text-finflow-rust' : 'bg-finflow-green/15 text-finflow-green'
            }`}
          >
            <Wallet className="h-5 w-5" strokeWidth={2} />
          </div>
        </div>
      </Card>
    </div>
  );
}