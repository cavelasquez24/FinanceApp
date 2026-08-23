import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  type TooltipValueType,
} from 'recharts';
import { useMemo } from 'react';
import type { NetWorthTimelineDto } from '../types/analytics.types';
import { formatCurrency } from '../../../utils/formatCurrency';

interface Props {
  data: NetWorthTimelineDto;
}

export function NetWorthChart({ data }: Props) {
  const chartData = useMemo(
    () =>
      data.labels.map((label, i) => ({
        name: label,
        Activos: data.totalAssets[i] ?? 0,
        Pasivos: data.totalLiabilities[i] ?? 0,
        Patrimonio: data.netWorth[i] ?? 0,
      })),
    [data]
  );

  const isPositive = data.netWorthChange >= 0;

  return (
    <div className="space-y-4">
      {/* Delta badges */}
      <div className="flex flex-wrap gap-3">
        <div className="flex items-center gap-1.5 rounded-xl border border-[#EFEAE2] bg-finflow-cream px-3 py-1.5 text-xs">
          <span className="text-finflow-muted">Cambio total</span>
          <span className={`font-semibold ${isPositive ? 'text-finflow-green' : 'text-finflow-rust'}`}>
            {isPositive ? '+' : ''}{formatCurrency(data.netWorthChange)}
          </span>
        </div>
        <div className="flex items-center gap-1.5 rounded-xl border border-[#EFEAE2] bg-finflow-cream px-3 py-1.5 text-xs">
          <span className="text-finflow-muted">%</span>
          <span className={`font-semibold ${isPositive ? 'text-finflow-green' : 'text-finflow-rust'}`}>
            {isPositive ? '+' : ''}{data.netWorthChangePct.toFixed(1)}%
          </span>
        </div>
      </div>

      {/* Chart */}
      <div className="h-[240px] w-full">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={chartData} margin={{ top: 5, right: 10, bottom: 5, left: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#EFEAE2" />
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
              domain={['auto', 'auto']}
              width={48}
            />
            <Tooltip
              contentStyle={{
                backgroundColor: 'rgba(255,255,255,0.95)',
                border: '1px solid #EFEAE2',
                borderRadius: '16px',
                boxShadow: '0 8px 30px rgba(44,42,41,0.08)',
                fontSize: 12,
              }}
              formatter={(value: TooltipValueType | undefined, name: string | number | undefined) => [
                formatCurrency(Number(value ?? 0)),
                name ?? '',
              ]}
            />
            <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 12 }} />
            <Line
              type="monotone"
              dataKey="Activos"
              stroke="#8FA888"
              strokeWidth={2}
              dot={false}
              activeDot={{ r: 4 }}
            />
            <Line
              type="monotone"
              dataKey="Pasivos"
              stroke="#C97B63"
              strokeWidth={2}
              dot={false}
              activeDot={{ r: 4 }}
            />
            <Line
              type="monotone"
              dataKey="Patrimonio"
              stroke="#2C2A29"
              strokeWidth={2.5}
              dot={false}
              activeDot={{ r: 5 }}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
