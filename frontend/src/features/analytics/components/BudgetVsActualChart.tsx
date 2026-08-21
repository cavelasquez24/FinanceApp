import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ReferenceLine,
  ResponsiveContainer,
} from 'recharts';
import { useMemo } from 'react';
import type { BudgetVsActualHistoryDto } from '../types/analytics.types';
import { formatCurrency } from '../../../utils/formatCurrency';

interface Props {
  data: BudgetVsActualHistoryDto;
}

export function BudgetVsActualChart({ data }: Props) {
  const chartData = useMemo(
    () =>
      data.periods.map((p) => ({
        name: p.label,
        Presupuesto: p.budgeted,
        Real: p.actual,
        adherencePct: p.adherencePct,
      })),
    [data]
  );

  if (data.periods.length === 0) {
    return (
      <p className="py-6 text-center text-xs text-[#7C756E]">Sin períodos presupuestados.</p>
    );
  }

  return (
    <div className="space-y-4">
      {/* Adherence badges */}
      <div className="flex flex-wrap gap-2">
        {data.periods.map((p) => {
          const ok = p.adherencePct <= 100;
          return (
            <div
              key={p.label}
              className={`rounded-xl border px-2.5 py-1 text-[10px] font-semibold ${
                ok
                  ? 'border-[#8FA888]/30 bg-[#8FA888]/10 text-[#5A7853]'
                  : 'border-[#C97B63]/30 bg-[#C97B63]/10 text-[#8B4A36]'
              }`}
            >
              {p.label}: {p.adherencePct.toFixed(0)}%
            </div>
          );
        })}
      </div>

      {/* Grouped bar chart */}
      <div className="h-[220px] w-full">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={chartData} margin={{ top: 5, right: 10, bottom: 5, left: 0 }} barGap={4}>
            <CartesianGrid strokeDasharray="3 3" stroke="#EFEAE2" vertical={false} />
            <XAxis
              dataKey="name"
              stroke="#7C756E"
              fontSize={11}
              tickLine={false}
              axisLine={{ stroke: '#EFEAE2' }}
            />
            <YAxis
              stroke="#7C756E"
              fontSize={11}
              tickLine={false}
              axisLine={{ stroke: '#EFEAE2' }}
              tickFormatter={(v) => `$${(v / 1000).toFixed(0)}k`}
              width={44}
            />
            <Tooltip
              contentStyle={{
                backgroundColor: 'rgba(255,255,255,0.95)',
                border: '1px solid #EFEAE2',
                borderRadius: '16px',
                boxShadow: '0 8px 30px rgba(44,42,41,0.08)',
                fontSize: 12,
              }}
              formatter={(value: number, name: string) => [formatCurrency(value), name]}
            />
            <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 12 }} />
            <Bar dataKey="Presupuesto" fill="#5C7A99" opacity={0.5} radius={[4, 4, 0, 0]} />
            <Bar dataKey="Real" fill="#2C2A29" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
