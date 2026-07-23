// src/features/investments/components/CreateInvestmentForm.tsx
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { createInvestmentSchema, type CreateInvestmentFormValues } from '../schemas/investment.schema';
import { useCreateInvestment } from '../hooks/useInvestments';
import { Button, Input } from '../../../components/ui';

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
      purchaseDate: new Date().toISOString().split('T')[0],
      isHistoricalImport: false,
    }
  });

  const isHistoricalImport = useWatch({ control, name: 'isHistoricalImport' });

  const onSubmit = (data: CreateInvestmentFormValues) => {
    const payload = {
      ...data,
      ticker: data.ticker?.trim() || undefined,
      broker: data.broker?.trim() || undefined,
      notes: data.notes?.trim() || undefined,
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
          <label className="text-sm font-medium text-[#7C756E]">Tipo de Activo</label>
          <select 
            {...register('type')}
            className="w-full rounded-xl border border-[#EFEAE2] bg-[#FBF9F4] px-4 py-2.5 text-sm text-[#2C2A29] outline-none transition-all focus:border-[#7C756E] focus:ring-1 focus:ring-[#7C756E]"
          >
            {/* Contrato: enums strictly in snake_case */}
            <option value="etf">ETF</option>
            <option value="stock">Acción</option>
            <option value="mutualfund">Fondo Mutuo</option>
            <option value="crypto">Criptomoneda</option>
            <option value="bond">Bono</option>
            <option value="other">Otro</option>
          </select>
          {errors.type && <span className="text-xs text-[#C97B63]">{errors.type.message}</span>}
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
            className="mt-1 rounded border-[#D8D1C7] text-[#2C2A29] focus:ring-[#7C756E]"
            {...register('isHistoricalImport')}
          />
          <span>
            <span className="block text-sm font-medium text-[#2C2A29]">Importar inversión histórica</span>
            <span className="mt-1 block text-xs leading-relaxed text-[#7C756E]">
              Actívalo si la compra ocurrió antes de usar FinanceApp. La importación establece el portafolio,
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
      {isHistoricalImport && (
        <Input
          label="Valor Actual"
          type="number"
          step="0.01"
          min="0"
          placeholder="Si se omite, usa el capital base"
          error={errors.currentValue?.message}
          {...register('currentValue', { setValueAs: (value) => value === '' ? undefined : Number(value) })}
        />
      )}

      <Input 
        label="Notas Adicionales (Opcional)"
        placeholder="Estrategia, objetivo, etc."
        error={errors.notes?.message}
        {...register('notes')}
      />

      <div className="mt-8 flex justify-end gap-3 pt-4 border-t border-[#EFEAE2]">
        {onCancel && (
          <Button type="button" variant="ghost" onClick={onCancel} className="text-[#7C756E] hover:bg-[#EFEAE2]">
            Cancelar
          </Button>
        )}
        <Button type="submit" isLoading={isPending} className="bg-[#2C2A29] text-[#FBF9F4] hover:bg-[#2C2A29]/90">
          Guardar Inversión
        </Button>
      </div>
    </form>
  );
}