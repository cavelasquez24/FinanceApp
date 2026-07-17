// src/components/dashboard/FinancialChart.tsx
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts';
import type { MonthlyTrend } from '../../types/dashboard.types';
import { useMemo } from 'react';

interface Props {
  data: MonthlyTrend;
}

export function FinancialChart({ data }: Props) {
  const chartData = useMemo(() => {
    if (!data || !data.labels) return [];
    return data.labels.map((label, index) => ({
      name: label,
      Ingresos: data.income[index] || 0,
      Gastos: data.expenses[index] || 0,
      Ahorros: data.savings[index] || 0,
    }));
  }, [data]);

  return (
    <div className="h-[300px] w-full">
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={chartData} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#EFEAE2" />
          
          <XAxis 
            dataKey="name" 
            stroke="#7C756E" 
            fontSize={12} 
            tickLine={false} 
            axisLine={{ stroke: '#EFEAE2' }} 
          />
          
          <YAxis
            stroke="#7C756E"
            fontSize={12}
            tickLine={false}
            axisLine={{ stroke: '#EFEAE2' }}
            tickFormatter={(value) => `$${value}`}
            domain={[0, 'auto']} // Bloquea el renderizado de valores negativos
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
          
          <Legend wrapperStyle={{ paddingTop: '12px', color: '#7C756E', fontSize: '12px' }} />
          
          {/* Interpolación monotoneX reemplaza a natural para prevenir el overshoot */}
          <Line 
            type="monotoneX" 
            dataKey="Ingresos" 
            stroke="#5C7A99" 
            strokeWidth={2.5} 
            dot={false} 
            activeDot={{ r: 4, fill: '#5C7A99' }} 
          />
          <Line 
            type="monotoneX" 
            dataKey="Gastos" 
            stroke="#C97B63" 
            strokeWidth={2.5} 
            dot={false} 
            activeDot={{ r: 4, fill: '#C97B63' }} 
          />
          <Line 
            type="monotoneX" 
            dataKey="Ahorros" 
            stroke="#8FA888" 
            strokeWidth={2.5} 
            dot={false} 
            activeDot={{ r: 4, fill: '#8FA888' }} 
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}