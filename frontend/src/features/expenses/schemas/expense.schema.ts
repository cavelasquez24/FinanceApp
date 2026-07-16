// src/features/expenses/schemas/expense.schema.ts
import { z } from 'zod';

export const expenseSchema = z.object({
  categoryId: z.string().min(1, 'La categoría es requerida'),
  amount: z
  .number()
  .positive('El monto debe ser mayor a 0'),
  description: z.string().max(500, 'Máximo 500 caracteres').optional(),
  date: z.string().min(1, 'La fecha es requerida'),
    paymentMethod: z
  .string()
  .min(1, 'Selecciona un método de pago válido'),
  isRecurring: z.boolean().default(false),
  recurrenceType: z.enum(['daily', 'weekly', 'biweekly', 'monthly', 'yearly']).nullable().optional(),
  notes: z.string().max(1000, 'Máximo 1000 caracteres').optional(),
}).superRefine((data, ctx) => {
  // Regla estricta del backend: si es recurrente, el tipo es obligatorio
  if (data.isRecurring && !data.recurrenceType) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Selecciona el tipo de recurrencia',
      path: ['recurrenceType'],
    });
  }
});

export type ExpenseFormData = z.infer<typeof expenseSchema>;