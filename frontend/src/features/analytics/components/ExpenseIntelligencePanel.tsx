import type { ExpenseIntelligenceDto } from '../types/analytics.types';
import { TopMerchantsTable } from './TopMerchantsTable';
import { RecurringExpensesList } from './RecurringExpensesList';
import { CategoryDriftChart } from './CategoryDriftChart';

interface Props {
  data: ExpenseIntelligenceDto;
}

export function ExpenseIntelligencePanel({ data }: Props) {
  return (
    <div className="grid gap-6 lg:grid-cols-3">
      <div>
        <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-[#7C756E]">
          Top Comercios
        </p>
        <TopMerchantsTable merchants={data.topMerchants} />
      </div>

      <div>
        <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-[#7C756E]">
          Gastos Recurrentes
        </p>
        <RecurringExpensesList expenses={data.recurringExpenses} />
      </div>

      <div>
        <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-[#7C756E]">
          Deriva por Categoría
        </p>
        <CategoryDriftChart drift={data.categoryDrift} />
      </div>
    </div>
  );
}
