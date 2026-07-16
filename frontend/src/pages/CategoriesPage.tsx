import { CATEGORY_ICONS, DEFAULT_ICON_KEY } from '../features/categories/utils/iconRegistry';
import { useState } from 'react';
import { useCategories } from '../features/categories/hooks/useCategories';
import { type Category } from '../types/category.types';
import { Button } from '../components/ui';
import { Plus, Edit2, Trash2, Shield, ArrowUpRight, ArrowDownLeft } from 'lucide-react';
import { cn } from '../utils/cn';
import { CategoryFormModal } from '../features/categories/components/CategoryFormModal';
import { DeleteCategoryModal } from '../features/categories/components/DeleteCategoryModal';

export default function CategoriesPage() {
  const { data: categories, isLoading } = useCategories();
  const [activeFilter, setActiveFilter] = useState<'all' | 'income' | 'expense'>('all');
  
  // Estados para modales
  const [isFormModalOpen, setIsFormModalOpen] = useState(false);
  const [categoryToEdit, setCategoryToEdit] = useState<Category | null>(null);
  
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [categoryToDelete, setCategoryToDelete] = useState<Category | null>(null);

  if (isLoading) {
    return (
      <div className="flex min-h-[50vh] flex-col items-center justify-center gap-3">
        <div className="h-8 w-8 animate-spin rounded-full border-2 border-[#5C7A99] border-t-transparent" />
        <span className="text-sm font-medium text-[#7C756E]">Cargando el catálogo visual...</span>
      </div>
    );
  }

  const filteredCategories = categories?.filter(cat => 
    activeFilter === 'all' ? true : cat.type === activeFilter
  ) || [];

  const handleOpenEdit = (category: Category) => {
    setCategoryToEdit(category);
    setIsFormModalOpen(true);
  };

  const handleOpenCreate = () => {
    setCategoryToEdit(null);
    setIsFormModalOpen(true);
  };

  const handleOpenDelete = (category: Category) => {
    setCategoryToDelete(category);
    setIsDeleteModalOpen(true);
  };

  return (
    <>
      <div className="space-y-8 animate-in fade-in duration-500">
        {/* Encabezado Principal */}
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 className="font-serif text-3xl font-semibold tracking-tight text-[#2C2A29]">
              Gestión de Categorías
            </h1>
            <p className="mt-1 text-sm text-[#7C756E]">
              Organiza las clasificaciones de tus flujos financieros.
            </p>
          </div>
          <Button 
            onClick={handleOpenCreate}
            className="flex items-center justify-center gap-2 rounded-xl bg-[#2C2A29] px-5 py-2.5 text-sm font-medium text-[#FBF9F4] shadow-sm transition-all hover:bg-[#1F1E1D]"
          >
            <Plus className="h-4 w-4" /> Nueva Categoría
          </Button>
        </div>

        {/* Filtros */}
        <div className="flex gap-2 rounded-xl bg-[#EFEAE2]/40 p-1 w-max backdrop-blur-sm">
          {(['all', 'income', 'expense'] as const).map((filter) => (
            <button
              key={filter}
              onClick={() => setActiveFilter(filter)}
              className={cn(
                'rounded-lg px-4 py-1.5 text-xs font-semibold tracking-wide transition-all duration-200 uppercase',
                activeFilter === filter
                  ? 'bg-white text-[#2C2A29] shadow-sm'
                  : 'text-[#7C756E] hover:text-[#2C2A29]'
              )}
            >
              {filter === 'all' ? 'Todas' : filter === 'income' ? 'Ingresos' : 'Gastos'}
            </button>
          ))}
        </div>

        {/* Bento Grid Layout */}
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {filteredCategories.map((category) => {
            const isIncome = category.type === 'income';
            
            // Resolución dinámica del icono
            const IconComponent = category.icon && CATEGORY_ICONS[category.icon] 
              ? CATEGORY_ICONS[category.icon] 
              : CATEGORY_ICONS[DEFAULT_ICON_KEY];
            
            return (
              <div
                key={category.id}
                className="group relative flex flex-col justify-between overflow-hidden rounded-[28px] border border-[#EFEAE2] bg-white/70 p-6 shadow-sm backdrop-blur-xl transition-all duration-300 hover:-translate-y-1 hover:shadow-md"
              >
                <div 
                  className="absolute top-0 inset-x-0 h-1.5 opacity-60" 
                  style={{ backgroundColor: category.color }}
                />

                <div className="flex items-start justify-between">
                  <div 
                    className="flex h-12 w-12 items-center justify-center rounded-2xl shadow-inner transition-transform duration-300 group-hover:scale-105"
                    style={{ backgroundColor: `${category.color}12`, color: category.color }}
                  >
                    {/* Renderizado del icono real */}
                    <IconComponent className="h-5 w-5" strokeWidth={2} />
                  </div>
                  
                  <span className={cn(
                    'flex items-center gap-1 rounded-full px-2.5 py-1 text-[11px] font-bold uppercase tracking-wider',
                    isIncome ? 'bg-[#5C7A99]/10 text-[#5C7A99]' : 'bg-[#C97B63]/10 text-[#C97B63]'
                  )}>
                    {isIncome ? <><ArrowUpRight className="h-3 w-3" strokeWidth={2.5} /> Ingreso</> : <><ArrowDownLeft className="h-3 w-3" strokeWidth={2.5} /> Gasto</>}
                  </span>
                </div>

                <div className="my-6">
                  <h3 className="font-serif text-lg font-medium tracking-tight text-[#2C2A29]">
                    {category.name}
                  </h3>
                </div>

                <div className="border-t border-[#EFEAE2]/60 pt-4">
                  {category.isDefault ? (
                    <div className="flex items-center gap-1.5 text-xs font-semibold tracking-wide text-[#7C756E]/60 uppercase">
                      <Shield className="h-3.5 w-3.5 text-[#5C7A99]" strokeWidth={2.5} />
                      Sistema Protegido
                    </div>
                  ) : (
                    <div className="flex items-center gap-4 transition-all duration-200">
                      <button 
                        onClick={() => handleOpenEdit(category)}
                        className="flex items-center gap-1 text-xs font-bold uppercase tracking-wider text-[#5C7A99] hover:text-[#4A6480] transition-colors"
                      >
                        <Edit2 className="h-3.5 w-3.5" /> Editar
                      </button>
                      <button 
                        onClick={() => handleOpenDelete(category)}
                        className="flex items-center gap-1 text-xs font-bold uppercase tracking-wider text-[#C97B63] hover:text-[#A6604D] transition-colors"
                      >
                        <Trash2 className="h-3.5 w-3.5" /> Eliminar
                      </button>
                    </div>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Modales Inyectados */}
      <CategoryFormModal 
        isOpen={isFormModalOpen} 
        onClose={() => setIsFormModalOpen(false)} 
        categoryToEdit={categoryToEdit} 
      />
      
      <DeleteCategoryModal 
        isOpen={isDeleteModalOpen} 
        onClose={() => setIsDeleteModalOpen(false)} 
        category={categoryToDelete} 
      />
    </>
  );
}