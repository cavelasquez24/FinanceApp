export interface SavingsGoal {
  id: string;
  name: string;
  description: string | null;
  targetAmount: number;
  currentAmount: number;
  remainingAmount: number;
  progressPercentage: number;
  targetDate: string | null;
  isCompleted: boolean;
  icon: string | null;
  estimatedMonthsToComplete: number | null;
  createdAt: string;
}

export interface CreateSavingsGoalDto {
  name: string;
  description?: string;
  targetAmount: number;
  targetDate?: string; // Formato "YYYY-MM-DD"
  icon?: string;
}

export interface DepositDto {
  amount: number;
  notes?: string;
}

export type SavingsWithdrawalReason =
  | 'Consumed'
  | 'ReallocatedToOtherGoal'
  | 'ReallocatedToLiquid'
  | 'Correction';

export interface SavingsGoalWithdrawal {
  id: string;
  withdrawalDate: string; // "YYYY-MM-DD"
  amount: number;
  linkedExpenseId: string | null;
  reason: SavingsWithdrawalReason;
  notes: string | null;
  createdAt: string;
  goalCurrentAmountAfter: number;
}

export interface WithdrawDto {
  amount: number;
  withdrawalDate?: string; // "YYYY-MM-DD", opcional → hoy
  reason: SavingsWithdrawalReason;
  linkedExpenseId?: string; // solo si reason = 'Consumed'
  notes?: string;
}