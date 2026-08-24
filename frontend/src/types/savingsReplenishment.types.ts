export type ReplenishmentStatus = "Active" | "Paused" | "Completed" | "Cancelled";
export type DebitType = "Automatic" | "Manual" | "Adjustment";

export interface ReplenishmentDebitDto {
  id: string;
  amount: number;
  debitDate: string; // "YYYY-MM-DD"
  type: DebitType;
  notes: string | null;
}

export interface SavingsReplenishmentDto {
  id: string;
  savingsGoalId: string;
  savingsGoalName: string;
  sourceAccountId: string;
  sourceAccountName: string;
  name: string;
  notes: string | null;
  amountTaken: number;
  amountReplenished: number;
  pendingAmount: number;
  monthlyDebitAmount: number;
  progressPercent: number;
  estimatedCyclesRemaining: number;
  autoDebitEnabled: boolean;
  isPaused: boolean;
  status: ReplenishmentStatus;
  createdAt: string;
  completedAt: string | null;
  lastDebitAt: string | null;
  debits: ReplenishmentDebitDto[];
}

export interface SavingsReplenishmentCreateDto {
  savingsGoalId: string;
  sourceAccountId: string;
  name: string;
  notes?: string;
  amountTaken: number;
  monthlyDebitAmount: number;
  autoDebitEnabled?: boolean;
}

export interface SavingsReplenishmentManualDebitDto {
  amount: number;
  notes?: string;
  idempotencyKey: string;
}

export interface SavingsReplenishmentPauseDto {
  reason?: string;
}

export interface ReplenishmentDebitFailureDto {
  replenishmentId: string;
  replenishmentName: string;
  requiredAmount: number;
  availableBalance: number;
}

export interface ReplenishmentCycleResultDto {
  processedCount: number;
  skippedAlreadyDebitedCount: number;
  insufficientFunds: ReplenishmentDebitFailureDto[];
}
