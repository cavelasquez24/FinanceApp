import type { YearOverYearDto } from '../types/analytics.types';
import { formatCurrency } from '../../../utils/formatCurrency';
import { cn } from '../../../utils/cn';

interface Props {
  data: YearOverYearDto;
}

function Delta({ value, pct }: { value: number; pct: number }) {
  const positive = value >= 0;
  return (
    <span className={cn('text-xs font-medium', positive ? 'text-[#8FA888]' : 'text-[#C97B63]')}>
      {positive ? '+' : ''}{pct.toFixed(1)}%
    </span>
  );
}

export function YearOverYearTable({ data }: Props) {
  const { year, previousYear, months, totals } = data;

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[540px] text-xs">
        <thead>
          <tr className="border-b border-[#EFEAE2]">
            <th className="pb-2 text-left font-medium text-[#7C756E]">Mes</th>
            <th className="pb-2 text-right font-medium text-[#7C756E]">{previousYear}</th>
            <th className="pb-2 text-right font-medium text-[#7C756E]">{year}</th>
            <th className="pb-2 text-right font-medium text-[#7C756E]">Δ</th>
          </tr>
        </thead>
        <tbody>
          {months.map((m) => {
            const incomeDiff = m.currentIncome - m.prevIncome;
            const incomePct = m.prevIncome !== 0 ? (incomeDiff / m.prevIncome) * 100 : 0;
            return (
              <tr key={m.monthLabel} className="border-b border-[#EFEAE2]/50 hover:bg-[#FBF9F4]">
                <td className="py-2 font-medium text-[#2C2A29]">{m.monthLabel}</td>
                <td className="py-2 text-right text-[#7C756E]">
                  <div>{formatCurrency(m.prevIncome)}</div>
                  <div className="text-[#C97B63]">{formatCurrency(m.prevExpenses)}</div>
                </td>
                <td className="py-2 text-right">
                  <div className="text-[#2C2A29]">{formatCurrency(m.currentIncome)}</div>
                  <div className="text-[#C97B63]">{formatCurrency(m.currentExpenses)}</div>
                </td>
                <td className="py-2 text-right">
                  <Delta value={incomeDiff} pct={incomePct} />
                </td>
              </tr>
            );
          })}
        </tbody>
        <tfoot>
          <tr className="border-t-2 border-[#EFEAE2] bg-[#FBF9F4]">
            <td className="py-2 font-semibold text-[#2C2A29]">Total ingr.</td>
            <td className="py-2 text-right" colSpan={2} />
            <td className="py-2 text-right">
              <Delta value={totals.incomeChangeAbs} pct={totals.incomeChangePct} />
            </td>
          </tr>
          <tr className="bg-[#FBF9F4]">
            <td className="py-2 font-semibold text-[#2C2A29]">Total gast.</td>
            <td className="py-2 text-right" colSpan={2} />
            <td className="py-2 text-right">
              <Delta value={-totals.expensesChangeAbs} pct={-totals.expensesChangePct} />
            </td>
          </tr>
          <tr className="bg-[#FBF9F4]">
            <td className="py-2 font-semibold text-[#2C2A29]">Ahorro neto</td>
            <td className="py-2 text-right" colSpan={2} />
            <td className="py-2 text-right">
              <Delta value={totals.netSavingsChangeAbs} pct={totals.netSavingsChangePct} />
            </td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
}
