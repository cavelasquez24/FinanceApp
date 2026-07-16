import { z } from 'zod';

export const incomeSchema = z.object({
  categoryId: z.string().min(1, 'La categoría es requerida'),
  amount: z
  .number()
  .positive('El monto debe ser mayor a 0'),
  description: z.string().max(500, 'Máximo 500 caracteres').optional(),
  date: z.string().min(1, 'La fecha es requerida'), // Formato YYYY-MM-DD
  source: z.string().max(200, 'Máximo 200 caracteres').optional(),
});

export type IncomeFormData = z.infer<typeof incomeSchema>;