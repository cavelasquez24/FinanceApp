import { cn } from '@/utils/cn';

type SpinnerSize = 'sm' | 'md' | 'lg' | 'xl';

const sizeClasses: Record<SpinnerSize, string> = {
  sm: 'h-4 w-4 border-2',
  md: 'h-6 w-6 border-2',
  lg: 'h-8 w-8 border-[3px]',
  xl: 'h-12 w-12 border-4',
};

interface SpinnerProps {
  size?: SpinnerSize;
  className?: string;
  label?: string;
}

export function Spinner({ size = 'md', className, label = 'Cargando...' }: SpinnerProps) {
  return (
    <span role="status" aria-label={label} className="inline-flex">
      <span
        className={cn(
          'rounded-full border-[#EFEAE2] border-t-[#5C7A99] animate-spin',
          sizeClasses[size],
          className
        )}
      />
      <span className="sr-only">{label}</span>
    </span>
  );
}

// ─── PageSpinner — full-page centered loading state ──────────────────────────

export function PageSpinner({ label }: { label?: string }) {
  return (
    <div className="flex min-h-[200px] flex-col items-center justify-center gap-3">
      <Spinner size="lg" label={label} />
      {label && <p className="text-sm text-[#7C756E]">{label}</p>}
    </div>
  );
}