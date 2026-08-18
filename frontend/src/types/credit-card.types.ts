export interface CreditCard {
  id: string;
  name: string;
  currentBalance: number;
  creditLimit: number | null;
  availableCredit: number | null;
  closingDay: number;
  dueDay: number;
  isActive: boolean;
  notes: string | null;
  createdAt: string;
}

export interface CreateCreditCardDto {
  name: string;
  openingBalance: number;
  openingDate: string;
  creditLimit?: number | null;
  closingDay: number;
  dueDay: number;
  notes?: string;
}

export interface UpdateCreditCardDto {
  name: string;
  creditLimit?: number | null;
  closingDay: number;
  dueDay: number;
  isActive: boolean;
  notes?: string;
}

export interface CreditCardPaymentDto {
  sourceAccountId: string;
  principalAmount: number;
  commissionAmount: number;
  commissionCategoryId?: string | null;
  paymentDate: string;
  notes?: string;
  idempotencyKey: string;
}

export interface CreditCardPayment {
  id: string;
  creditCardId: string;
  sourceAccountId: string;
  sourceAccountName: string;
  principalAmount: number;
  commissionAmount: number;
  commissionExpenseId: string | null;
  paymentDate: string;
  notes: string | null;
  idempotencyKey: string;
  cardBalanceAfter: number;
  createdAt: string;
  isVoided: boolean;
  voidedAt: string | null;
  voidReason: string | null;
  voidIdempotencyKey: string | null;
}

export interface CreditCardChargeDto {
  type: 'interest' | 'fee';
  categoryId: string;
  amount: number;
  date: string;
  description?: string;
  idempotencyKey: string;
}
export interface CreditCardPaymentVoidDto {
  date: string;
  reason: string;
  idempotencyKey: string;
}



export interface CreditCardTransaction {
  id: string;
  type: 'opening_balance' | 'purchase' | 'payment' | 'payment_reversal' | 'interest' | 'fee' | 'refund' | 'adjustment';
  amount: number;
  date: string;
  description: string;
  sourceType: string;
  sourceId: string;
  createdAt: string;
}
