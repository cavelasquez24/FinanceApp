import { useMemo, useState } from 'react';
import { Plus, X } from 'lucide-react';
import { useCreateTag, useTags } from '../hooks/useTags';

interface Props {
  value: string[];
  onChange: (ids: string[]) => void;
  label?: string;
}

export function TagSelector({ value, onChange, label = 'Etiquetas' }: Props) {
  const [search, setSearch] = useState('');
  const { data: tags = [] } = useTags();
  const createTag = useCreateTag();
  const selected = tags.filter((tag) => value.includes(tag.id));
  const available = useMemo(() => tags.filter((tag) =>
    !value.includes(tag.id) && tag.name.toLowerCase().includes(search.trim().toLowerCase())
  ).slice(0, 8), [tags, value, search]);
  const exact = tags.some((tag) => tag.name.toLowerCase() === search.trim().toLowerCase());

  const create = () => {
    const name = search.trim();
    if (!name || exact || value.length >= 10) return;
    createTag.mutate({ name }, {
      onSuccess: (response) => {
        const tag = response.data.data;
        if (tag) onChange([...value, tag.id]);
        setSearch('');
      },
    });
  };

  return (
    <div className="space-y-2">
      <label className="text-sm font-medium text-finflow-dark">{label}</label>
      <div className="flex min-h-11 flex-wrap gap-2 rounded-xl border border-[#EFEAE2] bg-white/70 p-2">
        {selected.map((tag) => (
          <button key={tag.id} type="button" onClick={() => onChange(value.filter((id) => id !== tag.id))}
            className="flex items-center gap-1 rounded-full px-2.5 py-1 text-xs text-white"
            style={{ backgroundColor: tag.color ?? 'var(--color-finflow-blue)' }}>
            {tag.name}<X className="h-3 w-3" />
          </button>
        ))}
        <input value={search} onChange={(e) => setSearch(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); create(); } }}
          disabled={value.length >= 10} placeholder={value.length >= 10 ? 'Máximo 10' : 'Buscar o crear...'}
          className="min-w-36 flex-1 bg-transparent px-1 text-sm outline-none" />
      </div>
      {search.trim() && (
        <div className="max-h-44 overflow-auto rounded-xl border border-[#EFEAE2] bg-white p-1 shadow-lg">
          {available.map((tag) => (
            <button key={tag.id} type="button" onClick={() => { onChange([...value, tag.id]); setSearch(''); }}
              className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-[#F3F1EC]">
              <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: tag.color ?? 'var(--color-finflow-blue)' }} />{tag.name}
            </button>
          ))}
          {!exact && value.length < 10 && (
            <button type="button" onClick={create} className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm text-finflow-blue hover:bg-[#F3F1EC]">
              <Plus className="h-4 w-4" />Crear “{search.trim()}”
            </button>
          )}
        </div>
      )}
      <p className="text-xs text-finflow-muted">{value.length}/10 · Puedes usar varias etiquetas.</p>
    </div>
  );
}
