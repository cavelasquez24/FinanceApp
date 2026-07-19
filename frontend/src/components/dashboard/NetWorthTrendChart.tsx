// src/components/dashboard/NetWorthTrendChart.tsx
//
// v2.0.1 sección 6.3 — PENDIENTE DE CONEXIÓN.
// Este componente está listo pero NO se usa en DashboardPage todavía:
// depende de MonthlySnapshot.NetWorthAtClose histórico (sección 3.5),
// que aún no existe en el backend (falta entidad + job de cierre +
// endpoint). Conectarlo ahora obligaría a mockear datos, lo cual va
// contra la regla de no usar datos falsos. Cuando el endpoint de
// snapshots exista (ej. GET /dashboard/net-worth-trend), solo hay que
// pasarle la respuesta como prop `data`.
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { useMemo } from 'react';

export interface NetWorthTrendPoint {
  label: string; // ej. "Ene 26"
  netWorth: number;
}

interface Props {
  data: NetWorthTrendPoint[];
}

export function NetWorthTrendChart({ data }: Props) {
  const chartData = useMemo(
    () => data.map((d) => ({ name: d.label, 'Patrimonio Neto': d.netWorth })),
    [data]
  );

  return (
    <div className="h-[260px] w-full">
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={chartData} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
          <defs>
            <linearGradient id="netWorthGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor="#2C2A29" stopOpacity={0.25} />
              <stop offset="95%" stopColor="#2C2A29" stopOpacity={0} />
            </linearGradient>
          </defs>

          <CartesianGrid strokeDasharray="3 3" stroke="#EFEAE2" />

          <XAxis
            dataKey="name"
            stroke="#7C756E"
            fontSize={12}
            tickLine={false}
            axisLine={{ stroke: '#EFEAE2' }}
          />

          {/* Dominio libre: patrimonio negativo (ej. deuda de largo plazo
              mayor a activos) debe verse, no ocultarse — mismo criterio
              que FinancialChart (6.2) */}
          <YAxis
            stroke="#7C756E"
            fontSize={12}
            tickLine={false}
            axisLine={{ stroke: '#EFEAE2' }}
            tickFormatter={(value) => `$${value}`}
            domain={['auto', 'auto']}
          />

          <Tooltip
            contentStyle={{
              backgroundColor: 'rgba(255,255,255,0.9)',
              border: '1px solid #EFEAE2',
              borderRadius: '16px',
              boxShadow: '0 8px 30px rgba(44,42,41,0.08)',
            }}
            labelStyle={{ color: '#2C2A29', fontWeight: 600 }}
            itemStyle={{ color: '#7C756E' }}
          />

          <Area
            type="monotoneX"
            dataKey="Patrimonio Neto"
            stroke="#2C2A29"
            strokeWidth={2.5}
            fill="url(#netWorthGradient)"
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}