export type FinancialAccountType = 'cash' | 'savings' | 'investment';

export interface FinancialAccount {
  id: string;
  name: string;
  type: FinancialAccountType;
  currentBalance: number;
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
}

export interface CreateAccountDto {
  name: string;
  type: FinancialAccountType;
  openingBalance: number;
  isDefault: boolean;
}

export interface UpdateAccountDto {
  name: string;
  currentBalance: number;
  isDefault: boolean;
  isActive: boolean;
}
