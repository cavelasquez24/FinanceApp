// src/pages/InvestmentsPage.tsx
import { useState } from 'react';
import { Plus, Edit2, Trash2, TrendingUp, DollarSign, PieChart, Activity, PlusCircle } from 'lucide-react';
import { AddInvestmentContributionForm } from '../features/investments/components/AddInvestmentContributionForm';
import { useInvestments, useInvestmentSummary, useDeleteInvestment } from '../features/investments/hooks/useInvestments';
import { CreateInvestmentForm } from '../features/investments/components/CreateInvestmentForm';
import { EditInvestmentForm } from '../features/investments/components/EditInvestmentForm';
import { AddInvestmentRecordForm } from '../features/investments/components/AddInvestmentRecordForm';
import { formatCurrency } from '../utils/formatCurrency';
import type { Investment } from '../types/investment.types';
import { Button, Spinner, Modal } from '../components/ui';

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

  if (loadingSummary || loadingInvestments) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <div className="flex flex-col items-center gap-3 text-[#7C756E]">
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
    <div className="space-y-8 bg-[#FBF9F4] min-h-screen p-4 md:p-8 font-sans">
      
      {/* Encabezado */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold text-[#2C2A29] tracking-tight">Portafolio</h1>
          <p className="text-sm text-[#7C756E] mt-1">Monitorea y gestiona tus activos financieros</p>
        </div>
        <Button 
          onClick={() => setIsCreateOpen(true)} 
          className="flex items-center gap-2 bg-[#2C2A29] text-[#FBF9F4] hover:bg-[#2C2A29]/90 rounded-2xl px-5 py-2.5 transition-all shadow-md"
        >
          <Plus className="h-4 w-4" />
          Nueva Inversión
        </Button>
      </div>

      {/* Resumen del Portafolio - Diseño Bento Grid / Glassmorphism */}
      {summary && (
        <div className="grid grid-cols-1 gap-6 md:grid-cols-4 md:grid-rows-2">
          
          {/* Bento Card: Valor Actual (Destacada) */}
          <div className="md:col-span-2 md:row-span-2 rounded-[28px] border border-[#EFEAE2] bg-white/60 backdrop-blur-xl p-8 shadow-sm flex flex-col justify-between relative overflow-hidden group">
            <div className="absolute top-6 right-6 p-3 bg-[#EFEAE2]/50 rounded-2xl text-[#2C2A29]">
              <DollarSign className="w-6 h-6 stroke-[1.5]" />
            </div>
            <div>
              <p className="text-sm font-medium text-[#7C756E] uppercase tracking-wider mb-2">Balance Total</p>
              <p className="text-5xl font-bold text-[#2C2A29] tracking-tight">
                {formatCurrency(summary.currentValue)}
              </p>
            </div>
            <div className="mt-8">
              <p className="text-sm font-medium text-[#7C756E] mb-1">Rendimiento Histórico</p>
              <div className="flex items-baseline gap-3">
                <span className={`text-2xl font-semibold ${summary.totalGain >= 0 ? 'text-[#8FA888]' : 'text-[#C97B63]'}`}>
                  {summary.totalGain >= 0 ? '+' : ''}{formatCurrency(summary.totalGain)}
                </span>
                <span className={`text-sm font-medium px-2.5 py-1 rounded-full ${summary.totalGain >= 0 ? 'bg-[#8FA888]/10 text-[#8FA888]' : 'bg-[#C97B63]/10 text-[#C97B63]'}`}>
                  {summary.totalGain >= 0 ? '+' : ''}{summary.totalGainPercentage.toFixed(2)}%
                </span>
              </div>
            </div>
          </div>

          {/* Bento Card: Total Invertido */}
          <div className="md:col-span-2 rounded-[28px] border border-[#EFEAE2] bg-white/60 backdrop-blur-xl p-6 shadow-sm flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-[#7C756E] uppercase tracking-wider mb-1">Capital Invertido</p>
              <p className="text-2xl font-bold text-[#2C2A29]">
                {formatCurrency(summary.totalInvested)}
              </p>
            </div>
            <div className="p-3 bg-[#EFEAE2]/50 rounded-2xl text-[#7C756E]">
              <PieChart className="w-5 h-5 stroke-[1.5]" />
            </div>
          </div>

          {/* Bento Card: Total Dividendos */}
          <div className="md:col-span-2 rounded-[28px] border border-[#EFEAE2] bg-white/60 backdrop-blur-xl p-6 shadow-sm flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-[#7C756E] uppercase tracking-wider mb-1">Dividendos Generados</p>
              <p className="text-2xl font-bold text-[#8FA888]">
                +{formatCurrency(summary.totalDividends)}
              </p>
            </div>
            <div className="p-3 bg-[#8FA888]/10 rounded-2xl text-[#8FA888]">
              <Activity className="w-5 h-5 stroke-[1.5]" />
            </div>
          </div>
        </div>
      )}

      {/* Lista de Inversiones - Tarjeta Modular Estilizada */}
      <div className="rounded-[28px] border border-[#EFEAE2] bg-white/60 backdrop-blur-xl shadow-sm overflow-hidden">
        <div className="p-6 border-b border-[#EFEAE2]">
          <h2 className="text-lg font-semibold text-[#2C2A29]">Detalle de Activos</h2>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-[#FBF9F4]/50 text-xs uppercase tracking-wider text-[#7C756E]">
                <th className="p-5 font-semibold">Activo</th>
                <th className="p-5 font-semibold">Tipo</th>
                <th className="p-5 font-semibold">Capital Base</th>
                <th className="p-5 font-semibold">Valor Actual</th>
                <th className="p-5 font-semibold">Retorno</th>
                <th className="p-5 font-semibold text-right">Gestión</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#EFEAE2]/50">
              {investments?.map((inv) => (
                <tr key={inv.id} className="transition-all hover:bg-white/80 group">
                  <td className="p-5">
                    <div className="flex flex-col">
                      <span className="font-semibold text-[#2C2A29]">{inv.name}</span>
                      {inv.ticker && <span className="text-xs font-medium text-[#7C756E] mt-0.5">{inv.ticker}</span>}
                    </div>
                  </td>
                  <td className="p-5">
                    <span className="inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-medium bg-[#EFEAE2]/50 text-[#7C756E] uppercase">
                      {inv.type}
                    </span>
                  </td>
                  <td className="p-5 text-sm font-medium text-[#7C756E]">
                    {formatCurrency(inv.initialAmount)}
                  </td>
                  <td className="p-5 text-sm font-bold text-[#2C2A29]">
                    {formatCurrency(inv.currentValue)}
                  </td>
                  <td className="p-5">
                    <div className="flex flex-col">
                      <span className={`text-sm font-bold ${inv.gainLoss >= 0 ? 'text-[#8FA888]' : 'text-[#C97B63]'}`}>
                        {inv.gainLoss >= 0 ? '+' : ''}{formatCurrency(inv.gainLoss)}
                      </span>
                      <span className={`text-xs font-medium mt-0.5 ${inv.gainLoss >= 0 ? 'text-[#8FA888]' : 'text-[#C97B63]'}`}>
                        ({inv.gainLossPercentage}%)
                      </span>
                    </div>
                  </td>
                  <td className="p-5 text-right">
                    <div className="flex items-center justify-end gap-1 opacity-0 transition-opacity group-hover:opacity-100">
                      <button 
                        onClick={() => setAddingRecordInvestment(inv)}
                        title="Añadir Valor/Registro" 
                        className="p-2 text-[#7C756E] hover:text-[#8FA888] hover:bg-[#8FA888]/10 rounded-xl transition-all"
                      >
                        <TrendingUp className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() => setAddingContributionInvestment(inv)}
                        title="Registrar aporte"
                        className="p-2 text-[#7C756E] hover:text-[#5C7A99] hover:bg-[#5C7A99]/10 rounded-xl transition-all"
                      >
                        <PlusCircle className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() => setEditingInvestment(inv)} 
                        title="Editar" 
                        className="p-2 text-[#7C756E] hover:text-[#2C2A29] hover:bg-[#EFEAE2] rounded-xl transition-all"
                      >
                        <Edit2 className="h-4 w-4" />
                      </button>
                      <button 
                        onClick={() => setDeletingInvestment(inv)} 
                        title="Eliminar" 
                        className="p-2 text-[#7C756E] hover:text-[#C97B63] hover:bg-[#C97B63]/10 rounded-xl transition-all"
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
                        <Activity className="h-6 w-6 text-[#7C756E]/50" />
                      </div>
                      <p className="text-sm font-medium text-[#7C756E]">No hay inversiones registradas.</p>
                      <p className="text-xs text-[#7C756E]/70">Comienza añadiendo un nuevo activo a tu portafolio.</p>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modales */}
      <Modal isOpen={isCreateOpen} onClose={() => setIsCreateOpen(false)} title="Nueva Inversión">
        <CreateInvestmentForm onSuccess={() => setIsCreateOpen(false)} onCancel={() => setIsCreateOpen(false)} />
      </Modal>

      <Modal isOpen={!!editingInvestment} onClose={() => setEditingInvestment(null)} title="Editar Inversión">
        {editingInvestment && (
          <EditInvestmentForm investment={editingInvestment} onSuccess={() => setEditingInvestment(null)} onCancel={() => setEditingInvestment(null)} />
        )}
      </Modal>

      {/* Modal Faltante: Añadir Registro */}
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

      <Modal isOpen={!!deletingInvestment} onClose={() => setDeletingInvestment(null)} title="Eliminar Inversión">
        <div className="space-y-4">
          <p className="text-sm text-[#7C756E] leading-relaxed">
            ¿Estás seguro que deseas eliminar la inversión <span className="font-bold text-[#2C2A29]">{deletingInvestment?.name}</span>? Esta acción también eliminará todo el historial de registros asociados y no se puede deshacer.
          </p>
          <div className="flex justify-end gap-3 mt-6">
            <Button variant="ghost" onClick={() => setDeletingInvestment(null)} className="text-[#7C756E] hover:bg-[#EFEAE2] rounded-xl">
              Cancelar
            </Button>
            <Button isLoading={isDeleting} onClick={handleDelete} className="bg-[#C97B63] text-white hover:bg-[#C97B63]/90 rounded-xl">
              Sí, eliminar
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}