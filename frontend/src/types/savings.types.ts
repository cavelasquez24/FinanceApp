import { type ApiResponse } from './api.types';

export interface SavingsGoal {
  id: string;
  name: string;
  description: string | null;
  targetAmount: number;
  currentAmount: number;
  remainingAmount: number;
  progressPercentage: number;
  targetDate: string | null;
  isCompleted: boolean;
  icon: string | null;
  estimatedMonthsToComplete: number | null;
  createdAt: string;
}

export interface CreateSavingsGoalDto {
  name: string;
  description?: string;
  targetAmount: number;
  targetDate?: string; // Formato "YYYY-MM-DD"
  icon?: string;
}

export interface DepositDto {
  amount: number;
  notes?: string;
}