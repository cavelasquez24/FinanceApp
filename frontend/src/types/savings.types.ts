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
  purpose: 'general' | 'emergency_fund';
  minimumProtectedAmount: number | null;
  pendingRestorationAmount: number;
  openRestorationsCount: number;
  nextRestorationDate: string | null;
}

export interface CreateSavingsGoalDto {
  name: string;
  description?: string;
  targetAmount: number;
  initialAmount?: number;
  initialSourceAccountId?: string;
  initialFundingDate?: string;
  idempotencyKey?: string;
  targetDate?: string; // Formato "YYYY-MM-DD"
  icon?: string;
  purpose?: 'general' | 'emergency_fund';
  minimumProtectedAmount?: number;
}

export interface DepositDto {
  amount: number;
  sourceAccountId: string;
  idempotencyKey: string;
  contributionDate?: string;
  notes?: string;
}

export interface ArchiveSavingsGoalDto {
  resolution: 'release' | 'reassign';
  destinationAccountId?: string;
  targetGoalId?: string;
  date?: string;
  idempotencyKey: string;
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
  destinationAccountId?: string;
  targetGoalId?: string;
  idempotencyKey: string;
  linkedExpenseId?: string; // solo si reason = 'Consumed'
  notes?: string;
}
export interface EmergencyFundUseDto {
  fundedAmount: number;
  expenseAmount: number;
  categoryId: string;
  expenseAccountId?: string;
  description: string;
  scheduledSourceAccountId?: string;
  acquisitionDate: string;
  paymentMethod: string;
  targetRestorationDate: string;
  scheduledContributionAmount: number;
  firstScheduledDate: string;
  notes?: string;
}

export interface EmergencyFundRestorationPaymentDto {
  amount: number;
  paymentDate: string;
  sourceAccountId?: string;
  notes?: string;
}

export interface EmergencyFundRestoration {
  id: string;
  savingsGoalId: string;
  linkedExpenseId: string;
  estimatedCompletionDate: string | null;
  description: string;
  acquisitionDate: string;
  originalAmount: number;
  restoredAmount: number;
  outstandingAmount: number;
  targetRestorationDate: string;
  scheduledContributionAmount: number;
  nextContributionAmount: number;
  nextScheduledDate: string;
  scheduledSourceAccountId?: string;
  status: 'open' | 'completed' | 'cancelled';
  completedDate: string | null;
  isOverdue: boolean;
  notes: string | null;
}

export interface DueRestorationProcessingResult {
  processedCount: number;
  processedAmount: number;
  insufficientFundsCount: number;
}
