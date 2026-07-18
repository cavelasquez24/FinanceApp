// src/pages/ExpensesPage.tsx
import { useState } from 'react';
import { format, parseISO } from 'date-fns';
import { Plus as PlusIcon, AlertCircle, Inbox, Pencil, Trash2 } from 'lucide-react';
import { useExpenses, useDeleteExpense } from '../features/expenses/hooks/useExpenses';
import { ExpenseForm } from '../features/expenses/components/ExpenseForm';
import { Button, Card, CardHeader, Spinner, ConfirmDialog } from '../components/ui';
import type { Expense } from '../types/expense.types';

export function ExpensesPage() {
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingExpense, setEditingExpense] = useState<Expense | null>(null);
  const [deletingExpense, setDeletingExpense] = useState<Expense | null>(null);

  const { data: response, isLoading, isError } = useExpenses({ page: 1, pageSize: 20 });
  const { mutate: deleteExpense, isPending: isDeleting } = useDeleteExpense();

  const expenses = response?.data?.data?.items || [];

  const handleOpenCreate = () => {
    setEditingExpense(null);
    setIsFormOpen((prev) => !prev);
  };

  const handleEdit = (expense: Expense) => {
    setIsFormOpen(false);
    setEditingExpense(expense);
  };

  const closeForm = () => {
    setIsFormOpen(false);
    setEditingExpense(null);
  };

  const confirmDelete = () => {
    if (!deletingExpense) return;
    deleteExpense(deletingExpense.id, {
      onSuccess: () => setDeletingExpense(null),
    });
  };

  const isFormVisible = isFormOpen || editingExpense !== null;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="font-serif text-2xl font-medium text-[#2C2A29]">Gastos</h1>
          <p className="text-sm text-[#7C756E]">
            Controla tus salidas de dinero y mantén tus finanzas sanas.
          </p>
        </div>
        <Button
          onClick={handleOpenCreate}
          leftIcon={<PlusIcon className="h-4 w-4" strokeWidth={2.5} />}
          className="!bg-[#2C2A29] !text-[#FBF9F4] hover:!bg-[#1F1E1D]"
        >
          Nuevo Gasto
        </Button>
      </div>

      {/* Formulario (crear o editar) */}
      {isFormVisible && (
        <Card key={editingExpense?.id ?? 'new'}>
          <CardHeader title={editingExpense ? 'Editar Gasto' : 'Registrar Nuevo Gasto'} />
          <ExpenseForm expense={editingExpense ?? undefined} onSuccess={closeForm} onCancel={closeForm} />
        </Card>
      )}

      {/* Tabla de Gastos */}
      <Card noPadding className="overflow-hidden">
        {isLoading ? (
          <div className="flex flex-col items-center gap-3 p-12 text-[#7C756E]">
            <Spinner />
            <span className="text-sm">Cargando gastos...</span>
          </div>
        ) : isError ? (
          <div className="flex flex-col items-center gap-2 p-12 text-center text-[#C97B63]">
            <AlertCircle className="h-6 w-6" strokeWidth={2} />
            <span className="text-sm font-medium">Error al cargar los gastos.</span>
          </div>
        ) : expenses.length === 0 ? (
          <div className="flex flex-col items-center gap-2 p-12 text-center text-[#7C756E]">
            <Inbox className="h-6 w-6" strokeWidth={2} />
            <span className="text-sm">Aún no tienes gastos registrados.</span>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-[#2C2A29]">
              <thead className="bg-[#F3F1EC] text-xs uppercase tracking-wide text-[#7C756E]">
                <tr>
                  <th className="px-6 py-4 font-medium">Fecha</th>
                  <th className="px-6 py-4 font-medium">Categoría</th>
                  <th className="px-6 py-4 font-medium">Método</th>
                  <th className="px-6 py-4 font-medium">Descripción</th>
                  <th className="px-6 py-4 text-right font-medium">Monto</th>
                  <th className="px-6 py-4 text-right font-medium">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#EFEAE2]">
                {expenses.map((expense) => (
                  <tr key={expense.id} className="transition-colors hover:bg-[#FBF9F4]">
                    <td className="px-6 py-4 text-[#7C756E]">
                      {format(parseISO(expense.date), 'dd/MM/yyyy')}
                    </td>
                    <td className="flex items-center gap-2 px-6 py-4">
                      <div
                        className="h-3 w-3 rounded-full"
                        style={{ backgroundColor: expense.categoryColor }}
                      />
                      {expense.categoryName}
                    </td>
                    <td className="px-6 py-4 capitalize text-[#7C756E]">
                      {expense.paymentMethod.replace('_', ' ')}
                    </td>
                    <td className="px-6 py-4 text-[#7C756E]">{expense.description || '-'}</td>
                    <td className="px-6 py-4 text-right font-medium text-[#C97B63]">
                      -{new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(expense.amount)}
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          type="button"
                          onClick={() => handleEdit(expense)}
                          className="rounded-lg p-2 text-[#7C756E] transition-colors hover:bg-[#EFEAE2] hover:text-[#5C7A99]"
                          aria-label="Editar gasto"
                        >
                          <Pencil className="h-4 w-4" strokeWidth={2} />
                        </button>
                        <button
                          type="button"
                          onClick={() => setDeletingExpense(expense)}
                          className="rounded-lg p-2 text-[#7C756E] transition-colors hover:bg-[#EFEAE2] hover:text-[#C97B63]"
                          aria-label="Eliminar gasto"
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
        isOpen={deletingExpense !== null}
        title="¿Eliminar este gasto?"
        description={
          deletingExpense
            ? `Se eliminará "${deletingExpense.categoryName}" del ${format(parseISO(deletingExpense.date), 'dd/MM/yyyy')} por ${new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(deletingExpense.amount)}. Esta acción no se puede deshacer.`
            : undefined
        }
        isLoading={isDeleting}
        onConfirm={confirmDelete}
        onCancel={() => setDeletingExpense(null)}
      />
    </div>
  );
}