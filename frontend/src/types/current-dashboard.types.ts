import type { FinancialPosition } from './dashboard.types';

export interface CurrentDashboard {
  asOf: string;
  cycleStart: string;
  cycleEnd: string;
  cycleLabel: string;
  cashBalance: number;
  savingsBalance: number;
  investmentBalance: number;
  debtBalance: number;
  netWorth: number;
  financialPosition: FinancialPosition;
  cycleIncome: number;
  cycleExpenses: number;
  cycleReimbursements: number;
  cycleNetExpenses: number;
  cycleSavings: number;
  cycleInvestments: number;
  cycleDebtPayments: number;
  cycleCashFlow: number;
  budgetAvailable: number;
  cycleAvailable: number;
  percentageUsed: number | null;
  isOverBudget: boolean | null;
  cycleReplenishmentCommitment: number;
  suggestedDailyAvailable: number;
  daysRemaining: number;
}
