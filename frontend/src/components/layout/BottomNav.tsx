import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { Home, ArrowLeftRight, Plus, Landmark, Menu } from 'lucide-react';
import { cn } from '../../utils/cn';
import { QuickActionSheet } from './QuickActionSheet';

const NAV_ITEMS = [
  { icon: Home,           label: 'Inicio',       path: '/' },
  { icon: ArrowLeftRight, label: 'Movimientos',   path: '/expenses'  },
  null,
  { icon: Landmark,       label: 'Patrimonio',    path: '/accounts'  },
  { icon: Menu,           label: 'Más',           path: null         },
] as const;

interface BottomNavProps {
  onOpenMenu: () => void;
}

export function BottomNav({ onOpenMenu }: BottomNavProps) {
  const location = useLocation();
  const navigate = useNavigate();
  const [quickOpen, setQuickOpen] = useState(false);

  return (
    <>
      <nav
        className="md:hidden fixed inset-x-0 bottom-0 z-50 flex items-center border-t border-[#EFEAE2]/70 bg-white/80 backdrop-blur-xl"
        style={{ paddingBottom: 'env(safe-area-inset-bottom)' }}
        aria-label="Navegación principal"
      >
        {NAV_ITEMS.map((item) => {
          // Central "+" button
          if (item === null) {
            return (
              <div key="quick-add" className="flex flex-1 justify-center">
                <button
                  type="button"
                  onClick={() => setQuickOpen(true)}
                  aria-label="Acciones rápidas"
                  className={cn(
                    'flex h-14 w-14 -translate-y-4 items-center justify-center rounded-full',
                    'bg-finflow-dark text-finflow-cream',
                    'shadow-[0_4px_20px_rgba(44,42,41,0.30)]',
                    'transition-transform active:scale-95'
                  )}
                >
                  <Plus className="h-6 w-6" strokeWidth={2.5} />
                </button>
              </div>
            );
          }

          const isActive = item.path ? location.pathname === item.path : false;
          const Icon = item.icon;

          return (
            <button
              key={item.label}
              type="button"
              onClick={() => {
                if (item.path) navigate(item.path);
                else onOpenMenu();
              }}
              aria-label={item.label}
              aria-current={isActive ? 'page' : undefined}
              className={cn(
                'flex flex-1 flex-col items-center gap-1 py-3 transition-colors',
                isActive ? 'text-finflow-dark' : 'text-finflow-muted'
              )}
            >
              <Icon className={cn('h-5 w-5', isActive ? 'stroke-[2.5]' : 'stroke-2')} />
              <span className={cn('text-[10px]', isActive ? 'font-semibold' : 'font-medium')}>
                {item.label}
              </span>
            </button>
          );
        })}
      </nav>

      <QuickActionSheet open={quickOpen} onClose={() => setQuickOpen(false)} />
    </>
  );
}
