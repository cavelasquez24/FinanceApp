// src/features/dashboard/components/MonthYearSelector.tsx
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { getCycleRange, formatCycleLabel } from '../../../utils/cycleUtils';

const MESES = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre',
];

interface Props {
  month: number;
  year: number;
  onChange: (month: number, year: number) => void;
  /** Cuando existe, el label muestra el rango real del ciclo (ej. "25 Jun – 25 Jul 2026")
   *  en vez del mes calendario plano. Mismo cálculo que el badge "Tu ciclo" del header —
   *  no se duplica lógica, se reusa cycleUtils.ts. */
  paydayDay?: number | null;
}

export function MonthYearSelector({ month, year, onChange, paydayDay }: Props) {
  const goToPrevious = () => {
    if (month === 1) onChange(12, year - 1);
    else onChange(month - 1, year);
  };

  const goToNext = () => {
    if (month === 12) onChange(1, year + 1);
    else onChange(month + 1, year);
  };

  const today = new Date();
  const isCurrentMonth = month === today.getMonth() + 1 && year === today.getFullYear();

  const cycleRange = paydayDay != null ? getCycleRange(month, year, paydayDay) : null;
  const label = cycleRange ? formatCycleLabel(cycleRange) : `${MESES[month - 1]} ${year}`;

  return (
    <div className="flex items-center gap-1 rounded-2xl border border-[#EFEAE2] bg-white/70 p-1 backdrop-blur-sm">
      <button
        type="button"
        onClick={goToPrevious}
        className="rounded-xl p-2 text-[#7C756E] transition-colors hover:bg-[#F3F1EC] hover:text-[#2C2A29]"
        aria-label="Ciclo anterior"
      >
        <ChevronLeft className="h-4 w-4" strokeWidth={2} />
      </button>

      <span className="min-w-[170px] text-center text-sm font-medium text-[#2C2A29]">
        {label}
      </span>

      <button
        type="button"
        onClick={goToNext}
        disabled={isCurrentMonth}
        className="rounded-xl p-2 text-[#7C756E] transition-colors hover:bg-[#F3F1EC] hover:text-[#2C2A29] disabled:cursor-not-allowed disabled:opacity-30 disabled:hover:bg-transparent"
        aria-label="Ciclo siguiente"
      >
        <ChevronRight className="h-4 w-4" strokeWidth={2} />
      </button>
    </div>
  );
}