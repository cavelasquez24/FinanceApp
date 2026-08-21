import type { DebtProjectionDto } from '../types/analytics.types';
import { formatCurrency } from '../../../utils/formatCurrency';
import { cn } from '../../../utils/cn';

interface Props {
  data: DebtProjectionDto;
}

export function DebtProjectionPanel({ data }: Props) {
  if (data.totalOutstanding === 0) {
    return (
      <p className="py-6 text-center text-xs text-[#7C756E]">Sin deudas activas.</p>
    );
  }

  return (
    <div className="space-y-4">
      {/* Summary row */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
        <div className="rounded-2xl border border-[#EFEAE2] bg-[#FBF9F4] p-3">
          <p className="text-[10px] uppercase tracking-wide text-[#7C756E]">Total deuda</p>
          <p className="mt-1 text-base font-semibold text-[#2C2A29]">
            {formatCurrency(data.totalOutstanding)}
          </p>
        </div>
        <div className="rounded-2xl border border-[#EFEAE2] bg-[#FBF9F4] p-3">
          <p className="text-[10px] uppercase tracking-wide text-[#7C756E]">Pago mensual prom.</p>
          <p className="mt-1 text-base font-semibold text-[#2C2A29]">
            {formatCurrency(data.avgMonthlyPayment)}
          </p>
        </div>
        {data.estimatedPayoffMonths > 0 && (
          <div className="rounded-2xl border border-[#8FA888]/30 bg-[#8FA888]/05 p-3">
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
              <p className="truncate text-xs font-medium text-[#2C2A29]">{d.name}</p>
              <p className="mt-0.5 text-[10px] text-[#7C756E]">
                {formatCurrency(d.avgMonthlyPayment)}/mes · {d.estimatedPayoffMonths} meses
              </p>
            </div>
            <div className="shrink-0 text-right">
              <p className="text-xs font-semibold text-[#C97B63]">
                {formatCurrency(d.currentBalance)}
              </p>
              {d.estimatedPayoffDate && (
                <p className="text-[10px] text-[#7C756E]">
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
