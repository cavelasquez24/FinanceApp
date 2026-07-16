import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { createBudgetSchema, type CreateBudgetFormValues } from '../schemas/budget.schema';
import { useExpenseCategories } from '../../categories/hooks/useCategories';
import { useUpdateBudget } from '../hooks/useBudget';
import { Button, Input, Card } from '../../../components/ui';
import { Plus, Trash2 } from 'lucide-react';

interface Props {
  budgetId: string;
  currentData: {
    month: number;
    year: number;
    totalLimit?: number;
    notes?: string;
    categories: {
      categoryId: string;
      amountLimit: number;
    }[];
  };
  onSuccess: () => void;
}

const selectClassName =
  'w-full rounded-xl border border-[#EFEAE2] bg-white/70 px-3 py-2.5 text-sm text-[#2C2A29] backdrop-blur-sm transition-colors focus:border-[#5C7A99] focus:outline-none focus:ring-2 focus:ring-[#5C7A99]/20';

export function EditBudgetForm({ budgetId, currentData, onSuccess }: Props) {
  const { data: categoriesResponse, isLoading: loadingCategories } = useExpenseCategories();
  const updateBudget = useUpdateBudget(budgetId);

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<CreateBudgetFormValues>({
    resolver: zodResolver(createBudgetSchema),
    defaultValues: {
      month: currentData.month,
      year: currentData.year,
      totalLimit: currentData.totalLimit,
      notes: currentData.notes,
      categories: currentData.categories.map((c) => ({
        categoryId: c.categoryId,
        amountLimit: c.amountLimit,
      })),
    },
  });

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'categories',
  });

  const onSubmit = (data: CreateBudgetFormValues) => {
    // El backend (BudgetUpdateDto) no acepta month/year — el periodo no se edita.
    // Armamos el payload explícito para que el tipo coincida con el contrato real.
    updateBudget.mutate(
      {
        totalLimit: data.totalLimit,
        notes: data.notes,
        categories: data.categories,
      },
      {
        onSuccess: () => onSuccess(),
      }
    );
  };

  if (loadingCategories) return <div className="text-sm text-[#7C756E]">Cargando categorías...</div>;

  // FIX: useExpenseCategories ya retorna Category[] desanidado (response.data.data dentro del hook).
  // El .data.data adicional aquí era el bug: siempre devolvía [] y dejaba el select vacío.
  const categories = categoriesResponse || [];

  return (
    <Card className="max-w-2xl">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
        <div>
          <h2 className="font-serif text-xl font-medium text-[#2C2A29]">Editar Presupuesto</h2>
          <p className="text-sm text-[#7C756E]">
            Periodo: {currentData.month}/{currentData.year}
          </p>
        </div>

        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-base font-medium text-[#2C2A29]">Categorías y Límites</h3>
            <Button
              type="button"
              variant="ghost"
              onClick={() => append({ categoryId: '', amountLimit: 0 })}
              className="!text-[#8FA888] hover:!bg-[#8FA888]/10"
            >
              <Plus className="mr-2 h-4 w-4" /> Agregar Rubro
            </Button>
          </div>

          {fields.map((field, index) => (
            <div key={field.id} className="flex items-start gap-4">
              <div className="flex-1">
                <select
                  {...register(`categories.${index}.categoryId` as const)}
                  defaultValue={field.categoryId}
                  className={selectClassName}
                >
                  <option value="">Seleccione una categoría</option>
                  {categories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
                {errors.categories?.[index]?.categoryId && (
                  <span className="text-xs text-[#C97B63]">
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
                  {...register(`categories.${index}.amountLimit` as const, { valueAsNumber: true })}
                />
                {errors.categories?.[index]?.amountLimit && (
                  <span className="text-xs text-[#C97B63]">
                    {errors.categories[index]?.amountLimit?.message}
                  </span>
                )}
              </div>

              <Button
                type="button"
                variant="danger"
                onClick={() => remove(index)}
                disabled={fields.length === 1}
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </div>
          ))}
        </div>

        <div className="border-t border-[#EFEAE2] pt-4">
          <Input label="Notas adicionales (Opcional)" {...register('notes')} />
        </div>

        <Button
          type="submit"
          className="w-full !bg-[#2C2A29] !text-[#FBF9F4] hover:!bg-[#1F1E1D]"
          disabled={updateBudget.isPending}
        >
          {updateBudget.isPending ? 'Guardando...' : 'Guardar Cambios'}
        </Button>
      </form>
    </Card>
  );
}