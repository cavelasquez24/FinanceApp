//migrado
import { forwardRef, type InputHTMLAttributes, type ReactNode } from 'react';
import { cn } from '@/utils/cn';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  hint?: string;
  leftIcon?: ReactNode;
  rightIcon?: ReactNode;
  /** Replaces rightIcon with a tappable button (e.g. password toggle) */
  rightAction?: ReactNode;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  (
    {
      label,
      error,
      hint,
      leftIcon,
      rightIcon,
      rightAction,
      className,
      id,
      disabled,
      ...props
    },
    ref
  ) => {
    const inputId =
      id ?? (label ? label.toLowerCase().replace(/\s+/g, '-') : undefined);

    return (
      <div className="flex w-full flex-col gap-1.5">
        {label && (
          <label
            htmlFor={inputId}
            className={cn(
              'text-sm font-medium text-[#2C2A29]',
              disabled && 'text-[#7C756E]/50'
            )}
          >
            {label}
          </label>
        )}

        <div className="relative flex items-center">
          {leftIcon && (
            <span
              className={cn(
                'pointer-events-none absolute left-3.5 flex items-center text-[#7C756E]',
                error && 'text-[#C97B63]'
              )}
            >
              {leftIcon}
            </span>
          )}

          <input
            ref={ref}
            id={inputId}
            disabled={disabled}
            className={cn(
              'w-full rounded-xl border bg-white/70 text-sm text-[#2C2A29] backdrop-blur-sm',
              'placeholder:text-[#7C756E]/70',
              'transition-colors duration-150',
              'focus:outline-none focus:ring-2 focus:ring-offset-0',
              'py-2.5',
              leftIcon ? 'pl-10' : 'pl-3.5',
              rightIcon || rightAction ? 'pr-10' : 'pr-3.5',

              !error
                ? 'border-[#EFEAE2] focus:border-[#5C7A99] focus:ring-[#5C7A99]/20'
                : 'border-[#C97B63]/60 bg-[#FBEEEA]/60 focus:border-[#C97B63] focus:ring-[#C97B63]/20',

              disabled &&
                'cursor-not-allowed border-[#EFEAE2] bg-[#F3F1EC] text-[#7C756E]/60',

              className
            )}
            {...props}
          />

          {rightIcon && !rightAction && (
            <span className="pointer-events-none absolute right-3.5 flex items-center text-[#7C756E]">
              {rightIcon}
            </span>
          )}

          {rightAction && (
            <span className="absolute right-3.5 flex items-center">
              {rightAction}
            </span>
          )}
        </div>

        {error ? (
          <p className="flex items-center gap-1 text-xs text-[#B5573F]">
            <svg
              className="h-3.5 w-3.5 shrink-0"
              viewBox="0 0 16 16"
              fill="currentColor"
              aria-hidden="true"
            >
              <path d="M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1zm.75 4.5v3.25a.75.75 0 0 1-1.5 0V5.5a.75.75 0 0 1 1.5 0zm-.75 6a.875.875 0 1 1 0-1.75.875.875 0 0 1 0 1.75z" />
            </svg>
            {error}
          </p>
        ) : hint ? (
          <p className="text-xs text-[#7C756E]">{hint}</p>
        ) : null}
      </div>
    );
  }
);

Input.displayName = 'Input';