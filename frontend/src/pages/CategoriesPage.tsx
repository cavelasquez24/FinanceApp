import { CATEGORY_ICONS, DEFAULT_ICON_KEY } from '../features/categories/utils/iconRegistry';
import { useState } from 'react';
import { useCategories } from '../features/categories/hooks/useCategories';
import { type Category } from '../types/category.types';
import { Button, PageHeader } from '../components/ui';
import { Plus, Edit2, Trash2, Shield, ArrowUpRight, ArrowDownLeft, FolderOpen } from 'lucide-react';
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
        <div className="h-8 w-8 animate-spin rounded-full border-2 border-finflow-blue border-t-transparent" />
        <span className="text-sm font-medium text-finflow-muted">Cargando el catálogo visual...</span>
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
        <PageHeader
          eyebrow="Configuración"
          title="Categorías"
          description="Organiza las clasificaciones de tus flujos financieros."
          action={
            <Button
              onClick={handleOpenCreate}
              className="flex items-center justify-center gap-2 rounded-xl bg-finflow-dark px-5 py-2.5 text-sm font-medium text-finflow-cream shadow-sm transition-all hover:bg-[#1F1E1D]"
              leftIcon={<Plus className="h-4 w-4" />}
            >
              Nueva Categoría
            </Button>
          }
        />

        {/* Filtros */}
        <div className="flex gap-2 rounded-xl bg-[#EFEAE2]/40 p-1 w-max backdrop-blur-sm" role="tablist">
          {(['all', 'income', 'expense'] as const).map((filter) => (
            <button
              key={filter}
              role="tab"
              aria-selected={activeFilter === filter}
              onClick={() => setActiveFilter(filter)}
              className={cn(
                'rounded-lg px-4 py-2 min-h-[44px] text-xs font-semibold tracking-wide transition-all duration-200 uppercase',
                activeFilter === filter
                  ? 'bg-white text-finflow-dark shadow-sm'
                  : 'text-finflow-muted hover:text-finflow-dark'
              )}
            >
              {filter === 'all' ? 'Todas' : filter === 'income' ? 'Ingresos' : 'Gastos'}
            </button>
          ))}
        </div>

        {/* Estado vacío */}
        {filteredCategories.length === 0 && (
          <div className="flex flex-col items-center justify-center gap-3 rounded-[28px] border border-dashed border-[#EFEAE2] bg-white/50 py-16 text-center">
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-[#EFEAE2]/60">
              <FolderOpen className="h-6 w-6 text-finflow-muted" strokeWidth={1.5} />
            </div>
            <p className="text-sm font-medium text-finflow-dark">
              {activeFilter === 'all'
                ? 'Aún no tienes categorías'
                : activeFilter === 'income'
                  ? 'No tienes categorías de ingreso'
                  : 'No tienes categorías de gasto'}
            </p>
            <p className="max-w-xs text-xs text-finflow-muted">
              Crea una categoría para empezar a clasificar tus movimientos.
            </p>
            <Button
              onClick={handleOpenCreate}
              className="mt-2 flex items-center justify-center gap-2 rounded-xl bg-finflow-dark px-5 py-2.5 text-sm font-medium text-finflow-cream shadow-sm transition-all hover:bg-[#1F1E1D]"
              leftIcon={<Plus className="h-4 w-4" />}
            >
              Nueva Categoría
            </Button>
          </div>
        )}

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
                    isIncome ? 'bg-finflow-blue/10 text-finflow-blue' : 'bg-finflow-rust/10 text-finflow-rust'
                  )}>
                    {isIncome ? <><ArrowUpRight className="h-3 w-3" strokeWidth={2.5} /> Ingreso</> : <><ArrowDownLeft className="h-3 w-3" strokeWidth={2.5} /> Gasto</>}
                  </span>
                </div>

                <div className="my-6">
                  <h3 className="font-serif text-lg font-medium tracking-tight text-finflow-dark">
                    {category.name}
                  </h3>
                </div>

                <div className="border-t border-[#EFEAE2]/60 pt-4">
                  {category.isDefault ? (
                    <div className="flex items-center gap-1.5 text-xs font-semibold tracking-wide text-finflow-muted uppercase">
                      <Shield className="h-3.5 w-3.5 text-finflow-blue" strokeWidth={2.5} />
                      Sistema Protegido
                    </div>
                  ) : (
                    <div className="flex items-center gap-2 -mx-2 transition-all duration-200">
                      <button
                        onClick={() => handleOpenEdit(category)}
                        className="flex min-h-[44px] items-center gap-1.5 rounded-lg px-2 text-xs font-bold uppercase tracking-wider text-finflow-blue hover:bg-finflow-blue/10 hover:text-[#4A6480] transition-colors"
                      >
                        <Edit2 className="h-3.5 w-3.5" /> Editar
                      </button>
                      <button
                        onClick={() => handleOpenDelete(category)}
                        className="flex min-h-[44px] items-center gap-1.5 rounded-lg px-2 text-xs font-bold uppercase tracking-wider text-finflow-rust hover:bg-finflow-rust/10 hover:text-[#A6604D] transition-colors"
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