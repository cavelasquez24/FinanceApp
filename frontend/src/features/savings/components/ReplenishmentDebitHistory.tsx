import type { ReplenishmentDebitDto } from "../../../types/savingsReplenishment.types";
import { formatCurrency } from "../../../utils/formatCurrency";
import { formatShortDate } from "../../../utils/formatDate";

interface Props {
  debits: ReplenishmentDebitDto[];
}

const typeLabels: Record<ReplenishmentDebitDto["type"], string> = {
  Automatic: "Automático",
  Manual: "Manual",
  Adjustment: "Ajuste",
};

export function ReplenishmentDebitHistory({ debits }: Props) {
  if (debits.length === 0) {
    return (
      <p className="py-3 text-center text-xs text-finflow-muted">
        Aún no hay débitos registrados.
      </p>
    );
  }

  return (
    <ul className="space-y-2">
      {debits.map((debit) => (
        <li
          key={debit.id}
          className="flex items-center justify-between rounded-xl border border-[#E8E1D8] bg-white/70 px-3 py-2 text-sm"
        >
          <div className="min-w-0">
            <span className="block text-finflow-dark">
              {formatShortDate(debit.debitDate)}
            </span>
            <span className="block text-xs text-finflow-muted">
              {typeLabels[debit.type]}
              {debit.notes ? ` · ${debit.notes}` : ""}
            </span>
          </div>
          <strong className="shrink-0 text-finflow-green">
            +{formatCurrency(debit.amount)}
          </strong>
        </li>
      ))}
    </ul>
  );
}
