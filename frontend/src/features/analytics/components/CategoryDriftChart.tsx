import type { CategoryDriftDto } from '../types/analytics.types';
import { formatCurrency } from '../../../utils/formatCurrency';
import { cn } from '../../../utils/cn';

interface Props {
  drift: CategoryDriftDto[];
}

export function CategoryDriftChart({ drift }: Props) {
  if (drift.length === 0) {
    return (
      <p className="py-6 text-center text-xs text-[#7C756E]">Sin datos de deriva este período.</p>
    );
  }

  // Sort by absolute drift descending
  const sorted = [...drift].sort((a, b) => Math.abs(b.driftAmount) - Math.abs(a.driftAmount));

  return (
    <div className="space-y-2.5">
      {sorted.map((d) => {
        const isUp = d.driftAmount > 0;
        return (
          <div key={d.categoryName} className="space-y-1">
            <div className="flex items-center justify-between gap-2">
              <div className="flex items-center gap-2 min-w-0">
                <span
                  className="h-2.5 w-2.5 shrink-0 rounded-full"
                  style={{ backgroundColor: d.categoryColor || '#7C756E' }}
                />
                <span className="truncate text-xs font-medium text-[#2C2A29]">
                  {d.categoryName}
                </span>
              </div>
              <div className="flex items-center gap-2 shrink-0">
                <span className="text-xs text-[#7C756E]">{formatCurrency(d.currentAmount)}</span>
                <span
                  className={cn(
                    'text-xs font-semibold',
                    isUp ? 'text-[#C97B63]' : 'text-[#8FA888]'
                  )}
                >
                  {isUp ? '+' : ''}{d.driftPct.toFixed(1)}%
                </span>
              </div>
            </div>
            {/* Dual bar: previous (grey) + current (colored) */}
            <div className="relative h-2 w-full overflow-hidden rounded-full bg-[#EFEAE2]">
              <div
                className="absolute inset-y-0 left-0 rounded-full opacity-40"
                style={{
                  backgroundColor: d.categoryColor || '#7C756E',
                  width: `${Math.min((d.previousAmount / Math.max(d.currentAmount, d.previousAmount, 1)) * 100, 100)}%`,
                }}
              />
              <div
                className="absolute inset-y-0 left-0 rounded-full"
                style={{
                  backgroundColor: d.categoryColor || '#7C756E',
                  width: `${Math.min((d.currentAmount / Math.max(d.currentAmount, d.previousAmount, 1)) * 100, 100)}%`,
                  opacity: 0.8,
                }}
              />
            </div>
          </div>
        );
      })}
    </div>
  );
}
