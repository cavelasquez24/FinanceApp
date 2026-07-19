// src/pages/ExpensesPage.tsx
import { useState } from 'react';
import { format, parseISO } from 'date-fns';
import { Plus as PlusIcon, AlertCircle, Inbox, Pencil, Trash2 } from 'lucide-react';
import { useExpenses, useDeleteExpense } from '../features/expenses/hooks/useExpenses';
import { ExpenseForm } from '../features/expenses/components/ExpenseForm';
import { ExpensesByCategoryChart } from '../components/dashboard/ExpensesByCategoryChart';
import { MonthYearSelector } from '../features/dashboard/components/MonthYearSelector';
import { useDashboardExpensesByCategory } from '../features/dashboard/hooks/useDashboard';
import { Button, Card, CardHeader, Spinner, ConfirmDialog } from '../components/ui';
import type { Expense } from '../types/expense.types';

// v2.1 — donut movido acá desde el Dashboard (vista de análisis, no de
// resumen ejecutivo). Reutiliza hook/endpoint existentes, cero endpoint
// nuevo. Selector de mes independiente del listado de abajo: la tabla de
// gastos no filtra por mes hoy — fuera de alcance de este cambio visual.
export function ExpensesPage() {
  const today = new Date();
  const [categoryMonth, setCategoryMonth] = useState(today.getMonth() + 1);
  const [categoryYear, setCategoryYear] = useState(today.getFullYear());

  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingExpense, setEditingExpense] = useState<Expense | null>(null);
  const [deletingExpense, setDeletingExpense] = useState<Expense | null>(null);

  const { data: response, isLoading, isError } = useExpenses({ page: 1, pageSize: 20 });
  const { mutate: deleteExpense, isPending: isDeleting } = useDeleteExpense();
  const {
    data: categoryData,
    isLoading: isLoadingCategory,
    isError: isErrorCategory,
  } = useDashboardExpensesByCategory(categoryMonth, categoryYear);

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

      {isFormVisible && (
        <Card key={editingExpense?.id ?? 'new'}>
          <CardHeader title={editingExpense ? 'Editar Gasto' : 'Registrar Nuevo Gasto'} />
          <ExpenseForm expense={editingExpense ?? undefined} onSuccess={closeForm} onCancel={closeForm} />
        </Card>
      )}

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <Card noPadding className="overflow-hidden lg:col-span-2">
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

        <Card className="!rounded-[28px]">
          <CardHeader title="Por categoría" subtitle="Distribución del mes seleccionado" />
          <div className="mb-4">
            <MonthYearSelector
              month={categoryMonth}
              year={categoryYear}
              onChange={(m, y) => {
                setCategoryMonth(m);
                setCategoryYear(y);
              }}
            />
          </div>
          {isLoadingCategory ? (
            <div className="flex flex-col items-center gap-3 p-8 text-[#7C756E]">
              <Spinner />
              <span className="text-sm">Cargando categorías...</span>
            </div>
          ) : isErrorCategory ? (
            <div className="flex flex-col items-center gap-2 p-8 text-center text-[#C97B63]">
              <AlertCircle className="h-6 w-6" strokeWidth={2} />
              <span className="text-sm font-medium">Error al cargar categorías.</span>
            </div>
          ) : categoryData ? (
            <ExpensesByCategoryChart data={categoryData} />
          ) : null}
        </Card>
      </div>

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