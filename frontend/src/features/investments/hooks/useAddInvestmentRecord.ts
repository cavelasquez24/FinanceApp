import { useMutation, useQueryClient } from '@tanstack/react-query';
import { investmentsApi } from '../../../api/investments.api';
import toast from 'react-hot-toast';
import type { InvestmentRecordFormValues }from '../schemas/investment-record.schema';

interface AddRecordVariables {
  investmentId: string;
  data: InvestmentRecordFormValues;
}

export function useAddInvestmentRecord(onSuccessCallback?: () => void) {
  const queryClient = useQueryClient();

 return useMutation({
  mutationFn: ({ investmentId, data }: AddRecordVariables) =>
    investmentsApi.addRecord(investmentId, data),

  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['investments'] });
    queryClient.invalidateQueries({ queryKey: ['dashboard'] });

    toast.success('Registro añadido al portafolio exitosamente');

    if (onSuccessCallback) {
      onSuccessCallback();
    }
  },

  onError: (error: any) => {
    const errorMessage =
      error?.response?.data?.error?.message ||
      'Hubo un error al guardar el registro';

    toast.error(errorMessage);
  }
});
}