// src/pages/InvestmentsPage.tsx
import { useState } from 'react';
import { Plus, Edit2, Trash2, TrendingUp, DollarSign, PieChart, Activity, PlusCircle, ArrowDownLeft, MoreHorizontal } from 'lucide-react';
import { AddInvestmentContributionForm } from '../features/investments/components/AddInvestmentContributionForm';
import { WithdrawalModal } from '../features/investments/components/WithdrawalModal';
import { useInvestments, useInvestmentSummary, useDeleteInvestment } from '../features/investments/hooks/useInvestments';
import { CreateInvestmentForm } from '../features/investments/components/CreateInvestmentForm';
import { EditInvestmentForm } from '../features/investments/components/EditInvestmentForm';
import { AddInvestmentRecordForm } from '../features/investments/components/AddInvestmentRecordForm';
import { formatCurrency } from '../utils/formatCurrency';
import type { Investment } from '../types/investment.types';
import { Button, Card, PageHeader, Spinner, Modal, BottomSheet } from '../components/ui';

export default function InvestmentsPage() {
  const { data: summary, isLoading: loadingSummary } = useInvestmentSummary();
  const { data: investments, isLoading: loadingInvestments } = useInvestments();
  const { mutate: deleteInvestment, isPending: isDeleting } = useDeleteInvestment();

  // Estados de Modales
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [editingInvestment, setEditingInvestment] = useState<Investment | null>(null);
  const [deletingInvestment, setDeletingInvestment] = useState<Investment | null>(null);
  const [addingRecordInvestment, setAddingRecordInvestment] = useState<Investment | null>(null);
  const [addingContributionInvestment, setAddingContributionInvestment] = useState<Investment | null>(null);
  const [withdrawingInvestment, setWithdrawingInvestment] = useState<Investment | null>(null);
  const [actionSheetInvestment, setActionSheetInvestment] = useState<Investment | null>(null);

  if (loadingSummary || loadingInvestments) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <div className="flex flex-col items-center gap-3 text-finflow-muted">
          <Spinner />
          <span className="text-sm font-medium tracking-wide">Cargando portafolio...</span>
        </div>
      </div>
    );
  }

  const handleDelete = () => {
    if (deletingInvestment) {
      deleteInvestment(deletingInvestment.id, {
        onSuccess: () => setDeletingInvestment(null)
      });
    }
  };

  return (
    <div className="space-y-8">

      <PageHeader
        eyebrow="Patrimonio"
        title="Portafolio de inversiones"
        action={
          <Button
            onClick={() => setIsCreateOpen(true)}
            className="flex items-center gap-2 bg-finflow-dark text-finflow-cream hover:bg-finflow-dark/90 rounded-2xl px-5 py-2.5 transition-all shadow-md"
            leftIcon={<Plus className="h-4 w-4" />}
          >
            Nueva Inversión
          </Button>
        }
      />

      {/* Resumen del Portafolio */}
      {summary && (
        <div className="grid grid-cols-1 gap-6 md:grid-cols-4 md:grid-rows-2">

          {/* Card: Valor Actual (Destacada) */}
          <Card className="md:col-span-2 md:row-span-2 !rounded-card !p-8 flex flex-col justify-between relative overflow-hidden group">
            <div className="absolute top-6 right-6 p-3 bg-[#EFEAE2]/50 rounded-2xl text-finflow-dark">
              <DollarSign className="w-6 h-6 stroke-[1.5]" />
            </div>
            <div>
              <p className="text-sm font-medium text-finflow-muted uppercase tracking-wider mb-2">Balance Total</p>
              <p className="text-5xl font-bold text-finflow-dark tracking-tight">
                {formatCurrency(summary.currentValue)}
              </p>
            </div>
            <div className="mt-8">
              <p className="text-sm font-medium text-finflow-muted mb-1">Rendimiento Histórico</p>
              <div className="flex items-baseline gap-3">
                <span className={`text-2xl font-semibold ${summary.totalGain >= 0 ? 'text-finflow-green' : 'text-finflow-rust'}`}>
                  {summary.totalGain >= 0 ? '+' : ''}{formatCurrency(summary.totalGain)}
                </span>
                <span className={`text-sm font-medium px-2.5 py-1 rounded-full ${summary.totalGain >= 0 ? 'bg-finflow-green/10 text-finflow-green' : 'bg-finflow-rust/10 text-finflow-rust'}`}>
                  {summary.totalGain >= 0 ? '+' : ''}{summary.totalGainPercentage.toFixed(2)}%
                </span>
              </div>
            </div>
          </Card>

          {/* Card: Total Invertido */}
          <Card className="md:col-span-2 !rounded-card-sm flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-finflow-muted uppercase tracking-wider mb-1">Capital Invertido</p>
              <p className="text-2xl font-bold text-finflow-dark">
                {formatCurrency(summary.totalInvested)}
              </p>
            </div>
            <div className="p-3 bg-[#EFEAE2]/50 rounded-2xl text-finflow-muted">
              <PieChart className="w-5 h-5 stroke-[1.5]" />
            </div>
          </Card>

          {/* Card: Total Dividendos */}
          <Card className="md:col-span-2 !rounded-card-sm flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-finflow-muted uppercase tracking-wider mb-1">Dividendos Generados</p>
              <p className="text-2xl font-bold text-finflow-green">
                +{formatCurrency(summary.totalDividends)}
              </p>
            </div>
            <div className="p-3 bg-finflow-green/10 rounded-2xl text-finflow-green">
              <Activity className="w-5 h-5 stroke-[1.5]" />
            </div>
          </Card>
        </div>
      )}

      {/* Lista de Inversiones */}
      <Card noPadding className="overflow-hidden">
        <div className="p-6 border-b border-[#EFEAE2]">
          <h2 className="text-lg font-semibold text-finflow-dark">Detalle de Activos</h2>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-finflow-cream/50 text-xs uppercase tracking-wider text-finflow-muted">
                <th className="p-5 font-semibold">Activo</th>
                <th className="p-5 font-semibold">Tipo</th>
                <th className="p-5 font-semibold">Capital Base</th>
                <th className="p-5 font-semibold">Valor Actual</th>
                <th className="p-5 font-semibold">Retorno No Realizado</th>
                <th className="p-5 font-semibold text-right">Gestión</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#EFEAE2]/50">
              {investments?.map((inv) => (
                <tr key={inv.id} className="transition-all hover:bg-white/80 group">
                  <td className="p-5">
                    <div className="flex flex-col">
                      <span className="font-semibold text-finflow-dark">{inv.name}</span>
                      {inv.ticker && <span className="text-xs font-medium text-finflow-muted mt-0.5">{inv.ticker}</span>}
                    </div>
                  </td>
                  <td className="p-5">
                    <span className="inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-medium bg-[#EFEAE2]/50 text-finflow-muted uppercase">
                      {inv.type}
                    </span>
                  </td>
                  <td className="p-5 text-sm font-medium text-finflow-muted">
                    {formatCurrency(inv.contributedCapital)}
                  </td>
                  <td className="p-5 text-sm font-bold text-finflow-dark">
                    {formatCurrency(inv.currentValue)}
                  </td>
                  <td className="p-5">
                    <div className="flex flex-col">
                      <span className={`text-sm font-bold ${inv.unrealizedGainLoss >= 0 ? 'text-finflow-green' : 'text-finflow-rust'}`}>
                        {inv.unrealizedGainLoss >= 0 ? '+' : ''}{formatCurrency(inv.unrealizedGainLoss)}
                      </span>
                      <span className={`text-xs font-medium mt-0.5 ${inv.unrealizedGainLoss >= 0 ? 'text-finflow-green' : 'text-finflow-rust'}`}>
                        ({inv.unrealizedGainLossPercentage >= 0 ? '+' : ''}{inv.unrealizedGainLossPercentage.toFixed(2)}%)
                      </span>
                    </div>
                  </td>
                  <td className="p-5 text-right">
                    {/* Mobile: always-visible ··· button */}
                    <button
                      onClick={() => setActionSheetInvestment(inv)}
                      title="Acciones"
                      aria-label="Acciones de inversión"
                      className="rounded-xl p-2 text-finflow-muted transition-all hover:bg-[#EFEAE2] md:hidden"
                    >
                      <MoreHorizontal className="h-5 w-5" />
                    </button>
                    {/* Desktop: hover-reveal actions (keyboard-accessible via focus-visible) */}
                    <div className="hidden items-center justify-end gap-1 opacity-0 transition-opacity group-hover:opacity-100 focus-within:opacity-100 md:flex">
                      <button
                        onClick={() => setAddingRecordInvestment(inv)}
                        title="Actualizar valor de mercado"
                        className="rounded-xl p-2 text-finflow-muted transition-all hover:bg-finflow-green/10 hover:text-finflow-green focus-visible:opacity-100"
                      >
                        <TrendingUp className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() => setAddingContributionInvestment(inv)}
                        title="Registrar aporte"
                        className="rounded-xl p-2 text-finflow-muted transition-all hover:bg-finflow-blue/10 hover:text-finflow-blue focus-visible:opacity-100"
                      >
                        <PlusCircle className="h-4 w-4" />
                      </button>
                      {inv.isActive && (
                        <button
                          onClick={() => setWithdrawingInvestment(inv)}
                          title="Registrar retiro"
                          className="rounded-xl p-2 text-finflow-muted transition-all hover:bg-finflow-rust/10 hover:text-finflow-rust focus-visible:opacity-100"
                        >
                          <ArrowDownLeft className="h-4 w-4" />
                        </button>
                      )}
                      <button
                        onClick={() => setEditingInvestment(inv)}
                        title="Editar"
                        className="rounded-xl p-2 text-finflow-muted transition-all hover:bg-[#EFEAE2] hover:text-finflow-dark focus-visible:opacity-100"
                      >
                        <Edit2 className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() => setDeletingInvestment(inv)}
                        title="Eliminar"
                        className="rounded-xl p-2 text-finflow-muted transition-all hover:bg-finflow-rust/10 hover:text-finflow-rust focus-visible:opacity-100"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {(!investments || investments.length === 0) && (
                <tr>
                  <td colSpan={6} className="p-16 text-center">
                    <div className="flex flex-col items-center gap-2">
                      <div className="p-4 bg-[#EFEAE2]/30 rounded-full mb-2">
                        <Activity className="h-6 w-6 text-finflow-muted/50" />
                      </div>
                      <p className="text-sm font-medium text-finflow-muted">No hay inversiones registradas.</p>
                      <p className="text-xs text-finflow-muted/70">Comienza añadiendo un nuevo activo a tu portafolio.</p>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Modales */}
      <Modal isOpen={isCreateOpen} onClose={() => setIsCreateOpen(false)} title="Nueva Inversión">
        <CreateInvestmentForm onSuccess={() => setIsCreateOpen(false)} onCancel={() => setIsCreateOpen(false)} />
      </Modal>

      <Modal isOpen={!!editingInvestment} onClose={() => setEditingInvestment(null)} title="Editar Inversión">
        {editingInvestment && (
          <EditInvestmentForm investment={editingInvestment} onSuccess={() => setEditingInvestment(null)} onCancel={() => setEditingInvestment(null)} />
        )}
      </Modal>

      <Modal isOpen={!!addingRecordInvestment} onClose={() => setAddingRecordInvestment(null)} title="Actualizar Valor del Activo">
        {addingRecordInvestment && (
          <AddInvestmentRecordForm
            investment={addingRecordInvestment}
            onSuccess={() => setAddingRecordInvestment(null)}
            onCancel={() => setAddingRecordInvestment(null)}
          />
        )}
      </Modal>

      <Modal isOpen={!!addingContributionInvestment} onClose={() => setAddingContributionInvestment(null)} title="Registrar Aporte">
        {addingContributionInvestment && (
          <AddInvestmentContributionForm
            investment={addingContributionInvestment}
            onSuccess={() => setAddingContributionInvestment(null)}
            onCancel={() => setAddingContributionInvestment(null)}
          />
        )}
      </Modal>

      <Modal isOpen={!!withdrawingInvestment} onClose={() => setWithdrawingInvestment(null)} title="Registrar Retiro">
        {withdrawingInvestment && (
          <WithdrawalModal
            investment={withdrawingInvestment}
            onSuccess={() => setWithdrawingInvestment(null)}
            onCancel={() => setWithdrawingInvestment(null)}
          />
        )}
      </Modal>

      <Modal isOpen={!!deletingInvestment} onClose={() => setDeletingInvestment(null)} title="Eliminar Inversión">
        <div className="space-y-4">
          <p className="text-sm text-finflow-muted leading-relaxed">
            ¿Estás seguro que deseas eliminar la inversión <span className="font-bold text-finflow-dark">{deletingInvestment?.name}</span>? Esta acción también eliminará todo el historial de registros asociados y no se puede deshacer.
          </p>
          <div className="flex justify-end gap-3 mt-6">
            <Button variant="ghost" onClick={() => setDeletingInvestment(null)} className="text-finflow-muted hover:bg-[#EFEAE2] rounded-xl">
              Cancelar
            </Button>
            <Button isLoading={isDeleting} onClick={handleDelete} className="bg-finflow-rust text-white hover:bg-finflow-rust/90 rounded-xl">
              Sí, eliminar
            </Button>
          </div>
        </div>
      </Modal>

      {/* Mobile action sheet for investment row */}
      <BottomSheet
        open={actionSheetInvestment !== null}
        onClose={() => setActionSheetInvestment(null)}
        title={actionSheetInvestment?.name ?? 'Inversión'}
      >
        {actionSheetInvestment && (
          <div className="flex flex-col gap-3 pb-4 pt-1">
            <button
              type="button"
              onClick={() => { setActionSheetInvestment(null); setAddingContributionInvestment(actionSheetInvestment); }}
              className="flex w-full items-center gap-4 rounded-[20px] bg-white/70 p-4 text-left shadow-sm transition-colors hover:bg-[#F3F1EC]"
            >
              <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-finflow-blue/10 text-finflow-blue">
                <PlusCircle className="h-6 w-6" />
              </span>
              <span className="text-base font-medium text-finflow-dark">Aportar</span>
            </button>
            <button
              type="button"
              onClick={() => { setActionSheetInvestment(null); setAddingRecordInvestment(actionSheetInvestment); }}
              className="flex w-full items-center gap-4 rounded-[20px] bg-white/70 p-4 text-left shadow-sm transition-colors hover:bg-[#F3F1EC]"
            >
              <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-finflow-green/10 text-finflow-green">
                <TrendingUp className="h-6 w-6" />
              </span>
              <span className="text-base font-medium text-finflow-dark">Actualizar valor</span>
            </button>

            <div className="my-1 border-t border-[#EFEAE2]" />

            <button
              type="button"
              onClick={() => { setActionSheetInvestment(null); setEditingInvestment(actionSheetInvestment); }}
              className="flex w-full items-center gap-3 rounded-[16px] px-4 py-3 text-left transition-colors hover:bg-[#F3F1EC]"
            >
              <Edit2 className="h-5 w-5 text-finflow-muted" />
              <span className="text-sm font-medium text-finflow-dark">Editar</span>
            </button>
            {actionSheetInvestment.isActive && (
              <button
                type="button"
                onClick={() => { setActionSheetInvestment(null); setWithdrawingInvestment(actionSheetInvestment); }}
                className="flex w-full items-center gap-3 rounded-[16px] px-4 py-3 text-left transition-colors hover:bg-[#F3F1EC]"
              >
                <ArrowDownLeft className="h-5 w-5 text-finflow-muted" />
                <span className="text-sm font-medium text-finflow-dark">Retirar</span>
              </button>
            )}
            <button
              type="button"
              onClick={() => { setActionSheetInvestment(null); setDeletingInvestment(actionSheetInvestment); }}
              className="flex w-full items-center gap-3 rounded-[16px] px-4 py-3 text-left transition-colors hover:bg-finflow-rust/8"
            >
              <Trash2 className="h-5 w-5 text-finflow-rust" />
              <span className="text-sm font-medium text-finflow-rust">Eliminar</span>
            </button>
          </div>
        )}
      </BottomSheet>
    </div>
  );
}
