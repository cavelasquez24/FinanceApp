import { type Category } from '../../../types/category.types';
import { useDeleteCategory } from '../hooks/useCategories';
import { Button } from '../../../components/ui';
import { AlertTriangle } from 'lucide-react';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  category: Category | null;
}

export function DeleteCategoryModal({ isOpen, onClose, category }: Props) {
  const deleteMutation = useDeleteCategory();

  if (!isOpen || !category) return null;

  const handleDelete = () => {
    deleteMutation.mutate(category.id, {
      onSuccess: () => onClose(),
    });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-[#2C2A29]/40 p-4 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="w-full max-w-sm rounded-[28px] border border-[#EFEAE2] bg-white/90 p-6 sm:p-8 shadow-xl backdrop-blur-xl animate-in zoom-in-95 duration-200 text-center">
        <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-[#C97B63]/10 mb-4">
          <AlertTriangle className="h-7 w-7 text-[#C97B63]" />
        </div>
        
        <h2 className="font-serif text-xl font-semibold text-[#2C2A29] mb-2">
          ¿Eliminar Categoría?
        </h2>
        <p className="text-sm text-[#7C756E] mb-6">
          Estás a punto de eliminar <strong>{category.name}</strong>. Esta acción no se puede deshacer y podría afectar el historial visual de tus transacciones.
        </p>

        <div className="flex flex-col gap-3">
          <Button
            onClick={handleDelete}
            disabled={deleteMutation.isPending}
            className="w-full h-11 rounded-xl !bg-[#C97B63] text-sm font-medium !text-white shadow-sm transition-all hover:!bg-[#A6604D] disabled:opacity-70"
          >
            {deleteMutation.isPending ? 'Eliminando...' : 'Sí, eliminar'}
          </Button>
          <Button
            variant="ghost"
            onClick={onClose}
            disabled={deleteMutation.isPending}
            className="w-full h-11 rounded-xl text-sm font-medium text-[#7C756E] hover:bg-[#EFEAE2]/60 transition-colors"
          >
            Cancelar
          </Button>
        </div>
      </div>
    </div>
  );
}