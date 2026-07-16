import { useState } from 'react';
import { Plus, Edit2, Trash2, Wallet, TrendingDown, CalendarClock, CreditCard } from 'lucide-react';
import {
  useDebts,
  useDebtSummary,
  useDeleteDebt,
} from '../features/debts/hooks/useDebts';
import { CreateDebtForm } from '../features/debts/components/CreateDebtForm';
import { EditDebtForm } from '../features/debts/components/EditDebtForm';
import { AddDebtPaymentForm } from '../features/debts/components/AddDebtPaymentForm';
import { formatCurrency } from '../utils/formatCurrency';
import type { Debt } from '../types/debt.types';
import { DEBT_TYPE_LABELS } from '../types/debt.types';
import {
  Button,
  PageSpinner,
  Modal,
  Card,
  KpiCard,
  Table,
  TableHead,
  TableBody,
  Th,
  Tr,
  Td,
  TableEmpty,
  Badge,
  ConfirmDialog,
} from '../components/ui';

export default function DebtsPage() {
  const { data: summary, isLoading: loadingSummary } = useDebtSummary();
  const { data: debts, isLoading: loadingDebts } = useDebts();
  const { mutate: deleteDebt, isPending: isDeleting } = useDeleteDebt();

  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [editingDebt, setEditingDebt] = useState<Debt | null>(null);
  const [deletingDebt, setDeletingDebt] = useState<Debt | null>(null);
  const [payingDebt, setPayingDebt] = useState<Debt | null>(null);

  if (loadingSummary || loadingDebts) {
    return <PageSpinner label="Cargando deudas..." />;
  }

  const handleDelete = () => {
    if (deletingDebt) {
      deleteDebt(deletingDebt.id, { onSuccess: () => setDeletingDebt(null) });
    }
  };

  return (
    <div className="space-y-8 bg-[#FBF9F4] min-h-screen p-4 md:p-8 font-sans">
      {/* Encabezado */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold text-[#2C2A29] tracking-tight">Deudas</h1>
          <p className="text-sm text-[#7C756E] mt-1">Controla tus pasivos y su evolución de pago</p>
        </div>
        <Button onClick={() => setIsCreateOpen(true)} leftIcon={<Plus className="h-4 w-4" />}>
          Nueva Deuda
        </Button>
      </div>

      {/* Resumen — Bento Grid */}
      {summary && (
        <div className="grid grid-cols-1 gap-6 md:grid-cols-4 md:grid-rows-2">
          {/* Saldo total pendiente — destacada */}
          <div className="md:col-span-2 md:row-span-2 rounded-[28px] border border-[#EFEAE2] bg-white/60 backdrop-blur-xl p-8 shadow-sm flex flex-col justify-between relative overflow-hidden">
            <div className="absolute top-6 right-6 p-3 bg-[#D9A46B]/10 rounded-2xl text-[#D9A46B]">
              <Wallet className="w-6 h-6 stroke-[1.5]" />
            </div>
            <div>
              <p className="text-sm font-medium text-[#7C756E] uppercase tracking-wider mb-2">
                Saldo Total Pendiente
              </p>
              <p className="text-5xl font-bold text-[#2C2A29] tracking-tight">
                {formatCurrency(summary.totalCurrentBalance)}
              </p>
            </div>
            <div className="mt-8">
              <div className="flex items-center justify-between mb-2">
                <p className="text-sm font-medium text-[#7C756E]">Progreso de Pago</p>
                <span className="text-sm font-semibold text-[#8FA888]">
                  {summary.totalPaidPercentage.toFixed(1)}%
                </span>
              </div>
              <div className="h-2 w-full rounded-full bg-[#EFEAE2] overflow-hidden">
                <div
                  className="h-full rounded-full bg-[#8FA888] transition-all"
                  style={{ width: `${Math.min(summary.totalPaidPercentage, 100)}%` }}
                />
              </div>
              <p className="mt-2 text-xs text-[#7C756E]">
                {formatCurrency(summary.totalPaid)} pagado de {formatCurrency(summary.totalOriginal)}
              </p>
            </div>
          </div>

          <KpiCard
            className="md:col-span-2"
            label="Deuda Original"
            value={formatCurrency(summary.totalOriginal)}
            icon={<CreditCard className="w-5 h-5 stroke-[1.5]" />}
          />

          <Card className="md:col-span-2 flex flex-col gap-3">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium uppercase tracking-wide text-[#7C756E]">
                Próximos Vencimientos
              </span>
              <CalendarClock className="w-5 h-5 text-[#7C756E]" />
            </div>
            {summary.upcomingPayments.length === 0 ? (
              <p className="text-sm text-[#7C756E]">Sin vencimientos próximos configurados.</p>
            ) : (
              <ul className="flex flex-col gap-2">
                {summary.upcomingPayments.slice(0, 3).map((p) => (
                  <li key={p.debtId} className="flex items-center justify-between text-sm">
                    <span className="text-[#2C2A29] font-medium truncate">{p.debtName}</span>
                    <span className="text-[#7C756E] shrink-0 ml-2">
                      Día {p.dueDay}
                      {p.minimumPayment ? ` · ${formatCurrency(p.minimumPayment)}` : ''}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </Card>
        </div>
      )}

      {/* Tabla de deudas */}
      <Card noPadding>
        <div className="p-6 border-b border-[#EFEAE2]">
          <h2 className="text-lg font-semibold text-[#2C2A29]">Detalle de Deudas</h2>
        </div>
        <Table>
          <TableHead>
            <Th>Deuda</Th>
            <Th>Tipo</Th>
            <Th>Saldo Original</Th>
            <Th>Saldo Pendiente</Th>
            <Th>Progreso</Th>
            <Th className="text-right">Gestión</Th>
          </TableHead>
          <TableBody>
            {debts?.map((debt) => (
              <Tr key={debt.id}>
                <Td>
                  <div className="flex flex-col">
                    <span className="font-semibold text-[#2C2A29]">{debt.name}</span>
                    {debt.creditor && (
                      <span className="text-xs text-[#7C756E] mt-0.5">{debt.creditor}</span>
                    )}
                  </div>
                </Td>
                <Td>
                  <Badge>{DEBT_TYPE_LABELS[debt.type]}</Badge>
                </Td>
                <Td>{formatCurrency(debt.originalAmount)}</Td>
                <Td>
                  <span className="font-bold text-[#2C2A29]">
                    {formatCurrency(debt.currentBalance)}
                  </span>
                  {debt.isPaidOff && (
                    <span className="ml-2 text-xs font-medium text-[#8FA888]">Liquidada</span>
                  )}
                </Td>
                <Td>
                  <div className="flex items-center gap-2 w-32">
                    <div className="h-1.5 flex-1 rounded-full bg-[#EFEAE2] overflow-hidden">
                      <div
                        className="h-full rounded-full bg-[#8FA888]"
                        style={{ width: `${Math.min(debt.paidPercentage, 100)}%` }}
                      />
                    </div>
                    <span className="text-xs font-medium text-[#7C756E]">
                      {debt.paidPercentage.toFixed(0)}%
                    </span>
                  </div>
                </Td>
                <Td className="text-right">
                  <div className="flex items-center justify-end gap-1">
                    <button
                      onClick={() => setPayingDebt(debt)}
                      disabled={debt.isPaidOff}
                      title="Registrar Pago"
                      className="p-2 text-[#7C756E] hover:text-[#8FA888] hover:bg-[#8FA888]/10 rounded-xl transition-all disabled:opacity-30 disabled:pointer-events-none"
                    >
                      <TrendingDown className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => setEditingDebt(debt)}
                      title="Editar"
                      className="p-2 text-[#7C756E] hover:text-[#2C2A29] hover:bg-[#EFEAE2] rounded-xl transition-all"
                    >
                      <Edit2 className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => setDeletingDebt(debt)}
                      title="Eliminar"
                      className="p-2 text-[#7C756E] hover:text-[#C97B63] hover:bg-[#C97B63]/10 rounded-xl transition-all"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </Td>
              </Tr>
            ))}
            {(!debts || debts.length === 0) && (
              <TableEmpty colSpan={6} message="No hay deudas registradas." />
            )}
          </TableBody>
        </Table>
      </Card>

      {/* Modales */}
      <Modal isOpen={isCreateOpen} onClose={() => setIsCreateOpen(false)} title="Nueva Deuda">
        <CreateDebtForm onSuccess={() => setIsCreateOpen(false)} onCancel={() => setIsCreateOpen(false)} />
      </Modal>

      <Modal isOpen={!!editingDebt} onClose={() => setEditingDebt(null)} title="Editar Deuda">
        {editingDebt && (
          <EditDebtForm
            debt={editingDebt}
            onSuccess={() => setEditingDebt(null)}
            onCancel={() => setEditingDebt(null)}
          />
        )}
      </Modal>

      <Modal isOpen={!!payingDebt} onClose={() => setPayingDebt(null)} title="Registrar Pago">
        {payingDebt && (
          <AddDebtPaymentForm
            debt={payingDebt}
            onSuccess={() => setPayingDebt(null)}
            onCancel={() => setPayingDebt(null)}
          />
        )}
      </Modal>

      <ConfirmDialog
        isOpen={!!deletingDebt}
        title="Eliminar Deuda"
        description={`¿Estás seguro que deseas eliminar "${deletingDebt?.name}"? Esta acción también eliminará el historial de pagos asociado y no se puede deshacer.`}
        isLoading={isDeleting}
        onConfirm={handleDelete}
        onCancel={() => setDeletingDebt(null)}
      />
    </div>
  );
}