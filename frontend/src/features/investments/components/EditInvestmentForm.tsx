// src/features/investments/components/EditInvestmentForm.tsx
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { updateInvestmentSchema, type UpdateInvestmentFormValues } from '../schemas/investment.schema';
import { useUpdateInvestment } from '../hooks/useInvestments';
import type { Investment } from '../../../types/investment.types';
import { Button, Input } from '../../../components/ui';

interface Props {
  investment: Investment;
  onSuccess?: () => void;
  onCancel?: () => void;
}

export function EditInvestmentForm({ investment, onSuccess, onCancel }: Props) {
  const { mutate: updateInvestment, isPending } = useUpdateInvestment();

  const { register, handleSubmit, formState: { errors } } = useForm<UpdateInvestmentFormValues>({
    resolver: zodResolver(updateInvestmentSchema),
    defaultValues: {
      name: investment.name,
      type: investment.type as any,
      ticker: investment.ticker || '',
      broker: investment.broker || '',
      notes: investment.notes || '',
      // No editamos el monto inicial ni la fecha de compra en la actualización según reglas de negocio habituales,
      // pero si el DTO lo permite, lo agregamos. Asumiendo que updateInvestmentSchema lo maneja.
    }
  });

  const onSubmit = (data: UpdateInvestmentFormValues) => {
    const payload = {
      ...data,
      ticker: data.ticker?.trim() || undefined,
      broker: data.broker?.trim() || undefined,
      notes: data.notes?.trim() || undefined,
    };

    updateInvestment({ id: investment.id, data: payload }, {
      onSuccess: () => {
        if (onSuccess) onSuccess();
      }
    });
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
      {/* Estructura UI idéntica al CreateInvestmentForm, omitida por brevedad, adaptada al Update DTO */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <Input label="Nombre de la Inversión" error={errors.name?.message} {...register('name')} />
        <div className="flex flex-col space-y-1.5">
          <label className="text-sm font-medium text-[#7C756E]">Tipo de Activo</label>
          <select {...register('type')} className="w-full rounded-xl border border-[#EFEAE2] bg-[#FBF9F4] px-4 py-2.5 text-sm text-[#2C2A29] outline-none">
            <option value="etf">ETF</option>
            <option value="stock">Acción</option>
            <option value="mutualfund">Fondo Mutuo</option>
            <option value="crypto">Criptomoneda</option>
            <option value="bond">Bono</option>
            <option value="other">Otro</option>
          </select>
        </div>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <Input label="Ticker (Opcional)" error={errors.ticker?.message} {...register('ticker')} />
        <Input label="Broker / Plataforma (Opcional)" error={errors.broker?.message} {...register('broker')} />
      </div>
      <Input label="Notas Adicionales" error={errors.notes?.message} {...register('notes')} />

      <div className="mt-8 flex justify-end gap-3 pt-4 border-t border-[#EFEAE2]">
        {onCancel && (
          <Button type="button" variant="ghost" onClick={onCancel} className="text-[#7C756E] hover:bg-[#EFEAE2]">
            Cancelar
          </Button>
        )}
        <Button type="submit" isLoading={isPending} className="bg-[#2C2A29] text-[#FBF9F4] hover:bg-[#2C2A29]/90">
          Guardar Cambios
        </Button>
      </div>
    </form>
  );
}