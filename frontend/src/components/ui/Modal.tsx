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

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 sm:p-6">
      {/* Backdrop Glassmorphism */}
      <div 
        className="absolute inset-0 bg-[#2C2A29]/20 backdrop-blur-sm transition-opacity"
        onClick={onClose}
        aria-hidden="true"
      />
      
      {/* Contenedor Bento/Cozy */}
      <div 
        className={cn(
          "relative w-full max-w-lg transform overflow-hidden rounded-[28px] bg-[#FBF9F4] p-6 text-left align-middle shadow-2xl transition-all border border-[#EFEAE2]",
          className
        )}
      >
        {/* Renderizado condicional del título integrado (por si se usa sin ModalHeader) */}
        {title && (
          <div className="flex items-center justify-between mb-6">
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
        
        <div className="mt-2">
          {children}
        </div>

        {!title && (
          <button
            onClick={onClose}
            className="absolute top-6 right-6 rounded-full p-2 text-[#7C756E] transition-colors hover:bg-[#EFEAE2] hover:text-[#2C2A29]"
            aria-label="Cerrar"
          >
            <X className="h-5 w-5" />
          </button>
        )}
      </div>
    </div>
  );
}

// 2. ModalHeader
export interface ModalHeaderProps {
  children: ReactNode;
  className?: string;
}

export function ModalHeader({ children, className }: ModalHeaderProps) {
  return (
    <div className={cn("mb-6 flex flex-col space-y-2 text-center sm:text-left", className)}>
      {children}
    </div>
  );
}

// 3. ModalBody (¡El que faltaba!)
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

// 4. ModalFooter
export interface ModalFooterProps {
  children: ReactNode;
  className?: string;
}

export function ModalFooter({ children, className }: ModalFooterProps) {
  return (
    <div className={cn("mt-6 flex flex-col-reverse sm:flex-row sm:justify-end sm:space-x-2 gap-2 sm:gap-0", className)}>
      {children}
    </div>
  );
}