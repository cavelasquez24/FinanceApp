import { z } from 'zod';

export const categorySchema = z.object({
  name: z.string().min(2, 'El nombre debe tener al menos 2 caracteres').max(50, 'Máximo 50 caracteres'),
  type: z.enum(['income', 'expense', 'both'], {
    message: 'Debe seleccionar el tipo de categoría',
  }),
  color: z.string().regex(/^#[0-9A-Fa-f]{6}$/, 'Color hexadecimal inválido'),
  icon: z.string().optional(), // Eliminamos .nullable()
});

export type CategoryFormValues = z.infer<typeof categorySchema>;