// src/components/shared/Sidebar.tsx
import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  TrendingUp,
  TrendingDown,
  Target,
  PiggyBank,
  BarChart3,
  User,
  X,
  Wallet,
  LogOut,
  TagsIcon,
  BoneFracture,
} from 'lucide-react';
import { cn } from '../../utils/cn';

interface SidebarProps {
  isOpen: boolean;
  onClose: () => void;
  /** Optional — renders a "Cerrar sesión" action in the footer when provided */
  onLogout?: () => void;
}

const navItems = [
  { to: '/dashboard',   label: 'Dashboard',   icon: LayoutDashboard },
  { to: '/incomes',     label: 'Ingresos',    icon: TrendingUp      },
  { to: '/expenses',    label: 'Gastos',      icon: TrendingDown    },
  { to: '/analysis',    label: 'An\u00e1lisis', icon: BarChart3     },
  { to: '/accounts',    label: 'Cuentas',     icon: Wallet          },
  { to: '/budget',      label: 'Presupuesto', icon: Target          },
  { to: '/investments', label: 'Inversiones', icon: BarChart3       },
  { to: '/debts',     label: 'Deudas',       icon: BoneFracture       },
  { to: '/savings',     label: 'Metas',       icon: PiggyBank       },
  { to: '/categories',  label: 'Categorías',  icon: TagsIcon            },
  { to: '/profile',     label: 'Perfil',      icon: User            },
];

export function Sidebar({ isOpen, onClose, onLogout }: SidebarProps) {
  return (
    <>
      {/* Overlay móvil */}
      {isOpen && (
        <div
          className="fixed inset-0 z-20 bg-[#2C2A29]/40 backdrop-blur-sm lg:hidden"
          onClick={onClose}
        />
      )}

      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-30 lg:static',
          'flex w-64 flex-col border-r border-[#EFEAE2]/70 bg-white/70 backdrop-blur-xl',
          'transition-transform duration-300',
          'lg:translate-x-0',
          isOpen ? 'translate-x-0' : '-translate-x-full'
        )}
      >
        {/* Logo */}
        <div className="flex items-center justify-between border-b border-[#EFEAE2]/70 p-6">
          <div className="flex items-center gap-2.5">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-[#2C2A29]">
              <Wallet className="h-4 w-4 text-[#FBF9F4]" strokeWidth={2} />
            </div>
            <span className="text-lg font-semibold tracking-tight text-[#2C2A29]">
              FinanceApp
            </span>
          </div>
          <button
            onClick={onClose}
            className="rounded-lg p-1 text-[#7C756E] transition-colors hover:bg-[#F3F1EC] hover:text-[#2C2A29] lg:hidden"
            aria-label="Cerrar menú"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Navegación */}
        <nav className="flex-1 space-y-1 p-4">
          {navItems.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              onClick={onClose}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-[#5C7A99]/10 text-[#5C7A99]'
                    : 'text-[#7C756E] hover:bg-[#F3F1EC] hover:text-[#2C2A29]'
                )
              }
            >
              <Icon className="h-5 w-5 shrink-0" strokeWidth={2} />
              {label}
            </NavLink>
          ))}
        </nav>

        {/* Footer */}
        <div className="border-t border-[#EFEAE2]/70 p-4">
          {onLogout && (
            <button
              onClick={onLogout}
              className="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-[#7C756E] transition-colors hover:bg-[#C97B63]/10 hover:text-[#C97B63]"
            >
              <LogOut className="h-5 w-5 shrink-0" strokeWidth={2} />
              Cerrar sesión
            </button>
          )}
          <p className="mt-2 text-center text-xs text-[#7C756E]/60">FinanceApp v1.0</p>
        </div>
      </aside>
    </>
  );
}