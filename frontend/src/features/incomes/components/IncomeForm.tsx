import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ChevronDown } from 'lucide-react';
import { incomeSchema, type IncomeFormData } from '../schemas/income.schema';
import { Button, Input } from '../../../components/ui';
import { useCreateIncome, useUpdateIncome } from '../hooks/useIncomes';
import type { Income } from '../../../types/income.types';
import { Spinner } from '../../../components/ui';
import { useCategories } from '../../categories/hooks/useCategories';

interface Props {
  income?: Income; // si viene, el form entra en modo edición
  onSuccess?: () => void;
  onCancel?: () => void;
}

export function IncomeForm({ income, onSuccess, onCancel }: Props) {
  const isEditMode = Boolean(income);

  const { mutate: createIncome, isPending: isCreating } = useCreateIncome();
  const { mutate: updateIncome, isPending: isUpdating } = useUpdateIncome();
  const isPending = isEditMode ? isUpdating : isCreating;
  const { data: categories, isLoading: isLoadingCategories } = useCategories('income');


  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<IncomeFormData>({
    resolver: zodResolver(incomeSchema),
    defaultValues: income
      ? {
          categoryId: income.categoryId,
          amount: income.amount,
          description: income.description ?? '',
          date: income.date,
          source: income.source ?? '',
        }
      : {
          date: new Date().toISOString().split('T')[0],
        },
  });

  const onSubmit = (data: IncomeFormData) => {
    if (isEditMode && income) {
      updateIncome(
        { id: income.id, dto: data },
        { onSuccess: () => onSuccess?.() }
      );
    } else {
      createIncome(data, { onSuccess: () => onSuccess?.() });
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="flex flex-col space-y-1.5">
          <label className="text-sm font-medium text-[#2C2A29]">Categoría</label>
          <div className="relative">
            {isLoadingCategories ? (
              <div className="flex h-[42px] items-center rounded-xl border border-[#EFEAE2] bg-white/70 px-3">
                <Spinner size="sm" />
              </div>
            ) : (
              <>
                <select {...register('categoryId')} className="w-full appearance-none rounded-xl ...">
                  <option value="">Selecciona una...</option>
                  {categories?.map((cat) => (
                    <option key={cat.id} value={cat.id}>{cat.name}</option>
                  ))}
                </select>
                <ChevronDown className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[#7C756E]" strokeWidth={2} />
              </>
            )}
          </div>
          {errors.categoryId && (
            <span className="text-xs text-[#C97B63]">{errors.categoryId.message}</span>
          )}
        </div>

        <Input
          label="Monto"
          type="number"
          step="0.01"
          placeholder="0.00"
          error={errors.amount?.message}
          {...register('amount', { valueAsNumber: true })}
        />
      </div>

      <Input
        label="Fecha"
        type="date"
        error={errors.date?.message}
        {...register('date')}
      />

      <Input
        label="Fuente (Opcional)"
        placeholder="Ej: Cliente Empresa X"
        error={errors.source?.message}
        {...register('source')}
      />

      <Input
        label="Descripción (Opcional)"
        placeholder="Notas adicionales..."
        error={errors.description?.message}
        {...register('description')}
      />

      <div className="mt-6 flex justify-end gap-3 border-t border-[#EFEAE2] pt-5">
        {onCancel && (
          <Button type="button" variant="ghost" onClick={onCancel}>
            Cancelar
          </Button>
        )}
        <Button
          type="submit"
          isLoading={isPending}
          className="!bg-[#5C7A99] !text-white hover:!bg-[#4D6884] focus-visible:!ring-[#5C7A99]/30"
        >
          {isEditMode ? 'Guardar Cambios' : 'Guardar Ingreso'}
        </Button>
      </div>
    </form>
  );
}