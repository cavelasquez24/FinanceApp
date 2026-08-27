import { useCallback, useEffect, useRef, type ReactNode } from 'react';
import { X } from 'lucide-react';
import { createPortal } from 'react-dom';
import { cn } from '../../utils/cn';

interface BottomSheetProps {
  open: boolean;
  onClose: () => void;
  children: ReactNode;
  title?: string;
}

/** Desplazamiento vertical mínimo (px) para que el gesto cuente como cierre. */
const CLOSE_THRESHOLD = 80;
/**
 * Margen tras un pinch en el que se siguen ignorando los gestos. Al terminar
 * un `gestureend` iOS todavía emite `touchend` de los dedos que se levantan;
 * sin esta ventana ese touchend se interpretaría como swipe de cierre.
 */
const PINCH_COOLDOWN_MS = 400;

export function BottomSheet({ open, onClose, children, title }: BottomSheetProps) {
  const sheetRef = useRef<HTMLDivElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);
  const grabRef = useRef<HTMLDivElement>(null);

  // Estado del gesto de arrastre en curso.
  const dragging = useRef(false);
  const moved = useRef(false);
  const startX = useRef(0);
  const startY = useRef(0);

  // Pinch-zoom en curso (o recién terminado): suprime cualquier cierre.
  const pinching = useRef(false);
  const pinchTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const cancelDrag = useCallback(() => {
    dragging.current = false;
    moved.current = false;
  }, []);

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

  /*
   * Eventos `gesture*` de WebKit: son la señal fiable de que el usuario está
   * haciendo pinch-zoom sobre la página. Mientras dura (y durante el cooldown)
   * se descarta el gesto de arrastre, que si no cerraría la hoja al separar o
   * juntar los dedos.
   */
  useEffect(() => {
    if (!open) return;

    const onGestureStart = () => {
      pinching.current = true;
      cancelDrag();
      if (pinchTimer.current) {
        clearTimeout(pinchTimer.current);
        pinchTimer.current = null;
      }
    };

    const onGestureEnd = () => {
      if (pinchTimer.current) clearTimeout(pinchTimer.current);
      pinchTimer.current = setTimeout(() => {
        pinching.current = false;
        pinchTimer.current = null;
      }, PINCH_COOLDOWN_MS);
    };

    document.addEventListener('gesturestart', onGestureStart);
    document.addEventListener('gesturechange', onGestureStart);
    document.addEventListener('gestureend', onGestureEnd);

    return () => {
      document.removeEventListener('gesturestart', onGestureStart);
      document.removeEventListener('gesturechange', onGestureStart);
      document.removeEventListener('gestureend', onGestureEnd);
      if (pinchTimer.current) {
        clearTimeout(pinchTimer.current);
        pinchTimer.current = null;
      }
      pinching.current = false;
    };
  }, [open, cancelDrag]);

  // Basic focus trap: focus first focusable child when sheet opens
  useEffect(() => {
    if (!open || !sheetRef.current) return;
    const el = sheetRef.current.querySelector<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    el?.focus();
  }, [open]);

  const handleTouchStart = (e: React.TouchEvent) => {
    // Multitáctil = pinch, nunca un swipe de cierre.
    if (e.touches.length > 1 || pinching.current) {
      cancelDrag();
      return;
    }

    const target = e.target as Node;
    const fromGrabArea = grabRef.current?.contains(target) ?? false;
    // Fuera de la cabecera solo se arrastra si el contenido ya está arriba del
    // todo; si no, el usuario está haciendo scroll dentro del formulario.
    const contentAtTop = (contentRef.current?.scrollTop ?? 0) <= 0;
    if (!fromGrabArea && !contentAtTop) {
      cancelDrag();
      return;
    }

    dragging.current = true;
    moved.current = false;
    startX.current = e.touches[0].clientX;
    startY.current = e.touches[0].clientY;
  };

  const handleTouchMove = (e: React.TouchEvent) => {
    if (e.touches.length > 1 || pinching.current) {
      cancelDrag();
      return;
    }
    moved.current = true;
  };

  const handleTouchEnd = (e: React.TouchEvent) => {
    const wasDragging = dragging.current && moved.current && !pinching.current;
    // Quedan dedos en pantalla: el gesto aún no ha terminado.
    const lastFingerUp = e.touches.length === 0;
    cancelDrag();
    if (!wasDragging || !lastFingerUp) return;

    const dx = e.changedTouches[0].clientX - startX.current;
    const dy = e.changedTouches[0].clientY - startY.current;
    // Exige un gesto claramente vertical y hacia abajo.
    if (dy > CLOSE_THRESHOLD && dy > Math.abs(dx) * 1.5) onClose();
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
        onClick={(e) => {
          // Solo el clic directo sobre el overlay cierra, y nunca durante un pinch.
          if (e.target !== e.currentTarget || pinching.current) return;
          onClose();
        }}
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
        onTouchMove={handleTouchMove}
        onTouchEnd={handleTouchEnd}
        onTouchCancel={cancelDrag}
      >
        {/* Zona de agarre: handle + cabecera */}
        <div ref={grabRef} className="shrink-0">
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
        </div>

        {/* Scrollable content */}
        <div ref={contentRef} className="min-h-0 flex-1 overflow-y-auto px-6 pb-2">
          {children}
        </div>
      </div>
    </div>,
    document.body
  );
}
