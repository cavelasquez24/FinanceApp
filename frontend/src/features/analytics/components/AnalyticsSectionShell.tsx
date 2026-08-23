import { type ReactNode } from 'react';
import { cn } from '../../../utils/cn';

interface Props {
  title: string;
  subtitle?: string;
  children: ReactNode;
  isLoading?: boolean;
  className?: string;
  action?: ReactNode;
}

function SkeletonBlock({ className }: { className?: string }) {
  return (
    <div className={cn('animate-pulse rounded-2xl bg-[#EFEAE2]/60', className)} />
  );
}

export function AnalyticsSectionShell({
  title,
  subtitle,
  children,
  isLoading = false,
  className,
  action,
}: Props) {
  return (
    <section
      className={cn(
        'rounded-[28px] border border-[#EFEAE2]/70 bg-white/60 p-6 shadow-[0_8px_30px_rgba(44,42,41,0.06)] backdrop-blur-xl',
        className
      )}
    >
      <div className="mb-4 flex items-start justify-between gap-4">
        <div className="min-w-0">
          <h2 className="text-sm font-semibold text-finflow-dark">{title}</h2>
          {subtitle && (
            <p className="mt-0.5 text-xs text-finflow-muted">{subtitle}</p>
          )}
        </div>
        {action && <div className="shrink-0">{action}</div>}
      </div>

      {isLoading ? (
        <div className="space-y-3">
          <SkeletonBlock className="h-8 w-2/3" />
          <SkeletonBlock className="h-32 w-full" />
          <SkeletonBlock className="h-6 w-1/2" />
        </div>
      ) : (
        children
      )}
    </section>
  );
}
