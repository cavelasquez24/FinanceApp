import { type HTMLAttributes } from 'react';
import { cn } from '@/utils/cn';

type BadgeVariant = 'default' | 'success' | 'warning' | 'danger' | 'info' | 'neutral';

const variantStyles: Record<BadgeVariant, string> = {
  default:  'bg-blue-50 text-blue-700 border-blue-200',
  success:  'bg-emerald-50 text-emerald-700 border-emerald-200',
  warning:  'bg-amber-50 text-amber-700 border-amber-200',
  danger:   'bg-red-50 text-red-700 border-red-200',
  info:     'bg-slate-50 text-slate-600 border-slate-200',
  neutral:  'bg-slate-100 text-slate-600 border-slate-200',
};

// ─── Badge ───────────────────────────────────────────────────────────────────

interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  variant?: BadgeVariant;
  /** Use when you want to pass a raw hex color (e.g. from the API) */
  color?: string;
  dot?: boolean;
}

export function Badge({
  variant = 'default',
  color,
  dot = false,
  className,
  children,
  ...props
}: BadgeProps) {
  const customStyle = color
    ? {
        backgroundColor: `${color}18`,  // 10% opacity
        color: color,
        borderColor: `${color}40`,       // 25% opacity
      }
    : undefined;

  return (
    <span
      style={customStyle}
      className={cn(
        'inline-flex items-center gap-1.5 px-2 py-0.5',
        'text-xs font-medium rounded-full border',
        !color && variantStyles[variant],
        className
      )}
      {...props}
    >
      {dot && (
        <span
          className="h-1.5 w-1.5 rounded-full shrink-0"
          style={color ? { backgroundColor: color } : undefined}
          aria-hidden="true"
        />
      )}
      {children}
    </span>
  );
}

// ─── CategoryBadge — pre-built for API category objects ──────────────────────

interface CategoryBadgeProps {
  name: string;
  color: string;  // HEX from API
  icon?: string | null;
  className?: string;
}

export function CategoryBadge({ name, color, icon, className }: CategoryBadgeProps) {
  return (
    <Badge color={color} dot className={className}>
      {icon && <span aria-hidden="true">{icon}</span>}
      {name}
    </Badge>
  );
}

// ─── StatusBadge — for budget/goal status ────────────────────────────────────

type Status = 'ok' | 'warning' | 'over' | 'completed' | 'active';

const statusConfig: Record<Status, { label: string; variant: BadgeVariant }> = {
  ok:        { label: 'En orden',    variant: 'success' },
  warning:   { label: 'Por límite',  variant: 'warning' },
  over:      { label: 'Excedido',    variant: 'danger'  },
  completed: { label: 'Completada',  variant: 'success' },
  active:    { label: 'Activa',      variant: 'default' },
};

export function StatusBadge({ status }: { status: Status }) {
  const { label, variant } = statusConfig[status];
  return <Badge variant={variant} dot>{label}</Badge>;
}
