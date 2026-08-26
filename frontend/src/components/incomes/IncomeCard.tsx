import { useState } from 'react';
import { format } from 'date-fns';
import { Pencil, Trash2 } from 'lucide-react';
import { parseDateOnly } from '../../utils/dateOnly';
import { BottomSheet } from '../ui/BottomSheet';
import type { Income } from '../../types/income.types';

const money = (v: number) =>
  new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(v);

interface IncomeCardProps {
  income: Income;
  onEdit: (income: Income) => void;
  onDelete: (income: Income) => void;
}

export function IncomeCard({ income, onEdit, onDelete }: IncomeCardProps) {
  const [menuOpen, setMenuOpen] = useState(false);

  const primaryLabel = income.description || income.source || income.categoryName;
  // Si la fuente ya se usó como título, no la repetimos debajo.
  const subtitle =
    income.source && income.source !== primaryLabel
      ? `${income.categoryName} · ${income.source}`
      : income.categoryName;

  return (
    <>
      <button
        type="button"
        onClick={() => setMenuOpen(true)}
        className="flex w-full items-center justify-between rounded-[20px] border border-[#EFEAE2] bg-white/70 px-4 py-3.5 text-left shadow-sm transition-colors active:bg-[#F3F1EC]"
      >
        {/* Izquierda: punto de categoría + nombre + subtítulo */}
        <div className="flex min-w-0 items-center gap-3">
          <span
            className="h-3 w-3 shrink-0 rounded-full"
            style={{ backgroundColor: income.categoryColor }}
            aria-hidden="true"
          />
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold text-finflow-dark">{primaryLabel}</p>
            <p className="truncate text-xs text-finflow-muted">{subtitle}</p>
          </div>
        </div>

        {/* Derecha: monto + fecha */}
        <div className="ml-3 shrink-0 text-right">
          <p className="text-sm font-semibold text-finflow-blue">+{money(income.amount)}</p>
          <p className="mt-0.5 text-xs text-finflow-muted">
            {format(parseDateOnly(income.date), 'dd/MM/yy')}
          </p>
        </div>
      </button>

      <BottomSheet open={menuOpen} onClose={() => setMenuOpen(false)} title={primaryLabel}>
        <div className="flex flex-col gap-3 pb-4 pt-1">
          <button
            type="button"
            onClick={() => { setMenuOpen(false); onEdit(income); }}
            className="flex w-full items-center gap-4 rounded-[20px] bg-white/70 p-4 text-left shadow-sm transition-colors hover:bg-[#F3F1EC]"
          >
            <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-[#EFEAE2] text-finflow-blue">
              <Pencil className="h-6 w-6" />
            </span>
            <span className="text-base font-medium text-finflow-dark">Editar</span>
          </button>

          <button
            type="button"
            onClick={() => { setMenuOpen(false); onDelete(income); }}
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
