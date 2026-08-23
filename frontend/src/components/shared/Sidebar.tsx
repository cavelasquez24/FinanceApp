// src/components/shared/Sidebar.tsx
import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  TrendingUp,
  TrendingDown,
  Target,
  PiggyBank,
  LineChart,
  BrainCircuit,
  User,
  X,
  Skull,
  Wallet,
  LogOut,
  Folder,
  Tags,
  BoneFracture,
  CreditCard,
  Undo2,
} from 'lucide-react';
import { cn } from '../../utils/cn';

interface SidebarProps {
  isOpen: boolean;
  onClose: () => void;
  onLogout?: () => void;
}

const navGroups = [
  {
    label: 'Resumen',
    items: [
      { to: '/',           label: 'Inicio',       icon: LayoutDashboard, end: true },
      { to: '/diagnostico', label: 'Diagnóstico',  icon: BrainCircuit,   end: false },
    ],
  },
  {
    label: 'Movimientos',
    items: [
      { to: '/incomes',        label: 'Ingresos',    icon: TrendingUp,  end: false },
      { to: '/expenses',       label: 'Gastos',      icon: TrendingDown, end: false },
      { to: '/reimbursements', label: 'Reembolsos',  icon: Undo2,       end: false },
    ],
  },
  {
    label: 'Patrimonio',
    items: [
      { to: '/accounts',     label: 'Cuentas',     icon: Wallet,      end: false },
      { to: '/investments',  label: 'Inversiones', icon: LineChart,   end: false },
      { to: '/savings',      label: 'Metas',       icon: PiggyBank,   end: false },
      { to: '/debts',        label: 'Deudas',      icon: BoneFracture, end: false },
      { to: '/credit-cards', label: 'Tarjetas',    icon: CreditCard,  end: false },
    ],
  },
  {
    label: 'Configuración',
    items: [
      { to: '/budget',      label: 'Presupuesto', icon: Target,  end: false },
      { to: '/categories',  label: 'Categorías',  icon: Folder,  end: false },
      { to: '/tags',        label: 'Etiquetas',   icon: Tags,    end: false },
      { to: '/profile',     label: 'Perfil',      icon: User,    end: false },
    ],
  },
];

export function Sidebar({ isOpen, onClose, onLogout }: SidebarProps) {
  return (
    <>
      {/* Overlay móvil */}
      {isOpen && (
        <div
          className="fixed inset-0 z-20 bg-finflow-dark/40 backdrop-blur-sm lg:hidden"
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
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-finflow-dark">
              <Skull className="h-4 w-4 text-finflow-cream" strokeWidth={2} aria-hidden="true" />
            </div>
            <span className="text-lg font-semibold tracking-tight text-finflow-dark">FinFlow</span>
          </div>
          <button
            onClick={onClose}
            className="rounded-lg p-1 text-finflow-muted transition-colors hover:bg-[#F3F1EC] hover:text-finflow-dark lg:hidden"
            aria-label="Cerrar menú"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Navegación agrupada */}
        <nav className="flex-1 overflow-y-auto p-4">
          {navGroups.map((group, gi) => (
            <div key={group.label} className={cn('space-y-1', gi > 0 && 'mt-4')}>
              {gi > 0 && <hr className="mb-3 border-[#EFEAE2]" />}
              <p className="mb-1 px-3 text-[10px] font-semibold uppercase tracking-widest text-finflow-muted/60">
                {group.label}
              </p>
              {group.items.map(({ to, label, icon: Icon, end }) => (
                <NavLink
                  key={to}
                  to={to}
                  end={end}
                  onClick={onClose}
                  className={({ isActive }) =>
                    cn(
                      'flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors',
                      isActive
                        ? 'bg-finflow-blue/10 text-finflow-blue'
                        : 'text-finflow-muted hover:bg-[#F3F1EC] hover:text-finflow-dark'
                    )
                  }
                >
                  <Icon className="h-5 w-5 shrink-0" strokeWidth={2} />
                  {label}
                </NavLink>
              ))}
            </div>
          ))}
        </nav>

        {/* Footer */}
        <div className="border-t border-[#EFEAE2]/70 p-4">
          {onLogout && (
            <button
              onClick={onLogout}
              className="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-finflow-muted transition-colors hover:bg-finflow-rust/10 hover:text-finflow-rust"
            >
              <LogOut className="h-5 w-5 shrink-0" strokeWidth={2} />
              Cerrar sesión
            </button>
          )}
          <p className="mt-2 text-center text-xs text-finflow-muted/60">FinFlow v1.0</p>
        </div>
      </aside>
    </>
  );
}
