import { type ReactNode, useEffect } from 'react';
import { X } from 'lucide-react';
import { cn } from '../../utils/cn';

export interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  children: ReactNode;
  className?: string;
}

// 1. Componente Principal Modal
export function Modal({ isOpen, onClose, title, children, className }: ModalProps) {
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = 'unset';
    }
    return () => { document.body.style.overflow = 'unset'; };
  }, [isOpen]);

  // Cerrar con Escape como respaldo adicional a la X y al backdrop
  useEffect(() => {
    if (!isOpen) return;
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 sm:p-6">
      {/* Backdrop Glassmorphism */}
      <div 
        className="absolute inset-0 bg-[#2C2A29]/20 backdrop-blur-sm transition-opacity"
        onClick={onClose}
        aria-hidden="true"
      />
      
      {/* Contenedor Bento/Cozy — ahora acotado en altura y con flex-col para permitir scroll interno */}
      <div 
        className={cn(
          "relative flex w-full max-w-lg max-h-[90vh] transform flex-col overflow-hidden rounded-[28px] bg-[#FBF9F4] p-6 text-left align-middle shadow-2xl transition-all border border-[#EFEAE2]",
          className
        )}
      >
        {/* Título integrado (por si se usa sin ModalHeader) — fijo, fuera del área con scroll */}
        {title && (
          <div className="flex shrink-0 items-center justify-between mb-6">
            <h3 className="font-serif text-xl font-medium text-[#2C2A29]">
              {title}
            </h3>
            <button
              onClick={onClose}
              className="rounded-full p-2 text-[#7C756E] transition-colors hover:bg-[#EFEAE2] hover:text-[#2C2A29]"
              aria-label="Cerrar"
            >
              <X className="h-5 w-5" />
            </button>
          </div>
        )}
        
        {/* Única zona con scroll interno: si el contenido excede el alto disponible, scrollea aquí, no la página */}
        <div className="mt-2 min-h-0 flex-1 overflow-y-auto">
          {children}
        </div>

        {!title && (
          <button
            onClick={onClose}
            className="absolute top-6 right-6 rounded-full p-2 text-[#7C756E] transition-colors hover:bg-[#EFEAE2] hover:text-[#2C2A29] bg-[#FBF9F4]/80 backdrop-blur-sm"
            aria-label="Cerrar"
          >
            <X className="h-5 w-5" />
          </button>
        )}
      </div>
    </div>
  );
}

// 2. ModalHeader — sticky al tope de la zona con scroll
export interface ModalHeaderProps {
  children: ReactNode;
  className?: string;
}

export function ModalHeader({ children, className }: ModalHeaderProps) {
  return (
    <div className={cn(
      "sticky top-0 z-10 -mt-2 mb-6 flex flex-col space-y-2 bg-[#FBF9F4] pt-2 pb-2 text-center sm:text-left",
      className
    )}>
      {children}
    </div>
  );
}

// 3. ModalBody
export interface ModalBodyProps {
  children: ReactNode;
  className?: string;
}

export function ModalBody({ children, className }: ModalBodyProps) {
  return (
    <div className={cn("py-2", className)}>
      {children}
    </div>
  );
}

// 4. ModalFooter — sticky al fondo de la zona con scroll, siempre visible
export interface ModalFooterProps {
  children: ReactNode;
  className?: string;
}

export function ModalFooter({ children, className }: ModalFooterProps) {
  return (
    <div className={cn(
      "sticky bottom-0 z-10 -mb-2 mt-6 flex flex-col-reverse gap-2 bg-[#FBF9F4] pt-4 pb-2 border-t border-[#EFEAE2] sm:flex-row sm:justify-end sm:gap-0 sm:space-x-2",
      className
    )}>
      {children}
    </div>
  );
}