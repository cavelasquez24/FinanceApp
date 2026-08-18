import { monthEndDateOnly } from '../utils/dateOnly';
// src/pages/ExpensesPage.tsx
import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { format } from 'date-fns';
import { parseDateOnly } from '../utils/dateOnly';
import { Plus as PlusIcon, AlertCircle, Inbox, Pencil, Trash2, Undo2 } from 'lucide-react';
import { useExpenses, useDeleteExpense } from '../features/expenses/hooks/useExpenses';
import { ExpenseForm } from '../features/expenses/components/ExpenseForm';
import { ExpensesByCategoryChart } from '../components/dashboard/ExpensesByCategoryChart';
import { MonthYearSelector } from '../features/dashboard/components/MonthYearSelector';
import { useDashboardExpensesByCategory } from '../features/dashboard/hooks/useDashboard';
import {
  Button,
  Card,
  CardHeader,
  Spinner,
  ConfirmDialog,
  TablePagination,
} from '../components/ui';
import type { Expense } from '../types/expense.types';
import { TagManager } from '../features/tags/components/TagManager';
import { TagExpenseReport } from '../features/tags/components/TagExpenseReport';
import { useTags } from '../features/tags/hooks/useTags';

// v2.1 — donut movido acá desde el Dashboard (vista de análisis, no de
// resumen ejecutivo). Reutiliza hook/endpoint existentes, cero endpoint
// nuevo. Selector de mes independiente del listado de abajo: la tabla de
// gastos no filtra por mes hoy — fuera de alcance de este cambio visual.
export function ExpensesPage() {
  const today = new Date();
  const [categoryMonth, setCategoryMonth] = useState(today.getMonth() + 1);
  const [searchParams, setSearchParams] = useSearchParams();
  const parsedPage = Number.parseInt(searchParams.get('page') ?? '1', 10);
  const page = Number.isFinite(parsedPage) && parsedPage > 0 ? parsedPage : 1;
  const pageSize = 20;

  const [categoryYear, setCategoryYear] = useState(today.getFullYear());

  const [isFormOpen, setIsFormOpen] = useState(false);
  const { data: availableTags = [] } = useTags();
  const [editingExpense, setEditingExpense] = useState<Expense | null>(null);
  const [deletingExpense, setDeletingExpense] = useState<Expense | null>(null);

  const { data: response, isLoading, isError, isFetching } = useExpenses({
    page,
    pageSize,
    search: searchParams.get('q') || undefined,
    merchant: searchParams.get('merchant') || undefined,
    tagIds: (searchParams.get('tags') ?? '').split(',').filter(Boolean),
    tagMatch: (searchParams.get('tagMatch') as 'any' | 'all') || 'any',
    untagged: searchParams.get('untagged') === 'true',
  });
  const { mutate: deleteExpense, isPending: isDeleting } = useDeleteExpense();
  const {
    data: categoryData,
    isLoading: isLoadingCategory,
    isError: isErrorCategory,
  } = useDashboardExpensesByCategory(categoryMonth, categoryYear);

  const pagedData = response?.data?.data;
  const expenses = pagedData?.items || [];
  const selectedTagIds = (searchParams.get('tags') ?? '').split(',').filter(Boolean);
  const updateFilter = (key: string, value?: string) => {
    const next = new URLSearchParams(searchParams);
    if (value) next.set(key, value); else next.delete(key);
    next.delete('page');
    setSearchParams(next);
  };
  const reportStart = categoryYear + '-' + String(categoryMonth).padStart(2, '0') + '-01';
  const reportEnd = monthEndDateOnly(categoryYear, categoryMonth);

  const changePage = (nextPage: number) => {
    const nextParams = new URLSearchParams(searchParams);
    if (nextPage <= 1) {
      nextParams.delete('page');
    } else {
      nextParams.set('page', String(nextPage));
    }
    setSearchParams(nextParams);
  };

  useEffect(() => {
    if (pagedData?.totalPages === undefined) return;

    const lastPage = Math.max(pagedData.totalPages, 1);
    if (page > lastPage) {
      const nextParams = new URLSearchParams(searchParams);
      if (lastPage === 1) {
        nextParams.delete('page');
      } else {
        nextParams.set('page', String(lastPage));
      }
      setSearchParams(nextParams, { replace: true });
    }
  }, [page, pagedData?.totalPages, searchParams, setSearchParams]);

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


      <Card>
        <CardHeader title="Filtros" subtitle="Combina texto, comercio y etiquetas" />
        <div className="grid gap-3 md:grid-cols-3">
          <input value={searchParams.get('q') ?? ''} onChange={(e) => updateFilter('q', e.target.value)}
            placeholder="Buscar descripción, notas o etiqueta" className="rounded-xl border border-[#EFEAE2] bg-white px-3 py-2 text-sm" />
          <input value={searchParams.get('merchant') ?? ''} onChange={(e) => updateFilter('merchant', e.target.value)}
            placeholder="Comercio" className="rounded-xl border border-[#EFEAE2] bg-white px-3 py-2 text-sm" />
          <select value={searchParams.get('tagMatch') ?? 'any'} onChange={(e) => updateFilter('tagMatch', e.target.value)}
            className="rounded-xl border border-[#EFEAE2] bg-white px-3 py-2 text-sm">
            <option value="any">Cualquiera de las etiquetas</option>
            <option value="all">Todas las etiquetas</option>
          </select>
        </div>
        <div className="mt-3 flex flex-wrap gap-2">
          {availableTags.map((tag) => {
            const active = selectedTagIds.includes(tag.id);
            return <button key={tag.id} type="button"
              onClick={() => updateFilter('tags', (active ? selectedTagIds.filter((id) => id !== tag.id) : [...selectedTagIds, tag.id]).join(','))}
              className={`rounded-full border px-3 py-1 text-xs ${active ? 'border-[#5C7A99] bg-[#5C7A99] text-white' : 'border-[#EFEAE2] bg-white'}`}>
              {tag.name}
            </button>;
          })}
          <label className="flex items-center gap-2 text-sm text-[#7C756E]">
            <input type="checkbox" checked={searchParams.get('untagged') === 'true'}
              onChange={(e) => updateFilter('untagged', e.target.checked ? 'true' : undefined)} />Sin etiquetas
          </label>
          {(searchParams.get('q') || searchParams.get('merchant') || selectedTagIds.length || searchParams.get('untagged')) && (
            <button type="button" onClick={() => setSearchParams({})} className="text-xs text-[#C97B63]">Limpiar filtros</button>
          )}
        </div>
      </Card>

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
            <>
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
                        {format(parseDateOnly(expense.date), 'dd/MM/yyyy')}
                      </td>
                      <td className="flex items-center gap-2 px-6 py-4">
                        <div
                          className="h-3 w-3 rounded-full"
                          style={{ backgroundColor: expense.categoryColor }}
                        />
                        {expense.categoryName}
                        {expense.tags.length > 0 && (
                          <div className="ml-2 flex flex-wrap gap-1">
                            {expense.tags.slice(0, 2).map((tag) => (
                              <span key={tag.id} className="rounded-full px-2 py-0.5 text-[10px] text-white"
                                style={{ backgroundColor: tag.color ?? '#5C7A99' }}>{tag.name}</span>
                            ))}
                            {expense.tags.length > 2 && <span className="text-xs text-[#7C756E]">+{expense.tags.length - 2}</span>}
                          </div>
                        )}
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
                          <Link
                            to={'/reimbursements?expenseId=' + expense.id}
                            className="rounded-lg p-2 text-[#7C756E] transition-colors hover:bg-[#5C7A99]/10 hover:text-[#5C7A99]"
                            aria-label="Registrar reembolso de este gasto"
                          >
                            <Undo2 className="h-4 w-4" strokeWidth={2} />
                          </Link>
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
              {pagedData && (
                <TablePagination
                  page={pagedData.page}
                  totalPages={pagedData.totalPages}
                  totalCount={pagedData.totalCount}
                  pageSize={pagedData.pageSize}
                  hasNextPage={pagedData.hasNextPage}
                  hasPreviousPage={pagedData.hasPreviousPage}
                  onPageChange={changePage}
                  disabled={isFetching}
                />
              )}
            </>
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

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader title="Administrar etiquetas" subtitle="Edita, elimina o fusiona duplicados" />
          <TagManager />
        </Card>
        <Card>
          <CardHeader title="Análisis por etiquetas" subtitle="Totales asociados del mes seleccionado" />
          <TagExpenseReport startDate={reportStart} endDate={reportEnd} />
        </Card>
      </div>

      <ConfirmDialog
        isOpen={deletingExpense !== null}
        title="¿Eliminar este gasto?"
        description={
          deletingExpense
            ? `Se eliminará "${deletingExpense.categoryName}" del ${format(parseDateOnly(deletingExpense.date), 'dd/MM/yyyy')} por ${new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(deletingExpense.amount)}. Esta acción no se puede deshacer.`
            : undefined
        }
        isLoading={isDeleting}
        onConfirm={confirmDelete}
        onCancel={() => setDeletingExpense(null)}
      />
    </div>
  );
}
