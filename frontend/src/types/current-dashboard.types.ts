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
  cycleIncome: number;
  cycleExpenses: number;
  cycleSavings: number;
  cycleInvestments: number;
  cycleDebtPayments: number;
  cycleAvailable: number;
  suggestedDailyAvailable: number;
  daysRemaining: number;
}
