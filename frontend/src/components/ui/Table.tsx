import { type ReactNode, type ThHTMLAttributes, type TdHTMLAttributes } from 'react';
import { cn } from '@/utils/cn';
import { Button } from './Button';
import { Spinner } from './Spinner';

// ─── Table (root) ────────────────────────────────────────────────────────────

interface TableProps {
  children: ReactNode;
  className?: string;
}

export function Table({ children, className }: TableProps) {
  return (
    <div className={cn('w-full overflow-x-auto', className)}>
      <table className="w-full text-sm border-collapse">{children}</table>
    </div>
  );
}

// ─── TableHead ───────────────────────────────────────────────────────────────

export function TableHead({ children }: { children: ReactNode }) {
  return (
    <thead className="bg-slate-50 border-y border-slate-200">
      <tr>{children}</tr>
    </thead>
  );
}

// ─── Th ──────────────────────────────────────────────────────────────────────

interface ThProps extends ThHTMLAttributes<HTMLTableCellElement> {
  /** Shows an up/down sort indicator */
  sortDirection?: 'asc' | 'desc' | null;
  onSort?: () => void;
}

export function Th({ children, sortDirection, onSort, className, ...props }: ThProps) {
  const isSortable = !!onSort;

  return (
    <th
      scope="col"
      onClick={onSort}
      className={cn(
        'px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wide',
        'whitespace-nowrap select-none',
        isSortable && 'cursor-pointer hover:text-slate-700 transition-colors duration-100',
        className
      )}
      {...props}
    >
      <span className="inline-flex items-center gap-1.5">
        {children}
        {isSortable && (
          <span className="text-slate-300" aria-hidden="true">
            {sortDirection === 'asc' ? '↑' : sortDirection === 'desc' ? '↓' : '↕'}
          </span>
        )}
      </span>
    </th>
  );
}

// ─── TableBody ───────────────────────────────────────────────────────────────

export function TableBody({ children }: { children: ReactNode }) {
  return <tbody className="divide-y divide-slate-100">{children}</tbody>;
}

// ─── Tr ──────────────────────────────────────────────────────────────────────

interface TrProps {
  children: ReactNode;
  onClick?: () => void;
  className?: string;
}

export function Tr({ children, onClick, className }: TrProps) {
  return (
    <tr
      onClick={onClick}
      className={cn(
        'bg-white transition-colors duration-100',
        onClick && 'cursor-pointer hover:bg-slate-50',
        className
      )}
    >
      {children}
    </tr>
  );
}

// ─── Td ──────────────────────────────────────────────────────────────────────

export function Td({
  children,
  className,
  ...props
}: TdHTMLAttributes<HTMLTableCellElement>) {
  return (
    <td
      className={cn('px-4 py-3 text-slate-700 whitespace-nowrap', className)}
      {...props}
    >
      {children}
    </td>
  );
}

// ─── TableEmpty ──────────────────────────────────────────────────────────────

interface TableEmptyProps {
  colSpan: number;
  message?: string;
  action?: ReactNode;
}

export function TableEmpty({
  colSpan,
  message = 'No hay registros',
  action,
}: TableEmptyProps) {
  return (
    <tr>
      <td colSpan={colSpan}>
        <div className="flex flex-col items-center justify-center py-12 gap-3">
          <svg
            className="h-10 w-10 text-slate-300"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={1.5}
              d="M9 17v-6m3 6V7m3 10v-4M3 20h18M3 4h18"
            />
          </svg>
          <p className="text-sm text-slate-400">{message}</p>
          {action}
        </div>
      </td>
    </tr>
  );
}

// ─── TableLoading ────────────────────────────────────────────────────────────

export function TableLoading({ colSpan }: { colSpan: number }) {
  return (
    <tr>
      <td colSpan={colSpan}>
        <div className="flex items-center justify-center py-12">
          <Spinner size="md" />
        </div>
      </td>
    </tr>
  );
}

// ─── TablePagination ─────────────────────────────────────────────────────────

interface TablePaginationProps {
  page: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  onPageChange: (page: number) => void;
}

export function TablePagination({
  page,
  totalPages,
  totalCount,
  pageSize,
  hasNextPage,
  hasPreviousPage,
  onPageChange,
}: TablePaginationProps) {
  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, totalCount);

  return (
    <div className="flex items-center justify-between px-4 py-3 border-t border-slate-200 bg-white">
      {/* Count */}
      <span className="text-xs text-slate-500">
        {totalCount === 0
          ? 'Sin resultados'
          : `Mostrando ${from}–${to} de ${totalCount}`}
      </span>

      {/* Controls */}
      <div className="flex items-center gap-1">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => onPageChange(1)}
          disabled={!hasPreviousPage}
          aria-label="Primera página"
        >
          «
        </Button>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => onPageChange(page - 1)}
          disabled={!hasPreviousPage}
          aria-label="Página anterior"
        >
          ‹
        </Button>

        <span className="px-3 py-1 text-xs text-slate-700 font-medium">
          {page} / {totalPages || 1}
        </span>

        <Button
          variant="ghost"
          size="sm"
          onClick={() => onPageChange(page + 1)}
          disabled={!hasNextPage}
          aria-label="Página siguiente"
        >
          ›
        </Button>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => onPageChange(totalPages)}
          disabled={!hasNextPage}
          aria-label="Última página"
        >
          »
        </Button>
      </div>
    </div>
  );
}
