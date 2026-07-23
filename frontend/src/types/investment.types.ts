export type InvestmentType = 'etf' | 'stock' | 'mutualfund' | 'crypto' | 'bond' | 'other';

export interface Investment {
  id: string;
  name: string;
  type: InvestmentType;
  ticker: string | null;
  broker: string | null;
  initialAmount: number;
  currentValue: number;
  gainLoss: number;
  gainLossPercentage: number;
  purchaseDate: string; // "YYYY-MM-DD"
  isActive: boolean;
  notes: string | null;
  createdAt: string;
}

export interface InvestmentSummary {
  totalInvested: number;
  currentValue: number;
  totalGain: number;
  totalGainPercentage: number;
  totalDividends: number;
  byType: {
    type: InvestmentType;
    currentValue: number;
    percentage: number;
  }[];
}

export interface InvestmentRecord {
  id: string;
  investmentId: string;
  recordDate: string; // "YYYY-MM-DD"
  value: number;
  dividends: number;
  notes: string | null;
}

export interface CreateInvestmentDto {
  name: string;
  type: InvestmentType;
  ticker?: string; 
  broker?: string;
  initialAmount: number;
  currentValue?: number;
  purchaseDate: string; // "YYYY-MM-DD"
  isHistoricalImport: boolean;
  notes?: string;
}

export interface UpdateInvestmentDto {
  name: string;
  type?: InvestmentType;
  ticker?: string;
  broker?: string;
  isActive?: boolean;
  notes?: string;
}

export interface CreateInvestmentRecordDto {
  recordDate: string; // "YYYY-MM-DD"
  value: number;
  dividends?: number; // default 0
  notes?: string;
}

export interface InvestmentContribution {
  id: string;
  contributionDate: string; // "YYYY-MM-DD"
  amount: number;
  notes: string | null;
  createdAt: string;
}

export interface CreateInvestmentContributionDto {
  contributionDate?: string; // "YYYY-MM-DD", opcional → hoy si se omite
  amount: number;            // > 0
  notes?: string;
}