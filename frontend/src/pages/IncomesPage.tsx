import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { format } from 'date-fns';
import { parseDateOnly } from '../utils/dateOnly';
import { useIncomes, useDeleteIncome } from '../features/incomes/hooks/useIncomes';
import { IncomeForm } from '../features/incomes/components/IncomeForm';
import {
  Button,
  Card,
  CardHeader,
  PageHeader,
  Spinner,
  ConfirmDialog,
  TablePagination,
} from '../components/ui';
import { Plus as PlusIcon, AlertCircle, Inbox, Pencil, Trash2 } from 'lucide-react';
import type { Income } from '../types/income.types';

export function IncomesPage() {
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingIncome, setEditingIncome] = useState<Income | null>(null);
  const [searchParams, setSearchParams] = useSearchParams();
  const parsedPage = Number.parseInt(searchParams.get('page') ?? '1', 10);
  const page = Number.isFinite(parsedPage) && parsedPage > 0 ? parsedPage : 1;
  const pageSize = 20;

  const [deletingIncome, setDeletingIncome] = useState<Income | null>(null);

  const { data: response, isLoading, isError, isFetching } = useIncomes({ page, pageSize });
  const { mutate: deleteIncome, isPending: isDeleting } = useDeleteIncome();

  const pagedData = response?.data?.data;
  const incomes = pagedData?.items || [];

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
      <PageHeader
        eyebrow="Movimientos"
        title="Ingresos"
        description="Gestiona y registra tus entradas de dinero."
        action={
          <Button
            onClick={handleOpenCreate}
            leftIcon={<PlusIcon className="h-4 w-4" strokeWidth={2.5} />}
            className="!bg-finflow-dark !text-finflow-cream hover:!bg-[#1F1E1D]"
          >
            Nuevo ingreso
          </Button>
        }
      />

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
          <div className="flex flex-col items-center gap-3 p-12 text-finflow-muted">
            <Spinner />
            <span className="text-sm">Cargando ingresos...</span>
          </div>
        ) : isError ? (
          <div className="flex flex-col items-center gap-2 p-12 text-center text-finflow-rust">
            <AlertCircle className="h-6 w-6" strokeWidth={2} />
            <span className="text-sm font-medium">Error al cargar los datos.</span>
          </div>
        ) : incomes.length === 0 ? (
          <div className="flex flex-col items-center gap-2 p-12 text-center text-finflow-muted">
            <Inbox className="h-6 w-6" strokeWidth={2} />
            <span className="text-sm">Aún no tienes ingresos registrados.</span>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-finflow-dark">
              <thead className="bg-[#F3F1EC] text-xs uppercase tracking-wide text-finflow-muted">
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
                  <tr key={income.id} className="transition-colors hover:bg-finflow-cream">
                    <td className="px-6 py-4 text-finflow-muted">
                      {format(parseDateOnly(income.date), 'dd/MM/yyyy')}
                    </td>
                    <td className="flex items-center gap-2 px-6 py-4">
                      <div
                        className="h-3 w-3 rounded-full"
                        style={{ backgroundColor: income.categoryColor }}
                      />
                      {income.categoryName}
                    </td>
                    <td className="px-6 py-4 text-finflow-muted">{income.description || '-'}</td>
                    <td className="px-6 py-4 text-right font-medium text-finflow-blue">
                      {new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(income.amount)}
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          type="button"
                          onClick={() => handleEdit(income)}
                          className="rounded-lg p-2 text-finflow-muted transition-colors hover:bg-[#EFEAE2] hover:text-finflow-blue"
                          aria-label="Editar ingreso"
                        >
                          <Pencil className="h-4 w-4" strokeWidth={2} />
                        </button>
                        <button
                          type="button"
                          onClick={() => setDeletingIncome(income)}
                          className="rounded-lg p-2 text-finflow-muted transition-colors hover:bg-[#EFEAE2] hover:text-finflow-rust"
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

      <ConfirmDialog
        isOpen={deletingIncome !== null}
        title="¿Eliminar este ingreso?"
        description={
          deletingIncome
            ? `Se eliminará "${deletingIncome.categoryName}" del ${format(parseDateOnly(deletingIncome.date), 'dd/MM/yyyy')} por ${new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(deletingIncome.amount)}. Esta acción no se puede deshacer.`
            : undefined
        }
        isLoading={isDeleting}
        onConfirm={confirmDelete}
        onCancel={() => setDeletingIncome(null)}
      />
    </div>
  );
}