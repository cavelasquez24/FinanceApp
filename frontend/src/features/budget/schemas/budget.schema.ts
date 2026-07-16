import { z } from 'zod';

// Regex genérico para cualquier formato de UUID (36 caracteres)
const uuidRegex = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/i;

export const createBudgetSchema = z.object({
  month: z.number().min(1).max(12),
  year: z.number().min(2000),
  totalLimit: z.number().optional(),
  notes: z.string().optional(),
  categories: z.array(
    z.object({
      // Cambiamos .uuid() por .regex()
      categoryId: z.string().regex(uuidRegex, { message: 'Seleccione una categoría válida' }),
      amountLimit: z.number().min(0.01, { message: 'El límite debe ser mayor a 0' })
    })
  ).min(1, { message: 'Agregue al menos un rubro' })
});

export type CreateBudgetFormValues = z.infer<typeof createBudgetSchema>;