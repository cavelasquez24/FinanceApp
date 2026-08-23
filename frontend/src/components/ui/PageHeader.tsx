import type { ReactNode } from 'react';

interface PageHeaderProps {
  eyebrow: string;
  title: string;
  description?: string;
  action?: ReactNode;
}

export function PageHeader({ eyebrow, title, description, action }: PageHeaderProps) {
  return (
    <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
      <div>
        <p className="text-xs font-semibold uppercase tracking-widest text-finflow-muted">
          {eyebrow}
        </p>
        <h1 className="mt-1 font-serif text-3xl font-semibold text-finflow-dark">{title}</h1>
        {description && (
          <p className="mt-2 max-w-lg text-sm text-finflow-muted">{description}</p>
        )}
      </div>
      {action && <div className="shrink-0">{action}</div>}
    </div>
  );
}
