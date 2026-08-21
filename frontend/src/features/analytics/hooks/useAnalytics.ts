import { useNetWorthTimeline } from './useNetWorthTimeline';
import { useHealthScore } from './useHealthScore';
import { useExpenseIntelligence } from './useExpenseIntelligence';
import { useDebtProjection } from './useDebtProjection';
import { useSavingsGoalEta } from './useSavingsGoalEta';
import { useYearOverYear } from './useYearOverYear';
import { useBudgetVsActual } from './useBudgetVsActual';

export function useAnalytics(month: number, year: number) {
  const netWorthTimeline = useNetWorthTimeline(12);
  const healthScore = useHealthScore(month, year);
  const expenseIntelligence = useExpenseIntelligence(month, year);
  const debtProjection = useDebtProjection();
  const savingsGoalEta = useSavingsGoalEta();
  const yoy = useYearOverYear(year);
  const budgetVsActual = useBudgetVsActual(6);

  return {
    netWorthTimeline,
    healthScore,
    expenseIntelligence,
    debtProjection,
    savingsGoalEta,
    yoy,
    budgetVsActual,
  };
}
