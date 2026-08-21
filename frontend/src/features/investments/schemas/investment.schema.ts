import { z } from 'zod';

const investmentTypes = ['etf', 'stock', 'mutualfund', 'crypto', 'bond', 'other'] as const;

const investmentBaseSchema = {
  name: z.string().min(1, 'El nombre es obligatorio').max(100, 'El nombre es muy largo'),
  type: z.enum(investmentTypes, { message: 'Tipo de inversión no válido' }),
  ticker: z.string().max(20, 'El ticker es muy largo').optional(),
  broker: z.string().max(100, 'El nombre del broker es muy largo').optional(),
  notes: z.string().max(500, 'Las notas son muy largas').optional(),
};

const historicalContributionSchema = z.object({
  contributionDate: z.string().min(1, 'Fecha requerida'),
  amount: z.number({ message: 'Monto requerido' }).positive('Debe ser mayor a cero'),
  notes: z.string().max(500).optional(),
});

export const createInvestmentSchema = z.object({
  ...investmentBaseSchema,
  initialAmount: z
    .number({ message: 'El monto inicial debe ser numérico' })
    .positive('El monto debe ser mayor a cero'),
  currentValue: z.number({ message: 'El valor actual debe ser numérico' })
    .nonnegative('El valor actual no puede ser negativo')
    .optional(),
  isHistoricalImport: z.boolean(),
  isConsolidatedSnapshot: z.boolean().optional(),
  historicalContributions: z.array(historicalContributionSchema).optional(),
  purchaseDate: z.string().min(1, 'La fecha de compra es obligatoria'),
});

export const updateInvestmentSchema = z.object({
  ...investmentBaseSchema,
});

export type CreateInvestmentFormValues = z.infer<typeof createInvestmentSchema>;
export type UpdateInvestmentFormValues = z.infer<typeof updateInvestmentSchema>;
export type HistoricalContributionFormValues = z.infer<typeof historicalContributionSchema>;
