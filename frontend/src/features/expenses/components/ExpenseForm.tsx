import { todayDateOnly } from '../../../utils/dateOnly';
// src/features/expenses/components/ExpenseForm.tsx
import { useForm } from 'react-hook-form';
import { useEffect, useRef } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { expenseSchema, type ExpenseFormData } from '../schemas/expense.schema';
import { Button, Input } from '../../../components/ui';
import { useCreateExpense, useUpdateExpense } from '../hooks/useExpenses';
import type { Expense } from '../../../types/expense.types';
import { useCategories } from '../../categories/hooks/useCategories';
import { Spinner } from '../../../components/ui';
import { TagSelector } from '../../tags/components/TagSelector';
import { useAccounts } from '../../accounts/hooks/useAccounts';
import { useCreditCards } from '../../credit-cards/hooks/useCreditCards';

interface Props {
  expense?: Expense; // si viene, el form entra en modo edición
  onSuccess?: () => void;
  onCancel?: () => void;
}

const selectClassName =
  'w-full rounded-xl border border-[#EFEAE2] bg-white/70 px-3 py-2.5 text-sm text-finflow-dark backdrop-blur-sm transition-colors focus:border-finflow-blue focus:outline-none focus:ring-2 focus:ring-finflow-blue/20';

// Valores de un formulario en blanco. Se recalculan en cada llamada para que
// `date` sea el día actual y no el de la primera vez que se montó el form.
const emptyExpenseDefaults = () => ({
  date: todayDateOnly(),
  accountId: '',
  isRecurring: false,
  creditCardId: '',
  tagIds: [] as string[],
  paymentMethod: 'debit_card',
});

