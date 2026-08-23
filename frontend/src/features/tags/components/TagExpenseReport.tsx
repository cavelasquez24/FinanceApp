import { useTagExpenseReport } from '../hooks/useTags';
import { Spinner } from '../../../components/ui';

interface Props { startDate: string; endDate: string; }

export function TagExpenseReport({ startDate, endDate }: Props) {
  const { data, isLoading } = useTagExpenseReport(startDate, endDate);
  if (isLoading) return <div className="p-6"><Spinner /></div>;
  if (!data) return null;
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-3 gap-3 text-center">
        <div className="rounded-xl bg-[#F3F1EC] p-3"><strong className="block text-lg">{data.coveragePercentage}%</strong><span className="text-xs text-finflow-muted">Cobertura</span></div>
        <div className="rounded-xl bg-[#F3F1EC] p-3"><strong className="block text-lg">{data.taggedExpenses}</strong><span className="text-xs text-finflow-muted">Etiquetados</span></div>
        <div className="rounded-xl bg-[#F3F1EC] p-3"><strong className="block text-lg">{data.untaggedExpenses}</strong><span className="text-xs text-finflow-muted">Sin etiquetas</span></div>
      </div>
      <div className="space-y-2">
        {data.tags.map((tag) => (
          <div key={tag.tagId} className="flex items-center justify-between border-b border-[#EFEAE2] py-2 text-sm">
            <span className="flex items-center gap-2"><i className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: tag.color ?? 'var(--color-finflow-blue)' }} />{tag.name} <small className="text-finflow-muted">({tag.expenseCount})</small></span>
            <span className="font-medium">${tag.totalAmount.toFixed(2)} <small className="text-finflow-muted">prom. ${tag.averageAmount.toFixed(2)}</small></span>
          </div>
        ))}
      </div>
      <p className="text-xs text-finflow-muted">Un gasto puede aparecer en varias etiquetas; estos totales no deben sumarse entre sí.</p>
    </div>
  );
}
