import { useEffect, useRef, type ReactNode } from 'react';
import { X } from 'lucide-react';
import { createPortal } from 'react-dom';
import { cn } from '../../utils/cn';

interface BottomSheetProps {
  open: boolean;
  onClose: () => void;
  children: ReactNode;
  title?: string;
}

export function BottomSheet({ open, onClose, children, title }: BottomSheetProps) {
  const sheetRef = useRef<HTMLDivElement>(null);
  const touchStartY = useRef(0);

  useEffect(() => {
    if (!open) return;
    const prev = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => { document.body.style.overflow = prev; };
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [open, onClose]);

  // Basic focus trap: focus first focusable child when sheet opens
  useEffect(() => {
    if (!open || !sheetRef.current) return;
    const el = sheetRef.current.querySelector<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    el?.focus();
  }, [open]);

  const handleTouchStart = (e: React.TouchEvent) => {
    touchStartY.current = e.touches[0].clientY;
  };

  const handleTouchEnd = (e: React.TouchEvent) => {
    if (e.changedTouches[0].clientY - touchStartY.current > 80) onClose();
  };

  return createPortal(
    <div
      className={cn(
        'fixed inset-0 z-[200] flex flex-col justify-end',
        'transition-all duration-300',
        open ? 'pointer-events-auto' : 'pointer-events-none'
      )}
      aria-hidden={!open}
    >
      {/* Overlay */}
      <div
        className={cn(
          'absolute inset-0 bg-finflow-dark/40 backdrop-blur-sm transition-opacity duration-300',
          open ? 'opacity-100' : 'opacity-0'
        )}
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Sheet panel */}
      <div
        ref={sheetRef}
        role="dialog"
        aria-modal="true"
        aria-label={title ?? 'Panel de opciones'}
        className={cn(
          'relative flex max-h-[85dvh] flex-col',
          'rounded-t-[28px] border-t border-[#EFEAE2] bg-finflow-cream shadow-2xl',
          'transition-transform duration-300 ease-out',
          open ? 'translate-y-0' : 'translate-y-full'
        )}
        style={{ paddingBottom: 'max(1.5rem, env(safe-area-inset-bottom))' }}
        onTouchStart={handleTouchStart}
        onTouchEnd={handleTouchEnd}
      >
        {/* Drag handle */}
        <div className="flex justify-center pb-1 pt-3" aria-hidden="true">
          <div className="h-1 w-10 rounded-full bg-[#EFEAE2]" />
        </div>

        {/* Title row */}
        {title && (
          <div className="flex shrink-0 items-center justify-between px-6 pb-4 pt-2">
            <h2 className="font-serif text-lg font-medium text-finflow-dark">{title}</h2>
            <button
              type="button"
              onClick={onClose}
              className="rounded-full p-2 text-finflow-muted transition-colors hover:bg-[#EFEAE2]"
              aria-label="Cerrar"
            >
              <X className="h-5 w-5" />
            </button>
          </div>
        )}

        {/* Scrollable content */}
        <div className="min-h-0 flex-1 overflow-y-auto px-6 pb-2">
          {children}
        </div>
      </div>
    </div>,
    document.body
  );
}
