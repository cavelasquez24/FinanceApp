export interface ScoreComponent {
  score: number;
  value: number;
  benchmark: number;
  label: string;
  status: 'Good' | 'Warning' | 'Critical';
}

export interface HealthScoreComponents {
  savingsRate: ScoreComponent;
  debtToIncome: ScoreComponent;
  emergencyFundCoverage: ScoreComponent;
  expenseRatio: ScoreComponent;
  budgetAdherence: ScoreComponent;
  investmentRate: ScoreComponent;
}

export interface FinancialHealthScoreDto {
  score: number;
  grade: 'A' | 'B' | 'C' | 'D' | 'F';
  components: HealthScoreComponents;
  recommendations: string[];
}

export interface NetWorthTimelineDto {
  labels: string[];
  netWorth: number[];
  totalAssets: number[];
  totalLiabilities: number[];
  netWorthChange: number;
  netWorthChangePct: number;
}

export interface TopMerchantDto {
  merchant: string;
  totalAmount: number;
  transactionCount: number;
  categoryName: string;
}

export interface RecurringExpenseDto {
  expenseId: string;
  description: string;
  amount: number;
  recurrenceType: string;
  categoryName: string;
  annualImpact: number;
}

export interface CategoryDriftDto {
  categoryName: string;
  categoryColor: string;
  currentAmount: number;
  previousAmount: number;
  driftAmount: number;
  driftPct: number;
}

export interface ExpenseIntelligenceDto {
  topMerchants: TopMerchantDto[];
  recurringExpenses: RecurringExpenseDto[];
  categoryDrift: CategoryDriftDto[];
}

export interface DebtLineProjectionDto {
  debtId: string;
  name: string;
  currentBalance: number;
  avgMonthlyPayment: number;
  estimatedPayoffMonths: number;
  estimatedPayoffDate: string;
}

export interface DebtProjectionDto {
  totalOutstanding: number;
  avgMonthlyPayment: number;
  estimatedPayoffMonths: number;
  estimatedPayoffDate: string;
  byDebt: DebtLineProjectionDto[];
}

export interface SavingsGoalEtaDto {
  goalId: string;
  name: string;
  currentAmount: number;
  targetAmount: number;
  remaining: number;
  progressPct: number;
  avgMonthlyContribution: number;
  estimatedMonthsToGoal: number | null;
  estimatedCompletionDate: string | null;
  isOnTrack: boolean;
}

export interface YoYMonthDto {
  monthLabel: string;
  currentIncome: number;
  currentExpenses: number;
  currentNetSavings: number;
  prevIncome: number;
  prevExpenses: number;
  prevNetSavings: number;
}

export interface YoYTotalsDto {
  incomeChangeAbs: number;
  incomeChangePct: number;
  expensesChangeAbs: number;
  expensesChangePct: number;
  netSavingsChangeAbs: number;
  netSavingsChangePct: number;
}

export interface YearOverYearDto {
  year: number;
  previousYear: number;
  months: YoYMonthDto[];
  totals: YoYTotalsDto;
}

export interface BudgetVsActualPeriodDto {
  label: string;
  budgeted: number;
  actual: number;
  variance: number;
  adherencePct: number;
}

export interface BudgetVsActualHistoryDto {
  periods: BudgetVsActualPeriodDto[];
}
