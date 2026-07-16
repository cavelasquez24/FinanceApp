export type DebtType = 'credit_card' | 'loan' | 'mortgage' | 'personal' | 'other';

export interface Debt {
  id: string;
  name: string;
  type: DebtType;
  creditor: string | null;
  originalAmount: number;
  currentBalance: number;
  amountPaid: number;
  paidPercentage: number;
  interestRate: number | null;
  minimumPayment: number | null;
  dueDay: number | null;
  startDate: string;              // "YYYY-MM-DD"
  targetPayoffDate: string | null;
  isActive: boolean;
  isPaidOff: boolean;
  notes: string | null;
  createdAt: string;
}

export interface DebtByType {
  type: DebtType;
  currentBalance: number;
  percentage: number;
}

export interface UpcomingPayment {
  debtId: string;
  debtName: string;
  dueDay: number;
  minimumPayment: number | null;
}

export interface DebtSummary {
  totalOriginal: number;
  totalCurrentBalance: number;
  totalPaid: number;
  totalPaidPercentage: number;
  byType: DebtByType[];
  upcomingPayments: UpcomingPayment[];
}

export interface DebtPayment {
  id: string;
  paymentDate: string;   // "YYYY-MM-DD"
  amount: number;
  principalAmount: number;
  interestAmount: number;
  notes: string | null;
  createdAt: string;
}

export interface CreateDebtDto {
  name: string;
  type: DebtType;
  creditor?: string;
  originalAmount: number;
  currentBalance: number;
  interestRate?: number;
  minimumPayment?: number;
  dueDay?: number;
  startDate: string;               // "YYYY-MM-DD"
  targetPayoffDate?: string;
  notes?: string;
}

export interface UpdateDebtDto {
  name: string;
  creditor?: string;
  currentBalance: number;
  interestRate?: number;
  minimumPayment?: number;
  dueDay?: number;
  targetPayoffDate?: string;
  isActive: boolean;
  notes?: string;
}

export interface CreateDebtPaymentDto {
  paymentDate: string;    // "YYYY-MM-DD"
  amount: number;
  principalAmount: number;
  interestAmount?: number; // default 0
  notes?: string;
}

export const DEBT_TYPE_LABELS: Record<DebtType, string> = {
  credit_card: 'Tarjeta de Crédito',
  loan: 'Préstamo',
  mortgage: 'Hipoteca',
  personal: 'Deuda Personal',
  other: 'Otro',
};