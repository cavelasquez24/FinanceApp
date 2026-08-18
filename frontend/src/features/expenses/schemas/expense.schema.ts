// src/features/expenses/schemas/expense.schema.ts
import { z } from 'zod';

export const expenseSchema = z.object({
  categoryId: z.string().min(1, 'La categoría es requerida'),
  accountId: z.string().optional(),
  creditCardId: z.string().optional(),
  amount: z
  .number()
  .positive('El monto debe ser mayor a 0'),
  description: z.string().max(500, 'Máximo 500 caracteres').optional(),
  merchant: z.string().max(200, 'Máximo 200 caracteres').optional(),
  tagIds: z.array(z.string()).max(10, 'Máximo 10 etiquetas').default([]),
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
  if (data.paymentMethod === 'credit_card') {
    if (!data.creditCardId) ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Selecciona la tarjeta usada en la compra.',
      path: ['creditCardId'],
    });
    if (data.accountId) ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Una compra con tarjeta no descuenta una cuenta líquida.',
      path: ['accountId'],
    });
  } else {
    if (!data.accountId) ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Selecciona la cuenta desde la que se pagó.',
      path: ['accountId'],
    });
    if (data.creditCardId) ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'La tarjeta solo aplica al método tarjeta de crédito.',
      path: ['creditCardId'],
    });
  }
});

export type ExpenseFormData = z.infer<typeof expenseSchema>;