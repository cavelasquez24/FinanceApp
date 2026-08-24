export type FinancialAccountType = 'cash' | 'savings' | 'investment';

export interface FinancialAccount {
  id: string;
  name: string;
  type: FinancialAccountType;
  currentBalance: number;
  openingBalance?: number | null;
  openingDate?: string | null;
  isDefault: boolean;
  isSystem: boolean;
  isActive: boolean;
}

export interface AccountTransaction {
  id: string;
  accountId: string;
  accountName: string;
  amount: number;
  date: string;
  description: string;
  transferId?: string;
}

export interface CreateAccountDto {
  name: string;
  type: FinancialAccountType;
  openingBalance: number;
  openingDate: string;
  isDefault: boolean;
}

export interface UpdateAccountDto {
  name: string;
  currentBalance: number;
  openingBalance?: number | null;
  openingDate?: string | null;
  isDefault: boolean;
  isActive: boolean;
}

export interface ReconciliationPreview {
  accountId: string;
  accountName: string;
  ledgerBalance: number;
  currentBalance: number;
  lastReconciliationDate: string | null;
  lastReconciliationActualBalance: number | null;
}

export interface ReconciliationCreateDto {
  actualBalance: number;
  reconciliationDate: string;
  notes?: string;
}

export interface ReconciliationResult {
  id: string;
  accountId: string;
  accountName: string;
  reconciliationDate: string;
  expectedBalance: number;
  actualBalance: number;
  difference: number;
  adjustmentCreated: boolean;
  adjustmentTransactionId: string | null;
  notes: string | null;
  status: "Reconciled" | "Skipped";
  createdAt: string;
}
