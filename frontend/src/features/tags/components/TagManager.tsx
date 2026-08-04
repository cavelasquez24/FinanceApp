import { useState } from 'react';
import { Check, Merge, Pencil, Plus, Trash2, X } from 'lucide-react';
import { useCreateTag, useDeleteTag, useMergeTags, useTags, useUpdateTag } from '../hooks/useTags';

export function TagManager() {
  const { data: tags = [] } = useTags();
  const update = useUpdateTag();
  const remove = useDeleteTag();
  const merge = useMergeTags();
  const create = useCreateTag();
  const [editing, setEditing] = useState<string | null>(null);
  const [name, setName] = useState('');
  const [source, setSource] = useState<string | null>(null);
  const [target, setTarget] = useState('');
  const [newName, setNewName] = useState('');

  return (
    <div className="space-y-3">
      <form
        className="flex flex-col gap-2 sm:flex-row"
        onSubmit={(event) => {
          event.preventDefault();
          const cleanName = newName.trim();
          if (!cleanName) return;
          create.mutate({ name: cleanName }, { onSuccess: () => setNewName('') });
        }}
      >
        <input
          value={newName}
          onChange={(event) => setNewName(event.target.value)}
          maxLength={50}
          placeholder="Ej.: amigos, almuerzo, trabajo"
          className="min-w-0 flex-1 rounded-xl border border-[#EFEAE2] bg-white px-3 py-2 text-sm outline-none focus:border-[#5C7A99]"
        />
        <button type="submit" disabled={!newName.trim() || create.isPending} className="flex items-center justify-center gap-2 rounded-xl bg-[#2C2A29] px-4 py-2 text-sm text-white disabled:opacity-40">
          <Plus className="h-4 w-4" /> Crear etiqueta
        </button>
      </form>
      {tags.length === 0 && <p className="text-sm text-[#7C756E]">Todavía no tienes etiquetas. Crea la primera aquí o desde un gasto.</p>}
      <div className="flex flex-wrap gap-2">
        {tags.map((tag) => (
          <div key={tag.id} className="flex items-center gap-1 rounded-full border border-[#EFEAE2] bg-white px-2 py-1">
            <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: tag.color ?? '#5C7A99' }} />
            {editing === tag.id ? (
              <>
                <input value={name} onChange={(e) => setName(e.target.value)} className="w-28 bg-transparent text-sm outline-none" />
                <button type="button" onClick={() => update.mutate({ id: tag.id, name }, { onSuccess: () => setEditing(null) })}><Check className="h-3.5 w-3.5" /></button>
                <button type="button" onClick={() => setEditing(null)}><X className="h-3.5 w-3.5" /></button>
              </>
            ) : (
              <>
                <span className="text-sm">{tag.name}</span>
                <span className="text-xs text-[#7C756E]">({tag.usageCount})</span>
                <button type="button" aria-label="Editar etiqueta" onClick={() => { setEditing(tag.id); setName(tag.name); }}><Pencil className="h-3.5 w-3.5" /></button>
                <button type="button" aria-label="Fusionar etiqueta" onClick={() => { setSource(tag.id); setTarget(''); }}><Merge className="h-3.5 w-3.5" /></button>
                <button type="button" aria-label="Eliminar etiqueta" onClick={() => { if (confirm(`¿Eliminar la etiqueta “${tag.name}”? Los gastos conservarán su historial.`)) remove.mutate(tag.id); }}><Trash2 className="h-3.5 w-3.5 text-[#C97B63]" /></button>
              </>
            )}
          </div>
        ))}
      </div>
      {source && (
        <div className="flex flex-wrap items-center gap-2 rounded-xl bg-[#F3F1EC] p-3 text-sm">
          <span>Fusionar <strong>{tags.find((t) => t.id === source)?.name}</strong> en</span>
          <select value={target} onChange={(e) => setTarget(e.target.value)} className="rounded-lg border border-[#EFEAE2] bg-white px-2 py-1">
            <option value="">Selecciona destino</option>
            {tags.filter((t) => t.id !== source).map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
          </select>
          <button type="button" disabled={!target} onClick={() => merge.mutate({ sourceId: source, targetTagId: target }, { onSuccess: () => setSource(null) })}
            className="rounded-lg bg-[#2C2A29] px-3 py-1.5 text-white disabled:opacity-40">Fusionar</button>
          <button type="button" onClick={() => setSource(null)}>Cancelar</button>
        </div>
      )}
    </div>
  );
}
