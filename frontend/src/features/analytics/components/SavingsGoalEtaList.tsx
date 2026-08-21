import type { SavingsGoalEtaDto } from '../types/analytics.types';
import { formatCurrency } from '../../../utils/formatCurrency';
import { cn } from '../../../utils/cn';

interface Props {
  goals: SavingsGoalEtaDto[];
}

export function SavingsGoalEtaList({ goals }: Props) {
  if (goals.length === 0) {
    return (
      <p className="py-6 text-center text-xs text-[#7C756E]">Sin metas de ahorro activas.</p>
    );
  }

  return (
    <div className="space-y-3">
      {goals.map((g) => (
        <div key={g.goalId} className="rounded-2xl border border-[#EFEAE2] p-3">
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0">
              <p className="truncate text-xs font-medium text-[#2C2A29]">{g.name}</p>
              <p className="mt-0.5 text-[10px] text-[#7C756E]">
                {formatCurrency(g.currentAmount)} / {formatCurrency(g.targetAmount)}
              </p>
            </div>
            <span
              className={cn(
                'shrink-0 rounded-full px-2 py-0.5 text-[10px] font-semibold',
                g.isOnTrack
                  ? 'bg-[#8FA888]/15 text-[#5A7853]'
                  : 'bg-[#D4A855]/15 text-[#8B6A1A]'
              )}
            >
              {g.isOnTrack ? 'En camino' : 'Rezagado'}
            </span>
          </div>

          {/* Progress bar */}
          <div className="mt-2.5 h-2 w-full rounded-full bg-[#EFEAE2]">
            <div
              className="h-2 rounded-full bg-[#5C7A99] transition-all duration-500"
              style={{ width: `${Math.min(g.progressPct, 100)}%` }}
            />
          </div>

          <div className="mt-2 flex flex-wrap items-center justify-between gap-x-4 gap-y-1">
            <p className="text-[10px] text-[#7C756E]">
              {g.progressPct.toFixed(1)}% completado
            </p>
            {g.estimatedMonthsToGoal != null ? (
              <p className="text-[10px] text-[#5C7A99]">
                ETA: {g.estimatedMonthsToGoal} meses
                {g.estimatedCompletionDate && (
                  <> · {new Date(g.estimatedCompletionDate).toLocaleDateString('es', { month: 'short', year: 'numeric' })}</>
                )}
              </p>
            ) : (
              <p className="text-[10px] text-[#7C756E]">Sin aportes recientes</p>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
