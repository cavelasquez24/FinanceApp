export interface Tag {
  id: string;
  name: string;
  color: string | null;
  usageCount: number;
  lastUsedAt: string | null;
}

export interface TagExpenseReportItem {
  tagId: string;
  name: string;
  color: string | null;
  totalAmount: number;
  expenseCount: number;
  averageAmount: number;
}

export interface TagExpenseReport {
  startDate: string;
  endDate: string;
  totalExpenses: number;
  taggedExpenses: number;
  untaggedExpenses: number;
  coveragePercentage: number;
  tags: TagExpenseReportItem[];
}
