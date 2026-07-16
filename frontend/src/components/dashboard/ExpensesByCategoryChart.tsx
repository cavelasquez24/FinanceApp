// src/components/dashboard/ExpensesByCategoryChart.tsx
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip } from 'recharts';
import type { ExpensesByCategoryResponse } from '../../types/dashboard.types';

interface Props {
  data: ExpensesByCategoryResponse;
}

const currency = (value: number) =>
  new Intl.NumberFormat('es-US', { style: 'currency', currency: 'USD' }).format(value);

export function ExpensesByCategoryChart({ data }: Props) {
  if (!data.categories.length) {
    return (
      <div className="flex h-[280px] flex-col items-center justify-center gap-2 text-center text-[#7C756E]">
        <span className="text-sm">Sin gastos registrados este mes.</span>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="relative h-[220px] w-full">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={data.categories}
              dataKey="amount"
              nameKey="categoryName"
              innerRadius={65}
              outerRadius={95}
              paddingAngle={3}
              cornerRadius={6}
              stroke="none"
            >
              {data.categories.map((entry) => (
                <Cell key={entry.categoryName} fill={entry.categoryColor} />
              ))}
            </Pie>
                <Tooltip
                formatter={(value, name) => [
                    currency(Number(value ?? 0)),
                    String(name),
                ]}
                contentStyle={{
                    backgroundColor: 'rgba(255,255,255,0.95)',
                    border: '1px solid #EFEAE2',
                    borderRadius: '16px',
                    boxShadow: '0 8px 30px rgba(44,42,41,0.08)',
                }}
                labelStyle={{ color: '#2C2A29', fontWeight: 600 }}
                />
          </PieChart>
        </ResponsiveContainer>

        {/* Total centrado */}
        <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
          <span className="text-xs text-[#7C756E]">Total gastado</span>
          <span className="font-serif text-lg font-medium text-[#2C2A29]">
            {currency(data.totalAmount)}
          </span>
        </div>
      </div>

      {/* Leyenda */}
      <div className="space-y-2">
        {data.categories.map((cat) => (
          <div key={cat.categoryName} className="flex items-center justify-between text-sm">
            <div className="flex items-center gap-2">
              <div
                className="h-2.5 w-2.5 rounded-full"
                style={{ backgroundColor: cat.categoryColor }}
              />
              <span className="text-[#2C2A29]">{cat.categoryName}</span>
            </div>
            <div className="flex items-center gap-2 text-[#7C756E]">
              <span>{currency(cat.amount)}</span>
              <span className="text-xs">({cat.percentage.toFixed(0)}%)</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}