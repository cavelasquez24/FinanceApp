import type { RecurringExpenseDto } from '../types/analytics.types';
import { formatCurrency } from '../../../utils/formatCurrency';
import { RefreshCw } from 'lucide-react';

interface Props {
  expenses: RecurringExpenseDto[];
}

const RECURRENCE_LABEL: Record<string, string> = {
  Monthly: 'Mensual',
  Weekly: 'Semanal',
  Biweekly: 'Quincenal',
  Quarterly: 'Trimestral',
  Annual: 'Anual',
  Daily: 'Diario',
};

export function RecurringExpensesList({ expenses }: Props) {
  if (expenses.length === 0) {
    return (
      <p className="py-6 text-center text-xs text-[#7C756E]">Sin gastos recurrentes registrados.</p>
    );
  }

  return (
    <div className="space-y-2">
      {expenses.map((e) => (
        <div
          key={e.expenseId}
          className="flex items-center justify-between gap-3 rounded-xl border border-[#EFEAE2] bg-[#FBF9F4] p-3"
        >
          <div className="flex items-center gap-2 min-w-0">
            <RefreshCw className="h-4 w-4 shrink-0 text-[#5C7A99]" strokeWidth={2} />
            <div className="min-w-0">
              <p className="truncate text-xs font-medium text-[#2C2A29]">{e.description}</p>
              <p className="text-[10px] text-[#7C756E]">
                {e.categoryName} · {RECURRENCE_LABEL[e.recurrenceType] ?? e.recurrenceType}
              </p>
            </div>
          </div>
          <div className="shrink-0 text-right">
            <p className="text-xs font-semibold text-[#2C2A29]">{formatCurrency(e.amount)}</p>
            <p className="text-[10px] text-[#7C756E]">{formatCurrency(e.annualImpact)}/año</p>
          </div>
        </div>
      ))}
    </div>
  );
}
