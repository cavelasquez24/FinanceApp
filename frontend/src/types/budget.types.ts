export interface BudgetCategoryInput {
  categoryId: string;
  amountLimit: number;
}

export interface CreateBudgetDto {
  month: number;
  year: number;
  totalLimit?: number;
  notes?: string;
  categories: BudgetCategoryInput[];
}

// Refleja BudgetUpdateDto del backend: sin month/year (el periodo no se edita, solo se reemplaza vía PUT)
export interface BudgetUpdateDto {
  totalLimit?: number;
  notes?: string;
  categories: BudgetCategoryInput[];
}