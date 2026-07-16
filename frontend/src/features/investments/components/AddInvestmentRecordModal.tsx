import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { format } from 'date-fns';
import { Modal, ModalHeader, ModalBody, ModalFooter } from '../../../components/ui/Modal';
import { 
  investmentRecordSchema, 
  type InvestmentRecordFormValues 
} from '../schemas/investment-record.schema';
import { useAddInvestmentRecord } from '../hooks/useAddInvestmentRecord';

interface AddInvestmentRecordModalProps {
  isOpen: boolean;
  onClose: () => void;
  investmentId: string | null;
  investmentName?: string;
}

export function AddInvestmentRecordModal({ 
  isOpen, 
  onClose, 
  investmentId,
  investmentName 
}: AddInvestmentRecordModalProps) {
  
  const { mutate: addRecord, isPending } = useAddInvestmentRecord(() => {
    reset();
    onClose();
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors }
  } = useForm<InvestmentRecordFormValues>({
    resolver: zodResolver(investmentRecordSchema),
    defaultValues: {
      recordDate: format(new Date(), 'yyyy-MM-dd'),
      value: undefined,
      dividends: 0,
      notes: ''
    }
  });

  const onSubmit = (data: InvestmentRecordFormValues) => {
    if (!investmentId) return;
    addRecord({ investmentId, data });
  };

  const handleClose = () => {
    reset();
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose}>
      <ModalHeader>
        <h2 className="text-xl font-serif font-medium text-[#2C2A29]">
          Añadir Registro
        </h2>
        {investmentName && (
          <p className="text-sm text-[#7C756E]">
            Actualizando valor para <span className="font-semibold">{investmentName}</span>
          </p>
        )}
      </ModalHeader>

      <ModalBody>
        <form id="investment-record-form" onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          
          <div>
            <label className="block text-sm font-medium text-[#2C2A29] mb-1">
              Fecha del Registro
            </label>
            <input
              type="date"
              {...register('recordDate')}
              className="w-full rounded-xl border border-[#EFEAE2] bg-white px-4 py-2 text-[#2C2A29] focus:border-[#8FA888] focus:outline-none focus:ring-1 focus:ring-[#8FA888] transition-shadow"
            />
            {errors.recordDate && (
              <p className="mt-1 text-sm text-red-500">{errors.recordDate.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-[#2C2A29] mb-1">
                Valor Actual ($)
              </label>
              <input
                type="number"
                step="0.01"
                {...register('value', { valueAsNumber: true })}
                className="w-full rounded-xl border border-[#EFEAE2] bg-white px-4 py-2 text-[#2C2A29] focus:border-[#8FA888] focus:outline-none focus:ring-1 focus:ring-[#8FA888] transition-shadow"
                placeholder="0.00"
              />
              {errors.value && (
                <p className="mt-1 text-sm text-red-500">{errors.value.message}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-[#2C2A29] mb-1">
                Dividendos ($)
              </label>
              <input
                type="number"
                step="0.01"
                {...register('dividends', { valueAsNumber: true })}
                className="w-full rounded-xl border border-[#EFEAE2] bg-white px-4 py-2 text-[#2C2A29] focus:border-[#8FA888] focus:outline-none focus:ring-1 focus:ring-[#8FA888] transition-shadow"
                placeholder="0.00"
              />
              {errors.dividends && (
                <p className="mt-1 text-sm text-red-500">{errors.dividends.message}</p>
              )}
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-[#2C2A29] mb-1">
              Notas (Opcional)
            </label>
            <textarea
              {...register('notes')}
              rows={3}
              className="w-full rounded-xl border border-[#EFEAE2] bg-white px-4 py-2 text-[#2C2A29] focus:border-[#8FA888] focus:outline-none focus:ring-1 focus:ring-[#8FA888] transition-shadow resize-none"
              placeholder="Ej. Aporte para estrategia a 10-20 años, reinversión en ETFs..."
            />
            {errors.notes && (
              <p className="mt-1 text-sm text-red-500">{errors.notes.message}</p>
            )}
          </div>
        </form>
      </ModalBody>

      <ModalFooter>
        <button
          type="button"
          onClick={handleClose}
          disabled={isPending}
          className="rounded-xl px-4 py-2 text-sm font-medium text-[#7C756E] hover:bg-[#EFEAE2] transition-colors disabled:opacity-50"
        >
          Cancelar
        </button>
        <button
          type="submit"
          form="investment-record-form"
          disabled={isPending}
          className="rounded-xl bg-[#2C2A29] px-4 py-2 text-sm font-medium text-[#FBF9F4] hover:bg-[#2C2A29]/90 transition-colors shadow-md disabled:opacity-50 flex items-center justify-center min-w-[120px]"
        >
          {isPending ? 'Guardando...' : 'Guardar Registro'}
        </button>
      </ModalFooter>
    </Modal>
  );
}