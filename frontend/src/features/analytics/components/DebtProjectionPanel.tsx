import type { DebtProjectionDto } from '../types/analytics.types';
import { formatCurrency } from '../../../utils/formatCurrency';

interface Props {
  data: DebtProjectionDto;
}

export function DebtProjectionPanel({ data }: Props) {
  if (data.totalOutstanding === 0) {
    return (
      <p className="py-6 text-center text-xs text-finflow-muted">Sin deudas activas.</p>
    );
  }

  return (
    <div className="space-y-4">
      {/* Summary row */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
        <div className="rounded-2xl border border-[#EFEAE2] bg-finflow-cream p-3">
          <p className="text-[10px] uppercase tracking-wide text-finflow-muted">Total deuda</p>
          <p className="mt-1 text-base font-semibold text-finflow-dark">
            {formatCurrency(data.totalOutstanding)}
          </p>
        </div>
        <div className="rounded-2xl border border-[#EFEAE2] bg-finflow-cream p-3">
          <p className="text-[10px] uppercase tracking-wide text-finflow-muted">Pago mensual prom.</p>
          <p className="mt-1 text-base font-semibold text-finflow-dark">
            {formatCurrency(data.avgMonthlyPayment)}
          </p>
        </div>
        {data.estimatedPayoffMonths > 0 && (
          <div className="rounded-2xl border border-finflow-green/30 bg-finflow-green/05 p-3">
            <p className="text-[10px] uppercase tracking-wide text-[#5A7853]">Libre de deuda</p>
            <p className="mt-1 text-base font-semibold text-[#5A7853]">
              {data.estimatedPayoffMonths} meses
            </p>
          </div>
        )}
      </div>

      {/* Per-debt list */}
      <div className="space-y-2">
        {data.byDebt.map((d) => (
          <div
            key={d.debtId}
            className="flex items-center justify-between gap-3 rounded-xl border border-[#EFEAE2] p-3"
          >
            <div className="min-w-0">
              <p className="truncate text-xs font-medium text-finflow-dark">{d.name}</p>
              <p className="mt-0.5 text-[10px] text-finflow-muted">
                {formatCurrency(d.avgMonthlyPayment)}/mes · {d.estimatedPayoffMonths} meses
              </p>
            </div>
            <div className="shrink-0 text-right">
              <p className="text-xs font-semibold text-finflow-rust">
                {formatCurrency(d.currentBalance)}
              </p>
              {d.estimatedPayoffDate && (
                <p className="text-[10px] text-finflow-muted">
                  {new Date(d.estimatedPayoffDate).toLocaleDateString('es', { month: 'short', year: '2-digit' })}
                </p>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
