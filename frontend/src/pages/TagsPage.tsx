import { useState } from 'react';
import { Card, CardHeader } from '../components/ui';
import { MonthYearSelector } from '../features/dashboard/components/MonthYearSelector';
import { TagManager } from '../features/tags/components/TagManager';
import { TagExpenseReport } from '../features/tags/components/TagExpenseReport';

export default function TagsPage() {
  const today = new Date();
  const [month, setMonth] = useState(today.getMonth() + 1);
  const [year, setYear] = useState(today.getFullYear());
  const reportStart = `${year}-${String(month).padStart(2, '0')}-01`;
  const reportEnd = new Date(year, month, 0).toISOString().split('T')[0];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="font-serif text-3xl font-semibold tracking-tight text-[#2C2A29]">Etiquetas</h1>
        <p className="mt-1 max-w-3xl text-sm text-[#7C756E]">
          Usa categorías para el tipo general del gasto y etiquetas para su contexto: personas, ocasión, lugar o propósito.
        </p>
      </div>

      <div className="grid gap-6 xl:grid-cols-2">
        <Card>
          <CardHeader title="Administrar etiquetas" subtitle="Crea, renombra, elimina o fusiona etiquetas duplicadas" />
          <TagManager />
        </Card>
        <Card>
          <CardHeader title="Análisis por etiquetas" subtitle="Cobertura y gasto asociado durante el mes seleccionado" />
          <div className="mb-4">
            <MonthYearSelector
              month={month}
              year={year}
              onChange={(nextMonth, nextYear) => { setMonth(nextMonth); setYear(nextYear); }}
            />
          </div>
          <TagExpenseReport startDate={reportStart} endDate={reportEnd} />
        </Card>
      </div>
    </div>
  );
}