export function ExpenseForm({ expense, onSuccess, onCancel }: Props) {
  const isEditMode = Boolean(expense);
  const purchaseIdempotencyKey = useRef(crypto.randomUUID());

  const { mutate: createExpense, isPending: isCreating } = useCreateExpense();
  const { mutate: updateExpense, isPending: isUpdating } = useUpdateExpense();
  const isPending = isEditMode ? isUpdating : isCreating;
  const { data: categories, isLoading: isLoadingCategories } = useCategories('expense');
  const { data: accounts, isLoading: isLoadingAccounts } = useAccounts();
  const { data: creditCards, isLoading: isLoadingCreditCards } = useCreditCards();
  const activeCreditCards = creditCards?.filter((card) => card.isActive) ?? [];
  const cashAccounts = accounts?.filter((account) => account.type === 'cash' && account.isActive) ?? [];
  const defaultCashAccount = cashAccounts.find((account) => account.isDefault);



  const {
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(expenseSchema),
    defaultValues: expense
      ? {
          categoryId: expense.categoryId,
          accountId: expense.accountId ?? '',
          amount: expense.amount,
          creditCardId: expense.creditCardId ?? '',
          merchant: expense.merchant ?? '',
          tagIds: expense.tags.map((tag) => tag.id),
          description: expense.description ?? '',
          date: expense.date,
          paymentMethod: expense.paymentMethod,
          isRecurring: expense.isRecurring,
          recurrenceType: expense.recurrenceType,
          notes: expense.notes ?? '',
        }
      : {
          date: todayDateOnly(),
          accountId: '',
          isRecurring: false,
          creditCardId: '',
          tagIds: [],
          paymentMethod: 'debit_card',
        },
  });

  const isRecurring = watch('isRecurring');
  const selectedAccountId = watch('accountId');

  const selectedCreditCardId = watch('creditCardId');
  const paymentMethod = watch('paymentMethod');
  useEffect(() => {
    if (!isEditMode && paymentMethod !== 'credit_card' && !selectedAccountId && defaultCashAccount) {
      setValue('accountId', defaultCashAccount.id, { shouldValidate: true });
    }
  }, [defaultCashAccount, isEditMode, paymentMethod, selectedAccountId, setValue]);

  useEffect(() => {
    if (paymentMethod === 'credit_card' && selectedAccountId) {
      setValue('accountId', '', { shouldValidate: true });
    }
    if (paymentMethod !== 'credit_card' && selectedCreditCardId) {
      setValue('creditCardId', '', { shouldValidate: true });
    }
  }, [paymentMethod, selectedAccountId, selectedCreditCardId, setValue]);

  const tagIds = watch('tagIds') ?? [];

  const onSubmit = (data: ExpenseFormData) => {
    // Limpieza de datos antes de enviar al backend según contrato
    const payload = {
      ...data,
      accountId: data.paymentMethod === 'credit_card' ? null : data.accountId,
      creditCardId: data.paymentMethod === 'credit_card' ? data.creditCardId : null,
      recurrenceType: data.isRecurring ? data.recurrenceType : null,
    };

    if (isEditMode && expense) {
      updateExpense(
        { id: expense.id, dto: payload },
        { onSuccess: () => onSuccess?.() }
      );
    } else {
      createExpense({
        ...payload,
        idempotencyKey: data.paymentMethod === 'credit_card' ? purchaseIdempotencyKey.current : null,
      }, {
        onSuccess: () => {
          // El form de creación no siempre se desmonta al cerrarse (en móvil
          // vive dentro de un BottomSheet que solo se oculta con CSS), así que
          // se limpia explícitamente en vez de confiar en el remontaje.
          reset(emptyExpenseDefaults());
          // Clave nueva por gasto: reusarla haría que el backend tratara la
          // siguiente compra con tarjeta como duplicado de la anterior.
          purchaseIdempotencyKey.current = crypto.randomUUID();
          onSuccess?.();
        },
      });
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1.5">
        <label className="text-sm font-medium text-finflow-dark">Categoría</label>
        {isLoadingCategories ? (
          <div className="flex h-[42px] items-center rounded-xl border border-[#EFEAE2] bg-white/70 px-3">
            <Spinner size="sm" />
          </div>
        ) : (
          <select {...register('categoryId')} className={selectClassName}>
            <option value="">Selecciona una...</option>
            {categories?.map((cat) => (
              <option key={cat.id} value={cat.id}>{cat.name}</option>
            ))}
          </select>
        )}
        {errors.categoryId && <span className="text-xs text-finflow-rust">{errors.categoryId.message}</span>}
      </div>

        <Input
          label="Monto"
          type="number"
          step="0.01"
          placeholder="0.00"
          error={errors.amount?.message}
          {...register('amount', { valueAsNumber: true })}
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <Input label="Fecha" type="date" error={errors.date?.message} {...register('date')} />

        <div className="flex flex-col gap-1.5">
          <label className="text-sm font-medium text-finflow-dark">Método de Pago</label>
          <select {...register('paymentMethod')} className={selectClassName}>
            <option value="cash">Efectivo</option>
            <option value="debit_card">Tarjeta de Débito</option>
            <option value="credit_card">Tarjeta de Crédito</option>
            <option value="transfer">Transferencia</option>
            <option value="other">Otro</option>
          </select>
          {errors.paymentMethod && (
            <span className="text-xs text-finflow-rust">{errors.paymentMethod.message}</span>
          )}
        </div>
      </div>
      {paymentMethod === 'credit_card' ? (
        <div className="flex flex-col gap-1.5">
          <label className="text-sm font-medium text-finflow-dark">Tarjeta utilizada</label>
          {isLoadingCreditCards ? (
            <div className="flex h-[42px] items-center rounded-xl border border-[#EFEAE2] bg-white/70 px-3"><Spinner size="sm" /></div>
          ) : (
            <select {...register('creditCardId')} className={selectClassName}>
              <option value="">Selecciona una tarjeta activa...</option>
              {activeCreditCards.map((card) => <option key={card.id} value={card.id}>{card.name} · saldo {card.currentBalance.toFixed(2)}</option>)}
            </select>
          )}
          {errors.creditCardId && <span className="text-xs text-finflow-rust">{errors.creditCardId.message}</span>}
          <p className="text-xs text-finflow-muted">La compra aumenta gasto y pasivo; no descuenta efectivo.</p>
        </div>
      ) : (
        <div className="flex flex-col gap-1.5">
          <label className="text-sm font-medium text-finflow-dark">Pagado desde</label>
          {isLoadingAccounts ? (
            <div className="flex h-[42px] items-center rounded-xl border border-[#EFEAE2] bg-white/70 px-3"><Spinner size="sm" /></div>
          ) : (
            <>
              <select {...register('accountId')} className={selectClassName}>
                <option value="">Selecciona una cuenta líquida...</option>
                {cashAccounts.map((account) => <option key={account.id} value={account.id}>{account.name}{account.isDefault ? ' (predeterminada)' : ''}</option>)}
              </select>
              <p className="text-xs text-finflow-muted">La cuenta predeterminada se muestra seleccionada; puedes cambiarla antes de guardar.</p>
            </>
          )}
          {errors.accountId && <span className="text-xs text-finflow-rust">{errors.accountId.message}</span>}
        </div>
      )}

      <Input
        label="Comercio (Opcional)"
        placeholder="Ej: Wing House"
        error={errors.merchant?.message}
        {...register('merchant')}
      />


      <Input
        label="Descripción Breve"
        placeholder="Ej: Compra supermercado"
        error={errors.description?.message}
        {...register('description')}
      />

      <Input
        label="Notas (Opcional)"
        placeholder="Detalles adicionales..."
        error={errors.notes?.message}
        {...register('notes')}
      />

      <TagSelector value={tagIds} onChange={(ids) => setValue('tagIds', ids, { shouldValidate: true })} />

      <div className="rounded-2xl border border-[#EFEAE2] bg-[#F3F1EC]/60 p-4">
        <div className="flex items-center gap-3">
          <input
            type="checkbox"
            id="isRecurring"
            {...register('isRecurring')}
            className="h-4 w-4 rounded border-[#EFEAE2] accent-finflow-blue focus:ring-2 focus:ring-finflow-blue/30"
            onChange={(e) => {
              setValue('isRecurring', e.target.checked);
              if (!e.target.checked) setValue('recurrenceType', null);
            }}
          />
          <label htmlFor="isRecurring" className="text-sm font-medium text-finflow-dark">
            Este es un gasto recurrente
          </label>
        </div>

        {isRecurring && (
          <div className="mt-4 flex flex-col gap-1.5">
            <label className="text-sm font-medium text-finflow-dark">Frecuencia de repetición</label>
            <select {...register('recurrenceType')} className={selectClassName}>
              <option value="">Selecciona la frecuencia...</option>
              <option value="daily">Diario</option>
              <option value="weekly">Semanal</option>
              <option value="biweekly">Quincenal</option>
              <option value="monthly">Mensual</option>
              <option value="yearly">Anual</option>
            </select>
            {errors.recurrenceType && (
              <span className="text-xs text-finflow-rust">{errors.recurrenceType.message}</span>
            )}
          </div>
        )}
      </div>

      <div className="mt-6 flex justify-end gap-3">
        {onCancel && (
          <Button
            type="button"
            className="!border !border-[#EFEAE2] !bg-white !text-finflow-dark hover:!bg-[#F3F1EC]"
            onClick={onCancel}
          >
            Cancelar
          </Button>
        )}
        <Button type="submit" isLoading={isPending} className="!bg-finflow-dark !text-finflow-cream hover:!bg-[#1F1E1D]">
          {isEditMode ? 'Guardar Cambios' : 'Guardar Gasto'}
        </Button>
      </div>
    </form>
  );
}
