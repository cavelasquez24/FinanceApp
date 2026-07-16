import { type HTMLAttributes, type ReactNode } from 'react';
import { cn } from '@/utils/cn';

// ─── Card (root) ────────────────────────────────────────────────────────────

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  /** Removes padding — useful when you need a full-bleed table or image */
  noPadding?: boolean;
}

export function Card({ noPadding = false, className, children, ...props }: CardProps) {
  return (
    <div
      className={cn(
        'rounded-[28px] border border-[#EFEAE2]/70 bg-white/60 shadow-[0_8px_30px_rgba(44,42,41,0.06)] backdrop-blur-xl',
        !noPadding && 'p-6',
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
}

// ─── CardHeader ─────────────────────────────────────────────────────────────

interface CardHeaderProps extends HTMLAttributes<HTMLDivElement> {
  title: string;
  subtitle?: string;
  /** Element rendered in the top-right corner (e.g. a Button or Badge) */
  action?: ReactNode;
}

export function CardHeader({ title, subtitle, action, className, ...props }: CardHeaderProps) {
  return (
    <div
      className={cn('flex items-start justify-between gap-4 mb-4', className)}
      {...props}
    >
      <div className="min-w-0">
        <h3 className="truncate text-sm font-semibold text-[#2C2A29]">{title}</h3>
        {subtitle && (
          <p className="mt-0.5 truncate text-xs text-[#7C756E]">{subtitle}</p>
        )}
      </div>
      {action && <div className="shrink-0">{action}</div>}
    </div>
  );
}

// ─── CardBody ────────────────────────────────────────────────────────────────

export function CardBody({ className, children, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div className={cn('flex flex-col', className)} {...props}>
      {children}
    </div>
  );
}

// ─── CardFooter ─────────────────────────────────────────────────────────────

interface CardFooterProps extends HTMLAttributes<HTMLDivElement> {
  /** Adds a top border separator */
  divided?: boolean;
}

export function CardFooter({ divided = false, className, children, ...props }: CardFooterProps) {
  return (
    <div
      className={cn(
        'mt-4 flex items-center justify-between gap-3',
        divided && 'border-t border-[#EFEAE2] pt-4',
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
}

// ─── KpiCard — pre-built for dashboard metrics ───────────────────────────────

interface KpiCardProps {
  label: string;
  value: string;
  /** e.g. "+12.4%" or "-3.1%" */
  change?: string;
  /** Positive = sage arrow up, negative = terracotta arrow down */
  changeDirection?: 'up' | 'down' | 'neutral';
  icon?: ReactNode;
  className?: string;
}

export function KpiCard({
  label,
  value,
  change,
  changeDirection = 'neutral',
  icon,
  className,
}: KpiCardProps) {
  const changeColor =
    changeDirection === 'up'
      ? 'text-[#8FA888]'
      : changeDirection === 'down'
      ? 'text-[#C97B63]'
      : 'text-[#7C756E]';

  const Arrow =
    changeDirection === 'up' ? (
      <svg className="h-3.5 w-3.5" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
        <path d="M8 3.5 3 9h3.5v4h3V9H13L8 3.5z" />
      </svg>
    ) : changeDirection === 'down' ? (
      <svg className="h-3.5 w-3.5" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
        <path d="M8 12.5 13 7H9.5V3h-3v4H3l5 5.5z" />
      </svg>
    ) : null;

  return (
    <Card className={cn('flex flex-col gap-3', className)}>
      <div className="flex items-center justify-between">
        <span className="text-xs font-medium uppercase tracking-wide text-[#7C756E]">
          {label}
        </span>
        {icon && <span className="text-[#7C756E]">{icon}</span>}
      </div>

      <span className="text-2xl font-semibold tracking-tight text-[#2C2A29]">{value}</span>

      {change && (
        <div className={cn('flex items-center gap-1 text-xs font-medium', changeColor)}>
          {Arrow}
          <span>{change} vs mes anterior</span>
        </div>
      )}
    </Card>
  );
}