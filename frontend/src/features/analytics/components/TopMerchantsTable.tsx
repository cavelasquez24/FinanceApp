import type { TopMerchantDto } from '../types/analytics.types';
import { formatCurrency } from '../../../utils/formatCurrency';

interface Props {
  merchants: TopMerchantDto[];
}

export function TopMerchantsTable({ merchants }: Props) {
  if (merchants.length === 0) {
    return (
      <p className="py-6 text-center text-xs text-finflow-muted">Sin datos de comercios este período.</p>
    );
  }

  const max = merchants[0]?.totalAmount ?? 1;

  return (
    <div className="space-y-2">
      {merchants.map((m, i) => (
        <div key={i} className="space-y-1.5">
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-2 min-w-0">
              <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-lg bg-finflow-blue/10 text-[10px] font-bold text-finflow-blue">
                {i + 1}
              </span>
              <div className="min-w-0">
                <p className="truncate text-xs font-medium text-finflow-dark">
                  {m.merchant || 'Sin nombre'}
                </p>
                <p className="text-[10px] text-finflow-muted">{m.categoryName}</p>
              </div>
            </div>
            <div className="shrink-0 text-right">
              <p className="text-xs font-semibold text-finflow-dark">{formatCurrency(m.totalAmount)}</p>
              <p className="text-[10px] text-finflow-muted">{m.transactionCount} tx</p>
            </div>
          </div>
          <div className="h-1.5 w-full rounded-full bg-[#EFEAE2]">
            <div
              className="h-1.5 rounded-full bg-finflow-blue/60 transition-all duration-500"
              style={{ width: `${(m.totalAmount / max) * 100}%` }}
            />
          </div>
        </div>
      ))}
    </div>
  );
}
