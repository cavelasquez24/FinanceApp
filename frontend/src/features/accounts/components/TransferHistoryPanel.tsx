import { format } from 'date-fns';
import { es } from 'date-fns/locale';
import { ArrowRight, ArrowRightLeft } from 'lucide-react';
import { parseDateOnly } from '../../../utils/dateOnly';
import { Card, Spinner } from '../../../components/ui';
import type { AccountTransferSummaryDto } from '../../../types/accountTransfer.types';

interface Props {
  transfers: AccountTransferSummaryDto[];
  isLoading?: boolean;
}

const money = (value: number) =>
  new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(value);

/** Agrupa por fecha conservando el orden descendente que ya trae el backend. */
function groupByDate(transfers: AccountTransferSummaryDto[]) {
  const groups = new Map<string, AccountTransferSummaryDto[]>();
  for (const transfer of transfers) {
    const bucket = groups.get(transfer.transferDate);
    if (bucket) bucket.push(transfer);
    else groups.set(transfer.transferDate, [transfer]);
  }
  return [...groups.entries()];
}

export function TransferHistoryPanel({ transfers, isLoading = false }: Props) {
  const groups = groupByDate(transfers);

  return (
    <Card className="!rounded-[24px]">
      <h2 className="font-serif text-lg font-medium text-finflow-dark">Transferencias</h2>
      <p className="mt-1 text-sm text-finflow-muted">
        Movimientos entre tus propias cuentas. No son ingresos ni gastos.
      </p>

      {isLoading ? (
        <div className="flex items-center justify-center py-10">
          <Spinner />
        </div>
      ) : transfers.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 py-10 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-[#EFEAE2]/60">
            <ArrowRightLeft className="h-5 w-5 text-finflow-muted" strokeWidth={1.5} />
          </div>
          <p className="text-sm font-medium text-finflow-dark">Aún no hay transferencias</p>
          <p className="max-w-xs text-xs text-finflow-muted">
            Usa el botón «Transferir» para mover dinero entre tus cuentas.
          </p>
        </div>
      ) : (
        <div className="mt-5 space-y-5">
          {groups.map(([date, items]) => (
            <div key={date}>
              <h3 className="mb-2 text-xs font-semibold uppercase tracking-wider text-finflow-muted">
                {format(parseDateOnly(date), "d 'de' MMMM yyyy", { locale: es })}
              </h3>
              <div className="divide-y divide-[#EFEAE2]">
                {items.map((transfer) => (
                  <div
                    key={transfer.id}
                    className="flex items-center justify-between gap-4 py-3 text-sm"
                  >
                    <div className="flex min-w-0 items-center gap-3">
                      <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-finflow-blue/10 text-finflow-blue">
                        <ArrowRightLeft className="h-4 w-4" />
                      </span>
                      <div className="min-w-0">
                        <p className="flex items-center gap-1.5 font-medium text-finflow-dark">
                          <span className="truncate">{transfer.fromAccountName}</span>
                          <ArrowRight
                            className="h-3.5 w-3.5 shrink-0 text-finflow-muted"
                            aria-label="hacia"
                          />
                          <span className="truncate">{transfer.toAccountName}</span>
                        </p>
                        {transfer.description && (
                          <p className="truncate text-xs text-finflow-muted">
                            {transfer.description}
                          </p>
                        )}
                      </div>
                    </div>
                    <span className="shrink-0 font-medium text-finflow-dark">
                      {money(transfer.amount)}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </Card>
  );
}
