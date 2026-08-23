import { useState } from 'react';
import { format } from 'date-fns';
import { Pencil, Trash2, Undo2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import { parseDateOnly } from '../../utils/dateOnly';
import { BottomSheet } from '../ui/BottomSheet';
import type { Expense } from '../../types/expense.types';

const money = (v: number) =>
  new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(v);

const formatPaymentMethod = (pm: string) =>
  pm.replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());

interface ExpenseCardProps {
  expense: Expense;
  onEdit: (expense: Expense) => void;
  onDelete: (expense: Expense) => void;
}

export function ExpenseCard({ expense, onEdit, onDelete }: ExpenseCardProps) {
  const [menuOpen, setMenuOpen] = useState(false);

  const primaryLabel = expense.description || expense.merchant || expense.categoryName;
  const subtitle = `${expense.categoryName} · ${formatPaymentMethod(expense.paymentMethod)}`;

  return (
    <>
      <button
        type="button"
        onClick={() => setMenuOpen(true)}
        className="flex w-full items-center justify-between rounded-[20px] border border-[#EFEAE2] bg-white/70 px-4 py-3.5 text-left shadow-sm transition-colors active:bg-[#F3F1EC]"
      >
        {/* Left: dot + name + subtitle */}
        <div className="flex min-w-0 items-center gap-3">
          <span
            className="h-3 w-3 shrink-0 rounded-full"
            style={{ backgroundColor: expense.categoryColor }}
            aria-hidden="true"
          />
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold text-finflow-dark">{primaryLabel}</p>
            <p className="truncate text-xs text-finflow-muted">{subtitle}</p>
            {expense.tags.length > 0 && (
              <div className="mt-1 flex flex-wrap gap-1">
                {expense.tags.slice(0, 2).map((tag) => (
                  <span
                    key={tag.id}
                    className="rounded-full px-1.5 py-0.5 text-[10px] text-white"
                    style={{ backgroundColor: tag.color ?? 'var(--color-finflow-blue)' }}
                  >
                    {tag.name}
                  </span>
                ))}
                {expense.tags.length > 2 && (
                  <span className="text-[10px] text-finflow-muted">+{expense.tags.length - 2}</span>
                )}
              </div>
            )}
          </div>
        </div>

        {/* Right: amount + date */}
        <div className="ml-3 shrink-0 text-right">
          <p className="text-sm font-semibold text-finflow-rust">-{money(expense.amount)}</p>
          <p className="mt-0.5 text-xs text-finflow-muted">
            {format(parseDateOnly(expense.date), 'dd/MM/yy')}
          </p>
        </div>
      </button>

      <BottomSheet open={menuOpen} onClose={() => setMenuOpen(false)} title={primaryLabel ?? 'Gasto'}>
        <div className="flex flex-col gap-3 pb-4 pt-1">
          <Link
            to={`/reimbursements?expenseId=${expense.id}`}
            onClick={() => setMenuOpen(false)}
            className="flex w-full items-center gap-4 rounded-[20px] bg-white/70 p-4 shadow-sm transition-colors hover:bg-[#F3F1EC]"
          >
            <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-finflow-blue/10 text-finflow-blue">
              <Undo2 className="h-6 w-6" />
            </span>
            <span className="text-base font-medium text-finflow-dark">Reembolso</span>
          </Link>

          <button
            type="button"
            onClick={() => { setMenuOpen(false); onEdit(expense); }}
            className="flex w-full items-center gap-4 rounded-[20px] bg-white/70 p-4 text-left shadow-sm transition-colors hover:bg-[#F3F1EC]"
          >
            <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-[#EFEAE2] text-finflow-blue">
              <Pencil className="h-6 w-6" />
            </span>
            <span className="text-base font-medium text-finflow-dark">Editar</span>
          </button>

          <button
            type="button"
            onClick={() => { setMenuOpen(false); onDelete(expense); }}
            className="flex w-full items-center gap-4 rounded-[20px] bg-finflow-rust/8 p-4 text-left shadow-sm transition-colors hover:bg-finflow-rust/15"
          >
            <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-finflow-rust/12 text-finflow-rust">
              <Trash2 className="h-6 w-6" />
            </span>
            <span className="text-base font-medium text-finflow-rust">Eliminar</span>
          </button>
        </div>
      </BottomSheet>
    </>
  );
}
