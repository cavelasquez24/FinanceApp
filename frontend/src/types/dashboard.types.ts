// src/types/dashboard.types.ts

export interface DashboardPeriod {
  month: number;
  year: number;
  label: string;
}

export interface PreviousMonth {
  totalIncome: number;
  totalExpenses: number;
  netSavings: number;
  totalDebtPayments: number;
}

export interface DashboardChanges {
  incomeChange: number;
  expensesChange: number;
  savingsChange: number;
  debtPaymentsChange: number;
}

export interface DashboardOverview {
  period: DashboardPeriod;
  totalIncome: number;
  totalExpenses: number;
  netSavings: number;
  savingsRate: number;
  totalDebt: number;
  totalDebtPayments: number;
  totalInvestments: number;
  totalSavingsGoals: number;
  netWorth: number;
  previousMonth: PreviousMonth;
  changes: DashboardChanges;
}

export interface MonthlyTrend {
  labels: string[];
  income: number[];
  expenses: number[];
  residual: number[];
}

export interface CategoryChartItem {
  categoryName: string;
  categoryColor: string;
  categoryIcon: string | null;
  amount: number;
  percentage: number;
}

export interface ExpensesByCategoryResponse {
  categories: CategoryChartItem[];
  totalAmount: number;
}

// v2.0.1 — sección 5. GET /dashboard/cashflow-statement
export interface CashFlowStatement {
  income: number;
  consumptionExpenses: number;
  savingsContributions: number;
  investmentContributions: number;
  savingsWithdrawals: number;
  debtPrincipalPaid: number;
  cashFlowResidual: number;
  consumptionRate: number;
  wealthBuildingRate: number;
}