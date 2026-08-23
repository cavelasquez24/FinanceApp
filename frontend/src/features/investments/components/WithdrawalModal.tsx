import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { todayDateOnly } from '../../../utils/dateOnly';
import { useWithdrawInvestment } from '../hooks/useInvestments';
import { Button, Input } from '../../../components/ui';
import { formatCurrency } from '../../../utils/formatCurrency';
import type { Investment } from '../../../types/investment.types';

interface Props {
  investment: Investment;
  onSuccess: () => void;
  onCancel: () => void;
}

const withdrawalSchema = z.object({
  withdrawalAmount: z.number({ message: 'Ingresa el monto del retiro' }).positive('Debe ser mayor a cero'),
  capitalReturned: z.number({ message: 'Ingresa el capital retornado' }).nonnegative('No puede ser negativo'),
  fee: z.number().nonnegative('No puede ser negativo').default(0),
  withdrawalDate: z.string().min(1, 'La fecha es obligatoria'),
  notes: z.string().max(500).optional(),
}).superRefine((data, ctx) => {
  if (data.capitalReturned > data.withdrawalAmount) {
    ctx.addIssue({ code: 'custom', path: ['capitalReturned'], message: 'No puede superar el monto del retiro' });
  }
  if (data.fee >= data.withdrawalAmount) {
    ctx.addIssue({ code: 'custom', path: ['fee'], message: 'La comisión debe ser menor al monto del retiro' });
  }
  if (data.capitalReturned + data.fee > data.withdrawalAmount) {
    ctx.addIssue({ code: 'custom', path: ['capitalReturned'], message: 'Capital retornado + comisión no puede superar el monto del retiro' });
  }
});

type WithdrawalFormInput = z.input<typeof withdrawalSchema>;
type WithdrawalFormValues = z.infer<typeof withdrawalSchema>;

export function WithdrawalModal({ investment, onSuccess, onCancel }: Props) {
  const { mutate: withdraw, isPending } = useWithdrawInvestment();

  const { register, handleSubmit, control, formState: { errors } } = useForm<WithdrawalFormInput, any, WithdrawalFormValues>({
    resolver: zodResolver(withdrawalSchema),
    defaultValues: {
      withdrawalAmount: undefined as unknown as number,
      capitalReturned: undefined as unknown as number,
      fee: 0,
      withdrawalDate: todayDateOnly(),
    },
  });

  const [withdrawalAmount, capitalReturned, fee] = useWatch({
    control,
    name: ['withdrawalAmount', 'capitalReturned', 'fee'],
  });

  const safeAmount = Number(withdrawalAmount) || 0;
  const safeCapital = Number(capitalReturned) || 0;
  const safeFee = Number(fee) || 0;
  const netCash = safeAmount - safeFee;
  const realizedGain = safeAmount - safeCapital - safeFee;
  const remainingCapital = investment.contributedCapital - safeCapital;
  const remainingValue = investment.currentValue - safeAmount;

  const onSubmit = (data: WithdrawalFormValues) => {
    withdraw(
      { id: investment.id, data: { ...data, fee: data.fee ?? 0 } },
      { onSuccess }
    );
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
      {/* Contexto de la inversión */}
      <div className="rounded-2xl bg-finflow-cream border border-[#EFEAE2] p-4 space-y-1 text-sm">
        <p className="font-semibold text-finflow-dark">{investment.name}</p>
        <div className="flex gap-6 text-finflow-muted">
          <span>Capital base: <span className="font-medium text-finflow-dark">{formatCurrency(investment.contributedCapital)}</span></span>
          <span>Valor actual: <span className="font-medium text-finflow-dark">{formatCurrency(investment.currentValue)}</span></span>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <Input
          label="Monto del Retiro"
          type="number"
          step="0.01"
          min="0.01"
          placeholder="0.00"
          error={errors.withdrawalAmount?.message}
          {...register('withdrawalAmount', { valueAsNumber: true })}
        />
        <Input
          label="Capital Retornado"
          type="number"
          step="0.01"
          min="0"
          placeholder="Porción del monto que es devolución de capital"
          error={errors.capitalReturned?.message}
          {...register('capitalReturned', { valueAsNumber: true })}
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <Input
          label="Comisión / Fee (Opcional)"
          type="number"
          step="0.01"
          min="0"
          placeholder="0.00"
          error={errors.fee?.message}
          {...register('fee', { valueAsNumber: true })}
        />
        <Input
          label="Fecha del Retiro"
          type="date"
          error={errors.withdrawalDate?.message}
          {...register('withdrawalDate')}
        />
      </div>

      <Input
        label="Notas (Opcional)"
        placeholder="Ej: Cierre parcial de posición"
        error={errors.notes?.message}
        {...register('notes')}
      />

      {/* Panel de cálculo en tiempo real */}
      {safeAmount > 0 && (
        <div className="rounded-2xl border border-[#EFEAE2] bg-white/60 p-4 space-y-2 text-sm">
          <p className="text-xs font-semibold text-finflow-muted uppercase tracking-wider mb-3">Resumen del Retiro</p>
          <div className="flex justify-between">
            <span className="text-finflow-muted">Monto bruto</span>
            <span className="font-medium text-finflow-dark">{formatCurrency(safeAmount)}</span>
          </div>
          {safeFee > 0 && (
            <div className="flex justify-between">
              <span className="text-finflow-muted">Comisión</span>
              <span className="font-medium text-finflow-rust">–{formatCurrency(safeFee)}</span>
            </div>
          )}
          <div className="flex justify-between border-t border-[#EFEAE2] pt-2">
            <span className="text-finflow-muted">Efectivo neto recibido</span>
            <span className="font-bold text-finflow-dark">{formatCurrency(netCash)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-finflow-muted">Ganancia/Pérdida realizada</span>
            <span className={`font-bold ${realizedGain >= 0 ? 'text-finflow-green' : 'text-finflow-rust'}`}>
              {realizedGain >= 0 ? '+' : ''}{formatCurrency(realizedGain)}
            </span>
          </div>
          <div className="border-t border-[#EFEAE2] pt-2 mt-1 space-y-1 text-xs text-finflow-muted">
            <div className="flex justify-between">
              <span>Capital base restante</span>
              <span className={remainingCapital < 0 ? 'text-finflow-rust font-medium' : ''}>{formatCurrency(Math.max(0, remainingCapital))}</span>
            </div>
            <div className="flex justify-between">
              <span>Valor de posición restante</span>
              <span className={remainingValue < 0 ? 'text-finflow-rust font-medium' : ''}>{formatCurrency(Math.max(0, remainingValue))}</span>
            </div>
          </div>
        </div>
      )}

      <div className="flex justify-end gap-3 pt-4 border-t border-[#EFEAE2]">
        <Button type="button" variant="ghost" onClick={onCancel} className="text-finflow-muted hover:bg-[#EFEAE2]">
          Cancelar
        </Button>
        <Button type="submit" isLoading={isPending} className="bg-finflow-dark text-finflow-cream hover:bg-finflow-dark/90">
          Registrar Retiro
        </Button>
      </div>
    </form>
  );
}
