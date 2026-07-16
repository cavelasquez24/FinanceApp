export interface Income {
  id: string;
  categoryId: string;
  categoryName: string;
  categoryColor: string;
  categoryIcon: string | null;
  amount: number;
  description: string | null;
  date: string;
  source: string | null;
  createdAt: string;
}

export interface IncomeCreateDto {
  categoryId: string;
  amount: number;
  description?: string;
  date: string;
  source?: string;
}

export interface IncomeUpdateDto extends IncomeCreateDto {}

export interface IncomeFilter {
  page?: number;
  pageSize?: number;
  categoryId?: string;
  startDate?: string;
  endDate?: string;
  minAmount?: number;
  maxAmount?: number;
  search?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface IncomeSummary {
  totalAmount: number;
  totalCount: number;
  byCategory: IncomeByCategoryDto[];
}

export interface IncomeByCategoryDto {
  categoryName: string;
  categoryColor: string;
  amount: number;
  percentage: number;
}