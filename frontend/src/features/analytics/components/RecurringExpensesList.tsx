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
      <p className="py-6 text-center text-xs text-finflow-muted">Sin gastos recurrentes registrados.</p>
    );
  }

  return (
    <div className="space-y-2">
      {expenses.map((e) => (
        <div
          key={e.expenseId}
          className="flex items-center justify-between gap-3 rounded-xl border border-[#EFEAE2] bg-finflow-cream p-3"
        >
          <div className="flex items-center gap-2 min-w-0">
            <RefreshCw className="h-4 w-4 shrink-0 text-finflow-blue" strokeWidth={2} />
            <div className="min-w-0">
              <p className="truncate text-xs font-medium text-finflow-dark">{e.description}</p>
              <p className="text-[10px] text-finflow-muted">
                {e.categoryName} · {RECURRENCE_LABEL[e.recurrenceType] ?? e.recurrenceType}
              </p>
            </div>
          </div>
          <div className="shrink-0 text-right">
            <p className="text-xs font-semibold text-finflow-dark">{formatCurrency(e.amount)}</p>
            <p className="text-[10px] text-finflow-muted">{formatCurrency(e.annualImpact)}/año</p>
          </div>
        </div>
      ))}
    </div>
  );
}
