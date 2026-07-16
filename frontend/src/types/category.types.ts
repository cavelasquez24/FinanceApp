export interface Category {
  id: string;
  name: string;
  type: 'income' | 'expense' | 'both';
  icon: string | null;
  color: string;
  isDefault: boolean;
}