//migrado
import { forwardRef, type ButtonHTMLAttributes } from 'react';
import { cn } from '@/utils/cn';

type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost';
type ButtonSize = 'sm' | 'md' | 'lg';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  isLoading?: boolean;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
}

const variantStyles: Record<ButtonVariant, string> = {
  primary:
    'bg-finflow-dark text-finflow-cream hover:bg-[#1F1E1D] active:bg-[#141312] ' +
    'focus-visible:ring-finflow-dark/30 disabled:bg-finflow-dark/30',
  secondary:
    'bg-white/70 text-finflow-dark border border-[#EFEAE2] backdrop-blur-sm hover:bg-[#F3F1EC] active:bg-[#EFEAE2] ' +
    'focus-visible:ring-finflow-blue/30 disabled:text-finflow-muted/50 disabled:border-[#EFEAE2]/60',
  danger:
    'bg-finflow-rust text-white hover:bg-[#B5684F] active:bg-[#9C5941] ' +
    'focus-visible:ring-finflow-rust/30 disabled:bg-finflow-rust/30',
  ghost:
    'bg-transparent text-finflow-muted hover:bg-[#F3F1EC] active:bg-[#EFEAE2] ' +
    'focus-visible:ring-finflow-blue/30 disabled:text-finflow-muted/40',
};

const sizeStyles: Record<ButtonSize, string> = {
  sm: 'h-8 px-3 text-sm gap-1.5',
  md: 'h-10 px-4 text-sm gap-2',
  lg: 'h-11 px-5 text-base gap-2',
};

const Spinner = () => (
  <svg
    className="animate-spin h-4 w-4"
    xmlns="http://www.w3.org/2000/svg"
    fill="none"
    viewBox="0 0 24 24"
    aria-hidden="true"
  >
    <circle
      className="opacity-25"
      cx="12"
      cy="12"
      r="10"
      stroke="currentColor"
      strokeWidth="4"
    />
    <path
      className="opacity-75"
      fill="currentColor"
      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
    />
  </svg>
);

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      variant = 'primary',
      size = 'md',
      isLoading = false,
      leftIcon,
      rightIcon,
      className,
      disabled,
      children,
      ...props
    },
    ref
  ) => {
    const isDisabled = disabled || isLoading;

    return (
      <button
        ref={ref}
        disabled={isDisabled}
        className={cn(
          // Base
          'inline-flex items-center justify-center font-medium rounded-xl',
          'transition-colors duration-150',
          'focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-offset-finflow-cream',
          'disabled:cursor-not-allowed',
          // Variant & size
          variantStyles[variant],
          sizeStyles[size],
          className
        )}
        {...props}
      >
        {isLoading ? (
          <>
            <Spinner />
            <span className="inline-flex items-center justify-center gap-2 whitespace-nowrap">{children}</span>
          </>
        ) : (
          <>
            {leftIcon && <span className="shrink-0">{leftIcon}</span>}
            <span className="inline-flex items-center justify-center gap-2 whitespace-nowrap">{children}</span>
            {rightIcon && <span className="shrink-0">{rightIcon}</span>}
          </>
        )}
      </button>
    );
  }
);

Button.displayName = 'Button';