export type TransferStatus = 'pending' | 'completed' | 'cancelled';

export interface AccountTransferCreateDto {
  fromAccountId: string;
  toAccountId: string;
  amount: number;
  transferDate: string; // "YYYY-MM-DD"
  description?: string;
  /**
   * Opcional. Reenviar el mismo valor en un reintento (p.ej. timeout de red)
   * evita que el backend duplique la transferencia.
   */
  transferGroupId?: string;
}

export interface AccountTransferDto {
  id: string;
  fromAccountId: string;
  fromAccountName: string;
  toAccountId: string;
  toAccountName: string;
  amount: number;
  transferDate: string;
  description: string | null;
  status: TransferStatus;
  transferGroupId: string;
  createdAt: string;
}

/** Versión liviana para el historial: sin transferGroupId. */
export interface AccountTransferSummaryDto {
  id: string;
  fromAccountId: string;
  fromAccountName: string;
  toAccountId: string;
  toAccountName: string;
  amount: number;
  transferDate: string;
  description: string | null;
  status: TransferStatus;
  createdAt: string;
}

export interface AccountTransferCreateResultDto {
  transfer: AccountTransferDto;
  insufficientFundsWarning: boolean;
}
