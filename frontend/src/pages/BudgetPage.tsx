import { useState } from 'react';
import { useBudgetByPeriod, useBudgetStatus } from '../features/budget/hooks/useBudget';
import { CreateBudgetForm } from '../features/budget/components/CreateBudgetForm';
import { EditBudgetForm } from '../features/budget/components/EditBudgetForm';
import { Card } from '../components/ui';
import { Pencil } from 'lucide-react';

export function BudgetPage() {
  const currentMonth = new Date().getMonth() + 1;
  const currentYear = new Date().getFullYear();
  const [isEditing, setIsEditing] = useState(false);

  const { data: periodResponse, isLoading: isLoadingPeriod } = useBudgetByPeriod(
    currentYear,
    currentMonth
  );

  // Leemos data correctamente — el backend retorna data: null si no existe
  const budgetData = periodResponse;
  const budgetId = budgetData?.id;

  const { data: statusResponse, isLoading: isLoadingStatus } = useBudgetStatus(budgetId);

  // --- CARGA ---
  if (isLoadingPeriod || (budgetId && isLoadingStatus)) {
    return (
      <div className="flex h-64 items-center justify-center">
        <div className="animate-pulse text-sm font-medium text-[#7C756E]">
          Cargando presupuesto...
        </div>
      </div>
    );
  }

  // --- SIN PRESUPUESTO ---
  if (!budgetId) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="font-serif text-2xl font-medium text-[#2C2A29]">Presupuesto</h1>
          <p className="mt-1 text-sm text-[#7C756E]">
            No tienes presupuesto para este mes. Configúralo a continuación.
          </p>
        </div>
        <CreateBudgetForm month={currentMonth} year={currentYear} />
      </div>
    );
  }

  // --- MODO EDICIÓN ---
  if (isEditing) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="font-serif text-2xl font-medium text-[#2C2A29]">Editar Presupuesto</h1>
          <button
            onClick={() => setIsEditing(false)}
            className="text-sm text-[#7C756E] transition-colors hover:text-[#2C2A29]"
          >
            Cancelar
          </button>
        </div>
        <EditBudgetForm
          budgetId={budgetId}
          currentData={{
            month: budgetData.month,
            year: budgetData.year,
            totalLimit: budgetData.totalLimit ?? undefined,
            notes: budgetData.notes ?? undefined,
            categories: budgetData.categories.map((c) => ({
              categoryId: c.categoryId,
              amountLimit: c.amountLimit,
            })),
          }}
          onSuccess={() => setIsEditing(false)}
        />
      </div>
    );
  }

  // --- CON PRESUPUESTO ---
  const status = statusResponse;

  const globalPercentage = status?.totalLimit
    ? Math.min((status.totalSpent / status.totalLimit) * 100, 100)
    : 0;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-end">
        <div>
          <h1 className="font-serif text-2xl font-medium text-[#2C2A29]">
            Presupuesto: {status?.period}
          </h1>
          <p className="text-sm text-[#7C756E]">Monitorea tus límites de gastos por categoría</p>
        </div>

        <div className="flex items-center gap-4">
          {/* Resumen global */}
          {status?.totalLimit && (
            <div className="text-right">
              <p className="text-sm text-[#7C756E]">Gasto Total Mensual</p>
              <p className="text-xl font-semibold text-[#2C2A29]">
                {new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(
                  status.totalSpent
                )}
                <span className="text-base font-normal text-[#7C756E]">
                  {' '}
                  / {new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(
                    status.totalLimit
                  )}
                </span>
              </p>
              <div className="float-right mt-2 h-2 w-48 overflow-hidden rounded-full bg-[#EFEAE2]">
                <div
                  className={`h-2 rounded-full transition-all duration-500 ${
                    status.isOverBudget ? 'bg-[#C97B63]' : 'bg-[#8FA888]'
                  }`}
                  style={{ width: `${globalPercentage}%` }}
                />
              </div>
            </div>
          )}

          {/* Botón editar */}
          <button
            onClick={() => setIsEditing(true)}
            className="flex items-center gap-2 rounded-xl border border-[#EFEAE2] bg-white/70 px-4 py-2 text-sm font-medium text-[#2C2A29] backdrop-blur-sm transition-colors hover:bg-[#F3F1EC]"
          >
            <Pencil className="h-4 w-4" />
            Editar
          </button>
        </div>
      </div>

      {/* Grid de categorías */}
      <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
        {status?.categories.map((cat) => {
          let barColor = 'bg-[#8FA888]';
          let badgeColor = 'text-[#8FA888] bg-[#8FA888]/10';
          let badgeLabel = 'Saludable';

          if (cat.isOverBudget) {
            barColor = 'bg-[#C97B63]';
            badgeColor = 'text-[#C97B63] bg-[#C97B63]/10';
            badgeLabel = 'Excedido';
          } else if (cat.alert) {
            barColor = 'bg-[#D9A46B]';
            badgeColor = 'text-[#D9A46B] bg-[#D9A46B]/10';
            badgeLabel = 'Alerta';
          }

          return (
            <Card key={cat.categoryName} className="transition-transform hover:-translate-y-1">
              <div className="mb-3 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div
                    className="h-3 w-3 rounded-full"
                    style={{ backgroundColor: cat.categoryColor }}
                  />
                  <h3 className="font-semibold text-[#2C2A29]">{cat.categoryName}</h3>
                </div>
                <span className="text-xs font-medium text-[#7C756E]">
                  {cat.percentageUsed.toFixed(0)}%
                </span>
              </div>

              <div className="mb-3 h-2.5 w-full rounded-full bg-[#EFEAE2]">
                <div
                  className={`h-2.5 rounded-full transition-all duration-500 ${barColor}`}
                  style={{ width: `${Math.min(cat.percentageUsed, 100)}%` }}
                />
              </div>

              <div className="mt-2 flex items-end justify-between">
                <div>
                  <p className="mb-1 text-xs text-[#7C756E]">Gastado / Límite</p>
                  <p className="text-sm font-medium text-[#2C2A29]">
                    {new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(
                      cat.amountSpent
                    )}
                    <span className="text-[#7C756E]"> / ${cat.amountLimit.toFixed(0)}</span>
                  </p>
                </div>

                <span className={`rounded-md px-2 py-1 text-xs font-medium ${badgeColor}`}>
                  {badgeLabel}
                </span>
              </div>
            </Card>
          );
        })}
      </div>
    </div>
  );
}