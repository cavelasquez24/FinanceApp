import type { HealthScoreComponents, ScoreComponent } from '../types/analytics.types';
import { cn } from '../../../utils/cn';

interface Props {
  components: HealthScoreComponents;
}

const STATUS_STYLES = {
  Good:     { bar: 'bg-[#8FA888]', badge: 'bg-[#8FA888]/15 text-[#5A7853]', label: 'Bien' },
  Warning:  { bar: 'bg-[#D4A855]', badge: 'bg-[#D4A855]/15 text-[#8B6A1A]', label: 'Atención' },
  Critical: { bar: 'bg-[#C97B63]', badge: 'bg-[#C97B63]/15 text-[#8B4A36]', label: 'Crítico' },
};

function ComponentRow({ c }: { c: ScoreComponent }) {
  const styles = STATUS_STYLES[c.status];
  return (
    <div className="space-y-1.5">
      <div className="flex items-center justify-between gap-2">
        <span className="text-xs font-medium text-[#2C2A29]">{c.label}</span>
        <div className="flex items-center gap-2 shrink-0">
          <span className={cn('rounded-full px-2 py-0.5 text-[10px] font-semibold', styles.badge)}>
            {styles.label}
          </span>
          <span className="w-8 text-right text-xs font-semibold text-[#2C2A29]">{c.score}</span>
        </div>
      </div>
      <div className="h-2 w-full rounded-full bg-[#EFEAE2]">
        <div
          className={cn('h-2 rounded-full transition-all duration-500', styles.bar)}
          style={{ width: `${c.score}%` }}
        />
      </div>
    </div>
  );
}

export function HealthScoreBreakdown({ components }: Props) {
  const items: ScoreComponent[] = [
    components.savingsRate,
    components.debtToIncome,
    components.emergencyFundCoverage,
    components.expenseRatio,
    components.budgetAdherence,
    components.investmentRate,
  ];

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {items.map((c) => (
        <ComponentRow key={c.label} c={c} />
      ))}
    </div>
  );
}
