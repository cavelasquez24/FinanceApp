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

export interface CreateAccountTransferDto {
  fromAccountId: string;
  toAccountId: string;
  amount: number;
  date: string;
  description?: string;
  idempotencyKey: string;
}

export interface AccountTransfer {
  transferId: string;
  fromAccountId: string;
  fromAccountName: string;
  toAccountId: string;
  toAccountName: string;
  amount: number;
  date: string;
  description: string;
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
