export interface SavingsGoal {
  id: string;
  savingsAccountId: string | null;
  savingsAccountName: string | null;
  savingsAccountBalance: number | null;
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
  purpose: "general" | "emergency_fund";
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
  initialFundingDate?: string;
  idempotencyKey?: string;
  savingsAccountId: string;
  initialFundingMode?: "existing_balance" | "account_transfer";
  initialSourceAccountId?: string;
  targetDate?: string; // Formato "YYYY-MM-DD"
  icon?: string;
  purpose?: "general" | "emergency_fund";
  minimumProtectedAmount?: number;
}

export interface DepositDto {
  amount: number;
  fundingMode: "existing_balance" | "account_transfer";
  sourceAccountId?: string;
  idempotencyKey: string;
  contributionDate?: string;
  notes?: string;
}

export interface ArchiveSavingsGoalDto {
  resolution: "release" | "reassign";
  targetGoalId?: string;
  date?: string;
  idempotencyKey: string;
  notes?: string;
}

export type SavingsWithdrawalReason =
  | "Consumed"
  | "ReallocatedToOtherGoal"
  | "ReallocatedToLiquid"
  | "Correction"
  | "TemporaryLoan";

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
  targetGoalId?: string;
  idempotencyKey: string;
  destinationAccountId?: string;
  expenseCategoryId?: string;
  expenseDescription?: string;
  notes?: string;
}
export interface EmergencyFundUseDto {
  fundedAmount: number;
  description: string;
  acquisitionDate: string;
  targetRestorationDate: string;
  useMode: "expense" | "account_transfer";
  destinationAccountId?: string;
  expenseCategoryId?: string;
  scheduledContributionAmount: number;
  firstScheduledDate: string;
  idempotencyKey: string;
  notes?: string;
}

export interface EmergencyFundRestorationPaymentDto {
  amount: number;
  paymentDate: string;
  idempotencyKey: string;
  notes?: string;
  fundingMode: "existing_balance" | "account_transfer";
  sourceAccountId?: string;
}

export interface EmergencyFundRestoration {
  id: string;
  savingsGoalId: string;
  linkedExpenseId: string | null;
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
  status: "open" | "completed" | "cancelled";
  completedDate: string | null;
  isOverdue: boolean;
  scheduledSourceAccountId: string | null;
  notes: string | null;
}
