import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { categorySchema, type CategoryFormValues } from '../schemas/category.schema';
import { type Category } from '../../../types/category.types';
import { useCreateCategory, useUpdateCategory } from '../hooks/useCategories';
import { Button, Input } from '../../../components/ui';
import { X } from 'lucide-react';
import { cn } from '../../../utils/cn';
import { CATEGORY_ICONS, DEFAULT_ICON_KEY } from '../utils/iconRegistry';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  categoryToEdit?: Category | null;
}

const PRESET_COLORS = [
  '#EF4444', '#F97316', '#EAB308', '#22C55E', '#14B8A6',
  '#3B82F6', '#6366F1', '#8B5CF6', '#D946EF', '#6B7280'
];

export function CategoryFormModal({ isOpen, onClose, categoryToEdit }: Props) {
  const createMutation = useCreateCategory();
  const updateMutation = useUpdateCategory();

  const isEditing = !!categoryToEdit;

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<CategoryFormValues>({
    resolver: zodResolver(categorySchema),
    defaultValues: {
      name: '',
      type: 'expense',
      color: PRESET_COLORS[0],
      icon: DEFAULT_ICON_KEY,
    },
  });

  const selectedColor = watch('color');
  const selectedIcon = watch('icon') || DEFAULT_ICON_KEY;

  useEffect(() => {
    if (categoryToEdit && isOpen) {
      reset({
        name: categoryToEdit.name,
        type: categoryToEdit.type,
        color: categoryToEdit.color,
        icon: categoryToEdit.icon ?? DEFAULT_ICON_KEY, 
      });
    } else if (!isOpen) {
      reset({
        name: '',
        type: 'expense',
        color: PRESET_COLORS[0],
        icon: DEFAULT_ICON_KEY,
      });
    }
  }, [categoryToEdit, isOpen, reset]);

  if (!isOpen) return null;

  const onSubmit = (data: CategoryFormValues) => {
    const payload = {
      ...data,
      icon: data.icon ?? undefined,
    };

    if (isEditing && categoryToEdit) {
      updateMutation.mutate(
        { id: categoryToEdit.id, data: payload },
        { onSuccess: () => onClose() }
      );
    } else {
      createMutation.mutate(payload, { onSuccess: () => onClose() });
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-[#2C2A29]/40 p-4 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="w-full max-w-md max-h-[90vh] overflow-y-auto rounded-[28px] border border-[#EFEAE2] bg-white/90 p-6 sm:p-8 shadow-xl backdrop-blur-xl animate-in zoom-in-95 duration-200 custom-scrollbar">
        <div className="flex items-center justify-between mb-6">
          <h2 className="font-serif text-xl font-semibold text-[#2C2A29]">
            {isEditing ? 'Editar Categoría' : 'Nueva Categoría'}
          </h2>
          <button onClick={onClose} className="rounded-full p-2 text-[#7C756E] hover:bg-[#EFEAE2]/60 transition-colors">
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <div className="space-y-5">
            <div>
              <Input
                label="Nombre de la categoría"
                placeholder="Ej. Alimentación"
                className="!bg-white/60 focus:!bg-white"
                {...register('name')}
              />
              {errors.name && <span className="text-xs text-[#C97B63] mt-1 block">{errors.name.message}</span>}
            </div>

            <div>
              <label className="block text-sm font-medium text-[#2C2A29] mb-1.5">Tipo de Flujo</label>
              <select
                {...register('type')}
                className="w-full rounded-xl border border-[#EFEAE2] bg-white/60 px-3 py-2.5 text-sm text-[#2C2A29] backdrop-blur-sm transition-all focus:border-[#5C7A99] focus:bg-white focus:outline-none focus:ring-2 focus:ring-[#5C7A99]/20"
              >
                <option value="expense">Gasto</option>
                <option value="income">Ingreso</option>
                <option value="both">Ambos</option>
              </select>
              {errors.type && <span className="text-xs text-[#C97B63] mt-1 block">{errors.type.message}</span>}
            </div>

            <div>
              <label className="block text-sm font-medium text-[#2C2A29] mb-2">Color Distintivo</label>
              <div className="flex flex-wrap gap-3">
                {PRESET_COLORS.map((color) => (
                  <button
                    key={color}
                    type="button"
                    onClick={() => setValue('color', color, { shouldValidate: true })}
                    className={cn(
                      "h-8 w-8 rounded-full transition-all duration-200 hover:scale-110",
                      selectedColor === color ? "ring-2 ring-offset-2 ring-[#5C7A99] scale-110" : "ring-1 ring-black/10"
                    )}
                    style={{ backgroundColor: color }}
                    aria-label={`Seleccionar color ${color}`}
                  />
                ))}
              </div>
              {errors.color && <span className="text-xs text-[#C97B63] mt-1 block">{errors.color.message}</span>}
            </div>

            {/* Nueva sección de iconos */}
            <div>
              <label className="block text-sm font-medium text-[#2C2A29] mb-2">Icono Representativo</label>
              <div className="flex flex-wrap gap-2 rounded-2xl bg-white/50 p-3 border border-[#EFEAE2]/60">
                {Object.entries(CATEGORY_ICONS).map(([key, IconComponent]) => (
                  <button
                    key={key}
                    type="button"
                    onClick={() => setValue('icon', key, { shouldValidate: true })}
                    className={cn(
                      "p-2.5 rounded-xl transition-all duration-200 flex items-center justify-center",
                      selectedIcon === key 
                        ? "bg-white shadow-sm ring-1 ring-[#EFEAE2] scale-105" 
                        : "text-[#7C756E] hover:bg-white/60 hover:text-[#2C2A29]"
                    )}
                    style={selectedIcon === key ? { color: selectedColor } : {}}
                    title={key}
                  >
                    <IconComponent className="h-5 w-5" strokeWidth={2} />
                  </button>
                ))}
              </div>
              {errors.icon && <span className="text-xs text-[#C97B63] mt-1 block">{errors.icon.message}</span>}
            </div>
          </div>

          <div className="pt-4 border-t border-[#EFEAE2]/60">
            <Button
              type="submit"
              disabled={isPending}
              className="w-full h-11 rounded-xl !bg-[#2C2A29] text-sm font-medium !text-[#FBF9F4] shadow-md transition-all hover:!bg-[#1F1E1D] hover:shadow-lg disabled:opacity-70"
            >
              {isPending ? 'Guardando...' : 'Guardar Categoría'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}