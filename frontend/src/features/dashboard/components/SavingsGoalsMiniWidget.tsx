// src/features/dashboard/components/SavingsGoalsMiniWidget.tsx
import { Link } from 'react-router-dom';
import { PiggyBank, ArrowRight, Inbox } from 'lucide-react';
import { Card, CardHeader, Spinner } from '../../../components/ui';
import { useSavingsGoals } from '../../../features/savings/hooks/useSavings';
import { formatCurrency } from '../../../utils/formatCurrency';

// v2.1 — reemplaza el card estático "Metas de ahorro" (solo el total en $,
// sin contexto de avance). Reutiliza useSavingsGoals() ya existente en el
// módulo Metas, cero endpoint nuevo. Muestra hasta 3 metas activas
// ordenadas por progreso; el resto queda en /savings.
export function SavingsGoalsMiniWidget() {
  const { data: goals, isLoading, isError } = useSavingsGoals();

  const topGoals = [...(goals ?? [])]
    .filter((g) => !g.isCompleted)
    .sort((a, b) => b.progressPercentage - a.progressPercentage)
    .slice(0, 3);

  return (
    <Card className="!rounded-[28px]">
      <CardHeader
        title="Metas de ahorro"
        subtitle="Progreso de tus metas activas"
        action={
          <Link
            to="/savings"
            className="flex items-center gap-1 text-xs font-medium text-[#7C756E] transition-colors hover:text-[#2C2A29]"
          >
            Ver todas
            <ArrowRight className="h-3.5 w-3.5" strokeWidth={2} />
          </Link>
        }
      />

      {isLoading ? (
        <div className="flex flex-col items-center gap-3 py-8 text-[#7C756E]">
          <Spinner />
          <span className="text-sm">Cargando metas...</span>
        </div>
      ) : isError ? (
        <p className="py-8 text-center text-sm text-[#C97B63]">Error al cargar metas.</p>
      ) : topGoals.length === 0 ? (
        <div className="flex flex-col items-center gap-2 py-8 text-center text-[#7C756E]">
          <Inbox className="h-5 w-5" strokeWidth={2} />
          <span className="text-sm">Aún no tienes metas activas.</span>
        </div>
      ) : (
        <div className="space-y-4">
          {topGoals.map((goal) => (
            <div key={goal.id}>
              <div className="flex items-center justify-between text-sm">
                <span className="flex items-center gap-1.5 text-[#2C2A29]">
                  <PiggyBank className="h-3.5 w-3.5 text-[#8FA888]" strokeWidth={2} />
                  {goal.name}
                </span>
                <span className="text-[#7C756E]">{goal.progressPercentage.toFixed(0)}%</span>
              </div>
              <div className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-[#EFEAE2]">
                <div
                  className="h-full rounded-full bg-[#8FA888] transition-all duration-500"
                  style={{ width: `${Math.min(100, goal.progressPercentage)}%` }}
                />
              </div>
              <p className="mt-1 text-xs text-[#7C756E]">
                {formatCurrency(goal.currentAmount)} de {formatCurrency(goal.targetAmount)}
              </p>
            </div>
          ))}
        </div>
      )}
    </Card>
  );
}