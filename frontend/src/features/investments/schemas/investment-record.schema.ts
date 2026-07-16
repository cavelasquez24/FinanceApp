import { z } from 'zod';
import { isValid, parse } from 'date-fns';

export const investmentRecordSchema = z.object({
  recordDate: z.string().refine((val) => {
    const parsedDate = parse(val, 'yyyy-MM-dd', new Date());
    return isValid(parsedDate);
  }, {
    message: "Fecha inválida. Use el formato YYYY-MM-DD",
  }),

  value: z.number("El valor es requerido")
    .min(0.01, "El valor debe ser mayor a cero"),

  dividends: z.number()
    .min(0, "Los dividendos no pueden ser negativos")
    .default(0),

  notes: z.string()
    .max(500, "Las notas no pueden exceder 500 caracteres")
    .optional(),
});

export type InvestmentRecordFormValues = z.input<typeof investmentRecordSchema>;

export type InvestmentRecordValidatedValues = z.output<typeof investmentRecordSchema>;