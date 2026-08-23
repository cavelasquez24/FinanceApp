import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { createBudgetSchema, type CreateBudgetFormValues } from '../schemas/budget.schema';
import { useExpenseCategories } from '../../categories/hooks/useCategories';
import { useCreateBudget } from '../hooks/useBudget';
import { Button, Input } from '../../../components/ui';
import { Plus, Trash2, Wallet } from 'lucide-react';
import { cn } from '../../../utils/cn';

interface Props {
  month: number;
  year: number;
}

const selectClassName =
  'w-full rounded-xl border border-[#EFEAE2] bg-white/60 px-3 py-2.5 text-sm text-finflow-dark backdrop-blur-sm transition-all focus:border-finflow-blue focus:bg-white focus:outline-none focus:ring-2 focus:ring-finflow-blue/20';

export function CreateBudgetForm({ month, year }: Props) {
  const { data: categoriesResponse, isLoading: loadingCategories } = useExpenseCategories();
  const createBudget = useCreateBudget();

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<CreateBudgetFormValues>({
    resolver: zodResolver(createBudgetSchema),
    defaultValues: {
      month,
      year,
      categories: [{ categoryId: '', amountLimit: 0 }],
    },
  });

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'categories',
  });

  const onSubmit = (data: CreateBudgetFormValues) => {
    createBudget.mutate(data);
  };

  if (loadingCategories) {
    return (
      <div className="flex animate-pulse flex-col gap-4 rounded-[28px] border border-[#EFEAE2] bg-white/70 p-8 backdrop-blur-xl max-w-2xl">
        <div className="h-6 w-1/3 rounded-md bg-[#EFEAE2]/60"></div>
        <div className="h-4 w-1/4 rounded-md bg-[#EFEAE2]/60"></div>
        <div className="mt-4 h-12 w-full rounded-xl bg-[#EFEAE2]/60"></div>
      </div>
    );
  }

  // CORRECCIÓN: El hook ya devuelve Category[], por lo que no es necesario el .data.data
  const categories = categoriesResponse || [];

  return (
    <div className="rounded-[28px] border border-[#EFEAE2] bg-white/70 p-6 shadow-sm backdrop-blur-xl sm:p-8 max-w-2xl">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-8">
        
        {/* Header Block */}
        <div className="flex items-start gap-4 border-b border-[#EFEAE2]/70 pb-6">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-finflow-blue/10 text-finflow-blue">
            <Wallet className="h-6 w-6" strokeWidth={2} />
          </div>
          <div>
            <h2 className="font-serif text-2xl font-semibold text-finflow-dark">
              Configurar Presupuesto
            </h2>
            <p className="mt-1 text-sm font-medium text-finflow-muted">
              Periodo: {String(month).padStart(2, '0')}/{year}
            </p>
          </div>
        </div>

        {/* Dynamic Fields Block */}
        <div className="space-y-5">
          <div className="flex items-center justify-between">
            <h3 className="text-base font-medium text-finflow-dark">Categorías y Límites</h3>
            <Button
              type="button"
              variant="ghost"
              onClick={() => append({ categoryId: '', amountLimit: 0 })}
              className={cn(
                "h-9 px-3 text-sm font-medium text-finflow-blue",
                "hover:bg-finflow-blue/10 hover:text-[#4A6480] transition-colors rounded-xl"
              )}
            >
              <Plus className="mr-2 h-4 w-4" /> Agregar Rubro
            </Button>
          </div>

          <div className="space-y-4">
            {fields.map((field, index) => (
              <div 
                key={field.id} 
                className="group flex flex-col gap-3 sm:flex-row sm:items-start rounded-2xl bg-white/40 p-3 border border-transparent transition-colors hover:border-[#EFEAE2] hover:bg-white/80"
              >
                <div className="flex-1">
                  <select
                    {...register(`categories.${index}.categoryId` as const)}
                    defaultValue={field.categoryId}
                    className={selectClassName}
                  >
                    <option value="" disabled>Seleccione una categoría</option>
                    {categories.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                  </select>
                  {errors.categories?.[index]?.categoryId && (
                    <span className="mt-1 block text-xs font-medium text-finflow-rust">
                      {errors.categories[index]?.categoryId?.message}
                    </span>
                  )}
                </div>

                <div className="flex-1">
                  <Input
                    type="number"
                    step="0.01"
                    placeholder="Límite USD"
                    defaultValue={field.amountLimit}
                    className="!bg-white/60 focus:!bg-white"
                    {...register(`categories.${index}.amountLimit` as const, { valueAsNumber: true })}
                  />
                  {errors.categories?.[index]?.amountLimit && (
                    <span className="mt-1 block text-xs font-medium text-finflow-rust">
                      {errors.categories[index]?.amountLimit?.message}
                    </span>
                  )}
                </div>

                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => remove(index)}
                  disabled={fields.length === 1}
                  className="h-[42px] px-3 text-finflow-rust hover:bg-finflow-rust/10 disabled:opacity-40 rounded-xl"
                  aria-label="Eliminar rubro"
                >
                  <Trash2 className="h-5 w-5" />
                </Button>
              </div>
            ))}
          </div>
          
          {errors.categories && typeof errors.categories.message === 'string' && (
            <p className="rounded-xl bg-finflow-rust/10 px-4 py-2.5 text-sm font-medium text-finflow-rust">
              {errors.categories.message}
            </p>
          )}
        </div>

        {/* Footer Block */}
        <div className="space-y-6 pt-2">
          <Input 
            label="Notas adicionales (Opcional)" 
            className="!bg-white/60 focus:!bg-white"
            {...register('notes')} 
          />

          <Button
            type="submit"
            className="w-full h-12 rounded-xl !bg-finflow-dark text-base font-medium !text-finflow-cream shadow-md transition-all hover:!bg-[#1F1E1D] hover:shadow-lg disabled:opacity-70"
            disabled={createBudget.isPending}
          >
            {createBudget.isPending ? 'Guardando Presupuesto...' : 'Crear Presupuesto'}
          </Button>
        </div>
      </form>
    </div>
  );
}