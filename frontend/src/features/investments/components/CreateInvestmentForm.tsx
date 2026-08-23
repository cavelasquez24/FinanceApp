import { todayDateOnly } from '../../../utils/dateOnly';
// src/features/investments/components/CreateInvestmentForm.tsx
import { useForm, useWatch, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { createInvestmentSchema, type CreateInvestmentFormValues } from '../schemas/investment.schema';
import { useCreateInvestment } from '../hooks/useInvestments';
import { Button, Input } from '../../../components/ui';
import { formatCurrency } from '../../../utils/formatCurrency';
import { Plus, Trash2 } from 'lucide-react';

interface Props {
  onSuccess?: () => void;
  onCancel?: () => void;
}

export function CreateInvestmentForm({ onSuccess, onCancel }: Props) {
  const { mutate: createInvestment, isPending } = useCreateInvestment();

  const { register, handleSubmit, control, formState: { errors } } = useForm<CreateInvestmentFormValues>({
    resolver: zodResolver(createInvestmentSchema),
    defaultValues: {
      type: 'etf',
      purchaseDate: todayDateOnly(),
      isHistoricalImport: false,
      isConsolidatedSnapshot: false,
      historicalContributions: [],
    }
  });

  const { fields, append, remove } = useFieldArray({ control, name: 'historicalContributions' });

  const [isHistoricalImport, isConsolidatedSnapshot, historicalContributions] = useWatch({
    control,
    name: ['isHistoricalImport', 'isConsolidatedSnapshot', 'historicalContributions'],
  });

  const showHistoricalContributions = isHistoricalImport && !isConsolidatedSnapshot;
  const capitalTotal = (historicalContributions ?? []).reduce((sum, c) => sum + (Number(c?.amount) || 0), 0);

  const onSubmit = (data: CreateInvestmentFormValues) => {
    const payload = {
      ...data,
      ticker: data.ticker?.trim() || undefined,
      broker: data.broker?.trim() || undefined,
      notes: data.notes?.trim() || undefined,
      historicalContributions: showHistoricalContributions && (data.historicalContributions?.length ?? 0) > 0
        ? data.historicalContributions
        : undefined,
    };

    createInvestment(payload, {
      onSuccess: () => {
        if (onSuccess) onSuccess();
      }
    });
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <Input
          label="Nombre de la Inversión"
          placeholder="Ej: S&P 500"
          error={errors.name?.message}
          {...register('name')}
        />

        <div className="flex flex-col space-y-1.5">
          <label className="text-sm font-medium text-finflow-muted">Tipo de Activo</label>
          <select
            {...register('type')}
            className="w-full rounded-xl border border-[#EFEAE2] bg-finflow-cream px-4 py-2.5 text-sm text-finflow-dark outline-none transition-all focus:border-finflow-muted focus:ring-1 focus:ring-finflow-muted"
          >
            <option value="etf">ETF</option>
            <option value="stock">Acción</option>
            <option value="mutualfund">Fondo Mutuo</option>
            <option value="crypto">Criptomoneda</option>
            <option value="bond">Bono</option>
            <option value="other">Otro</option>
          </select>
          {errors.type && <span className="text-xs text-finflow-rust">{errors.type.message}</span>}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <Input
          label="Ticker (Opcional)"
          placeholder="Ej: VOO, AAPL"
          error={errors.ticker?.message}
          {...register('ticker')}
        />
        <Input
          label="Broker / Plataforma (Opcional)"
          placeholder="Ej: Interactive Brokers, Binance"
          error={errors.broker?.message}
          {...register('broker')}
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <label className="flex cursor-pointer items-start gap-3 rounded-2xl border border-[#EFEAE2] bg-white/60 p-4 md:col-span-2">
          <input
            type="checkbox"
            className="mt-1 rounded border-[#D8D1C7] text-finflow-dark focus:ring-finflow-muted"
            {...register('isHistoricalImport')}
          />
          <span>
            <span className="block text-sm font-medium text-finflow-dark">Importar inversión histórica</span>
            <span className="mt-1 block text-xs leading-relaxed text-finflow-muted">
              Actívalo si la compra ocurrió antes de usar FinFlow. La importación establece el portafolio,
              pero no consume el disponible del ciclo.
            </span>
          </span>
        </label>

        <Input
          label="Monto Inicial Invertido"
          type="number"
          step="0.01"
          placeholder="0.00"
          error={errors.initialAmount?.message}
          {...register('initialAmount', { valueAsNumber: true })}
        />
        <Input
          label="Fecha de Compra"
          type="date"
          error={errors.purchaseDate?.message}
          {...register('purchaseDate')}
        />
      </div>

      {/* Opciones adicionales para importación histórica */}
      {isHistoricalImport && (
        <div className="space-y-4">
          <Input
            label="Valor Actual de Mercado"
            type="number"
            step="0.01"
            min="0"
            placeholder="Si se omite, usa el capital base"
            error={errors.currentValue?.message}
            {...register('currentValue', { setValueAs: (value) => value === '' ? undefined : Number(value) })}
          />

          <label className="flex cursor-pointer items-start gap-3 rounded-2xl border border-[#EFEAE2] bg-white/60 p-4">
            <input
              type="checkbox"
              className="mt-1 rounded border-[#D8D1C7] text-finflow-dark focus:ring-finflow-muted"
              {...register('isConsolidatedSnapshot')}
            />
            <span>
              <span className="block text-sm font-medium text-finflow-dark">Snapshot consolidado</span>
              <span className="mt-1 block text-xs leading-relaxed text-finflow-muted">
                Registra un valor de portafolio sin detallar aportes individuales. Útil cuando no tienes el historial completo.
              </span>
            </span>
          </label>

          {/* Lista de aportes históricos (solo si no es snapshot) */}
          {showHistoricalContributions && (
            <div className="rounded-2xl border border-[#EFEAE2] bg-white/40 p-4 space-y-3">
              <div className="flex items-center justify-between">
                <p className="text-sm font-semibold text-finflow-dark">Aportes Históricos</p>
                {capitalTotal > 0 && (
                  <span className="text-xs font-medium text-finflow-muted">
                    Total: <span className="font-bold text-finflow-dark">{formatCurrency(capitalTotal)}</span>
                  </span>
                )}
              </div>
              <p className="text-xs text-finflow-muted">
                Detalla cada aporte individual para tener trazabilidad completa del capital base.
              </p>

              {fields.map((field, index) => (
                <div key={field.id} className="grid grid-cols-[1fr_1fr_auto] gap-3 items-end">
                  <Input
                    label={index === 0 ? 'Fecha' : undefined}
                    type="date"
                    error={errors.historicalContributions?.[index]?.contributionDate?.message}
                    {...register(`historicalContributions.${index}.contributionDate`)}
                  />
                  <Input
                    label={index === 0 ? 'Monto' : undefined}
                    type="number"
                    step="0.01"
                    min="0.01"
                    placeholder="0.00"
                    error={errors.historicalContributions?.[index]?.amount?.message}
                    {...register(`historicalContributions.${index}.amount`, { valueAsNumber: true })}
                  />
                  <button
                    type="button"
                    onClick={() => remove(index)}
                    className="mb-0.5 p-2 text-finflow-muted hover:text-finflow-rust hover:bg-finflow-rust/10 rounded-xl transition-all"
                    title="Eliminar aporte"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              ))}

              <button
                type="button"
                onClick={() => append({ contributionDate: todayDateOnly(), amount: 0 })}
                className="flex items-center gap-2 text-sm text-finflow-muted hover:text-finflow-dark transition-colors"
              >
                <Plus className="h-4 w-4" />
                Agregar aporte
              </button>
            </div>
          )}
        </div>
      )}

      <Input
        label="Notas Adicionales (Opcional)"
        placeholder="Estrategia, objetivo, etc."
        error={errors.notes?.message}
        {...register('notes')}
      />

      <div className="mt-8 flex justify-end gap-3 pt-4 border-t border-[#EFEAE2]">
        {onCancel && (
          <Button type="button" variant="ghost" onClick={onCancel} className="text-finflow-muted hover:bg-[#EFEAE2]">
            Cancelar
          </Button>
        )}
        <Button type="submit" isLoading={isPending} className="bg-finflow-dark text-finflow-cream hover:bg-finflow-dark/90">
          Guardar Inversión
        </Button>
      </div>
    </form>
  );
}
