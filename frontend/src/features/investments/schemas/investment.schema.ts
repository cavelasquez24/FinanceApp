import { z } from 'zod';

// Contrato de Backend: "etf" | "stock" | "mutualfund" | "crypto" | "bond" | "other"[cite: 1]
const investmentTypes = ['etf', 'stock', 'mutualfund', 'crypto', 'bond', 'other'] as const;

// 1. Extraemos las propiedades compartidas entre Creación y Edición
const investmentBaseSchema = {
  name: z.string().min(1, 'El nombre es obligatorio').max(100, 'El nombre es muy largo'),
  type: z.enum(investmentTypes, {
    message: 'Tipo de inversión no válido',
  }),
  ticker: z.string().max(20, 'El ticker es muy largo').optional(),
  broker: z.string().max(100, 'El nombre del broker es muy largo').optional(),
  notes: z.string().max(500, 'Las notas son muy largas').optional(),
};

// 2. Esquema para CREAR (incluye fecha inicial y monto)
export const createInvestmentSchema = z.object({
  ...investmentBaseSchema,
  initialAmount: z
    .number({ message: 'El monto inicial debe ser numérico' })
    .positive('El monto debe ser mayor a cero'),
  // El frontend envía YYYY-MM-DD[cite: 1]
  currentValue: z.number({ message: 'El valor actual debe ser numérico' })
    .nonnegative('El valor actual no puede ser negativo')
    .optional(),
  isHistoricalImport: z.boolean(),
  purchaseDate: z.string().min(1, 'La fecha de compra es obligatoria'),
});

// 3. Esquema para ACTUALIZAR (solo permite editar metadatos)
export const updateInvestmentSchema = z.object({
  ...investmentBaseSchema,
});

// 4. Exportamos los tipos inferidos para usarlos en useForm<T>
export type CreateInvestmentFormValues = z.infer<typeof createInvestmentSchema>;
export type UpdateInvestmentFormValues = z.infer<typeof updateInvestmentSchema>;