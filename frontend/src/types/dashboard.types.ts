// src/types/dashboard.types.ts

export interface DashboardPeriod {
  month: number;
  year: number;
  label: string;
}

export interface PreviousMonth {
  totalIncome: number;
  totalExpenses: number;
  reimbursementsReceived: number;
  netPersonalExpenses: number;
  netSavings: number;
  totalDebtPayments: number;
}

export interface DashboardChanges {
  incomeChange: number;
  expensesChange: number;
  savingsChange: number;
  debtPaymentsChange: number;
}

export interface FinancialPositionWarning {
  code: string;
  message: string;
}

export interface FinancialPositionAssets {
  cashAccounts: number;
  savingsAccounts: number;
  accountOpeningBalances: number;
  accountAdjustments: number;
  investmentPositions: number;
  investmentCostBasis: number;
  investmentUnrealizedGainLoss: number;
  investmentAccountBalance: number;
  uninvestedInvestmentCash: number;
  investments: number;
  creditCardCredits: number;
  total: number;
}

export interface FinancialPositionLiabilities {
  debts: number;
  creditCards: number;
  total: number;
}

export interface FinancialPosition {
  asOf: string;
  valuationBasis: 'current_snapshot';
  historicalSnapshotsSupported: boolean;
  assets: FinancialPositionAssets;
  liabilities: FinancialPositionLiabilities;
  savingsGoalAllocations: number;
  netWorth: number;
  warnings: FinancialPositionWarning[];
}

export interface DashboardOverview {
  period: DashboardPeriod;
  netWorthAsOf: string;
  totalIncome: number;
  totalExpenses: number;
  reimbursementsReceived: number;
  netPersonalExpenses: number;
  netSavings: number;
  savingsRate: number;
  totalDebt: number;
  totalDebtPayments: number;
  totalInvestments: number;
  totalSavingsGoals: number;
  netWorth: number;
  pendingEmergencyFundRestoration: number;
  financialPosition: FinancialPosition;
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
  reimbursementsReceived: number;
  netPersonalExpenses: number;
  savingsContributions: number;
  restorationContributions: number;
  newSavingsContributions: number;
  investmentContributions: number;
  savingsWithdrawals: number;
  debtPrincipalPaid: number;
  cashFlowResidual: number;
  consumptionRate: number;
  wealthBuildingRate: number;
}