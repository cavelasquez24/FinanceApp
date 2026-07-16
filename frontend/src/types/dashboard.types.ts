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
}

export interface DashboardChanges {
  incomeChange: number;
  expensesChange: number;
  savingsChange: number;
}

export interface DashboardOverview {
  period: DashboardPeriod;
  totalIncome: number;
  totalExpenses: number;
  netSavings: number;
  savingsRate: number;
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
  savings: number[];
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