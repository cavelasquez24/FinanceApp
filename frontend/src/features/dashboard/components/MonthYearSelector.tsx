// src/features/dashboard/components/MonthYearSelector.tsx
import { ChevronLeft, ChevronRight } from 'lucide-react';

const MESES = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre',
];

interface Props {
  month: number;
  year: number;
  onChange: (month: number, year: number) => void;
}

export function MonthYearSelector({ month, year, onChange }: Props) {
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

  return (
    <div className="flex items-center gap-1 rounded-2xl border border-[#EFEAE2] bg-white/70 p-1 backdrop-blur-sm">
      <button
        type="button"
        onClick={goToPrevious}
        className="rounded-xl p-2 text-[#7C756E] transition-colors hover:bg-[#F3F1EC] hover:text-[#2C2A29]"
        aria-label="Mes anterior"
      >
        <ChevronLeft className="h-4 w-4" strokeWidth={2} />
      </button>

      <span className="min-w-[140px] text-center text-sm font-medium text-[#2C2A29]">
        {MESES[month - 1]} {year}
      </span>

      <button
        type="button"
        onClick={goToNext}
        disabled={isCurrentMonth}
        className="rounded-xl p-2 text-[#7C756E] transition-colors hover:bg-[#F3F1EC] hover:text-[#2C2A29] disabled:cursor-not-allowed disabled:opacity-30 disabled:hover:bg-transparent"
        aria-label="Mes siguiente"
      >
        <ChevronRight className="h-4 w-4" strokeWidth={2} />
      </button>
    </div>
  );
}