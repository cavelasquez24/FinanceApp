export type ReimbursementDestinationType = 'account' | 'credit_card';

export interface Reimbursement {
  id: string;
  expenseId: string | null;
  expenseDescription: string | null;
  destinationType: ReimbursementDestinationType;
  accountId: string | null;
  accountName: string | null;
  creditCardId: string | null;
  creditCardName: string | null;
  amount: number;
  date: string;
  person: string | null;
  notes: string | null;
  idempotencyKey: string;
  createdAt: string;
}

export interface CreateReimbursementDto {
  expenseId?: string | null;
  destinationType: ReimbursementDestinationType;
  accountId?: string | null;
  creditCardId?: string | null;
  amount: number;
  date: string;
  person?: string | null;
  notes?: string | null;
  idempotencyKey: string;
}

export type UpdateReimbursementDto = CreateReimbursementDto;

export interface ReimbursementSummary {
  grossExpenses: number;
  reimbursementsReceived: number;
  netPersonalExpenses: number;
}

