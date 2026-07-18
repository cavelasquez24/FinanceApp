import { useState } from 'react';
import { format, parseISO } from 'date-fns';
import { useIncomes, useDeleteIncome } from '../features/incomes/hooks/useIncomes';
import { IncomeForm } from '../features/incomes/components/IncomeForm';
import { Button, Card, CardHeader, Spinner, ConfirmDialog } from '../components/ui';
import { Plus as PlusIcon, AlertCircle, Inbox, Pencil, Trash2 } from 'lucide-react';
import type { Income } from '../types/income.types';

export function IncomesPage() {
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingIncome, setEditingIncome] = useState<Income | null>(null);
  const [deletingIncome, setDeletingIncome] = useState<Income | null>(null);

  const { data: response, isLoading, isError } = useIncomes({ page: 1, pageSize: 20 });
  const { mutate: deleteIncome, isPending: isDeleting } = useDeleteIncome();

  const incomes = response?.data?.data?.items || [];

  const handleOpenCreate = () => {
    setEditingIncome(null);
    setIsFormOpen((prev) => !prev);
  };

  const handleEdit = (income: Income) => {
    setIsFormOpen(false);
    setEditingIncome(income);
  };

  const closeForm = () => {
    setIsFormOpen(false);
    setEditingIncome(null);
  };

  const confirmDelete = () => {
    if (!deletingIncome) return;
    deleteIncome(deletingIncome.id, {
      onSuccess: () => setDeletingIncome(null),
    });
  };

  const isFormVisible = isFormOpen || editingIncome !== null;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="font-serif text-2xl font-medium text-[#2C2A29]">Ingresos</h1>
          <p className="text-sm text-[#7C756E]">Gestiona y registra tus entradas de dinero.</p>
        </div>
        <Button
          onClick={handleOpenCreate}
          leftIcon={<PlusIcon className="h-4 w-4" strokeWidth={2.5} />}
          className="!bg-[#2C2A29] !text-[#FBF9F4] hover:!bg-[#1F1E1D]"
        >
          Nuevo ingreso
        </Button>
      </div>

      {/* Formulario (crear o editar) */}
      {isFormVisible && (
        <Card key={editingIncome?.id ?? 'new'}>
          <CardHeader title={editingIncome ? 'Editar Ingreso' : 'Registrar Nuevo Ingreso'} />
          <IncomeForm income={editingIncome ?? undefined} onSuccess={closeForm} onCancel={closeForm} />
        </Card>
      )}

      {/* Tabla de Resultados */}
      <Card noPadding className="overflow-hidden">
        {isLoading ? (
          <div className="flex flex-col items-center gap-3 p-12 text-[#7C756E]">
            <Spinner />
            <span className="text-sm">Cargando ingresos...</span>
          </div>
        ) : isError ? (
          <div className="flex flex-col items-center gap-2 p-12 text-center text-[#C97B63]">
            <AlertCircle className="h-6 w-6" strokeWidth={2} />
            <span className="text-sm font-medium">Error al cargar los datos.</span>
          </div>
        ) : incomes.length === 0 ? (
          <div className="flex flex-col items-center gap-2 p-12 text-center text-[#7C756E]">
            <Inbox className="h-6 w-6" strokeWidth={2} />
            <span className="text-sm">Aún no tienes ingresos registrados.</span>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-[#2C2A29]">
              <thead className="bg-[#F3F1EC] text-xs uppercase tracking-wide text-[#7C756E]">
                <tr>
                  <th className="px-6 py-4 font-medium">Fecha</th>
                  <th className="px-6 py-4 font-medium">Categoría</th>
                  <th className="px-6 py-4 font-medium">Descripción</th>
                  <th className="px-6 py-4 text-right font-medium">Monto</th>
                  <th className="px-6 py-4 text-right font-medium">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#EFEAE2]">
                {incomes.map((income) => (
                  <tr key={income.id} className="transition-colors hover:bg-[#FBF9F4]">
                    <td className="px-6 py-4 text-[#7C756E]">
                      {format(parseISO(income.date), 'dd/MM/yyyy')}
                    </td>
                    <td className="flex items-center gap-2 px-6 py-4">
                      <div
                        className="h-3 w-3 rounded-full"
                        style={{ backgroundColor: income.categoryColor }}
                      />
                      {income.categoryName}
                    </td>
                    <td className="px-6 py-4 text-[#7C756E]">{income.description || '-'}</td>
                    <td className="px-6 py-4 text-right font-medium text-[#5C7A99]">
                      {new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(income.amount)}
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          type="button"
                          onClick={() => handleEdit(income)}
                          className="rounded-lg p-2 text-[#7C756E] transition-colors hover:bg-[#EFEAE2] hover:text-[#5C7A99]"
                          aria-label="Editar ingreso"
                        >
                          <Pencil className="h-4 w-4" strokeWidth={2} />
                        </button>
                        <button
                          type="button"
                          onClick={() => setDeletingIncome(income)}
                          className="rounded-lg p-2 text-[#7C756E] transition-colors hover:bg-[#EFEAE2] hover:text-[#C97B63]"
                          aria-label="Eliminar ingreso"
                        >
                          <Trash2 className="h-4 w-4" strokeWidth={2} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <ConfirmDialog
        isOpen={deletingIncome !== null}
        title="¿Eliminar este ingreso?"
        description={
          deletingIncome
            ? `Se eliminará "${deletingIncome.categoryName}" del ${format(parseISO(deletingIncome.date), 'dd/MM/yyyy')} por ${new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(deletingIncome.amount)}. Esta acción no se puede deshacer.`
            : undefined
        }
        isLoading={isDeleting}
        onConfirm={confirmDelete}
        onCancel={() => setDeletingIncome(null)}
      />
    </div>
  );
}