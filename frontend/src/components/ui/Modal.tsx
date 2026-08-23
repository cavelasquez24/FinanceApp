import { type ReactNode, useEffect } from 'react';
import { X } from 'lucide-react';
import { cn } from '../../utils/cn';
import { ModalPortal } from './ModalPortal';

export interface ModalProps { isOpen: boolean; onClose: () => void; title?: string; children: ReactNode; className?: string; }

export function Modal({ isOpen, onClose, title, children, className }: ModalProps) {
  useEffect(() => {
    if (!isOpen) return;
    const handleKeyDown = (event: KeyboardEvent) => { if (event.key === 'Escape') onClose(); };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;
  return (
    <ModalPortal>
      <div className="fixed inset-0 z-[100] flex min-h-dvh items-center justify-center overflow-y-auto p-3 sm:p-6">
        <div className="absolute inset-0 bg-finflow-dark/20 backdrop-blur-sm" onClick={onClose} aria-hidden="true" />
        <div className={cn('relative flex max-h-[calc(100dvh-1.5rem)] w-full max-w-lg flex-col overflow-hidden rounded-[28px] border border-[#EFEAE2] bg-finflow-cream p-6 text-left shadow-2xl sm:max-h-[calc(100dvh-3rem)]', className)}>
          {title && <div className="mb-6 flex shrink-0 items-center justify-between"><h3 className="font-serif text-xl font-medium text-finflow-dark">{title}</h3><button type="button" onClick={onClose} className="rounded-full p-2 text-finflow-muted hover:bg-[#EFEAE2]" aria-label="Cerrar"><X className="h-5 w-5" /></button></div>}
          <div className="min-h-0 flex-1 overflow-y-auto">{children}</div>
          {!title && <button type="button" onClick={onClose} className="absolute right-6 top-6 rounded-full bg-finflow-cream/80 p-2 text-finflow-muted backdrop-blur-sm hover:bg-[#EFEAE2]" aria-label="Cerrar"><X className="h-5 w-5" /></button>}
        </div>
      </div>
    </ModalPortal>
  );
}

export interface ModalHeaderProps { children: ReactNode; className?: string; }
export function ModalHeader({ children, className }: ModalHeaderProps) { return <div className={cn('sticky top-0 z-10 -mt-2 mb-6 flex flex-col space-y-2 bg-finflow-cream pb-2 pt-2 text-center sm:text-left', className)}>{children}</div>; }
export interface ModalBodyProps { children: ReactNode; className?: string; }
export function ModalBody({ children, className }: ModalBodyProps) { return <div className={cn('py-2', className)}>{children}</div>; }
export interface ModalFooterProps { children: ReactNode; className?: string; }
export function ModalFooter({ children, className }: ModalFooterProps) { return <div className={cn('sticky bottom-0 z-10 -mb-2 mt-6 flex flex-col-reverse gap-2 border-t border-[#EFEAE2] bg-finflow-cream pb-2 pt-4 sm:flex-row sm:justify-end sm:space-x-2', className)}>{children}</div>; }
