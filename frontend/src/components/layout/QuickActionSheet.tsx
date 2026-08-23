import { useNavigate } from 'react-router-dom';
import { TrendingDown, TrendingUp, LayoutDashboard } from 'lucide-react';
import { BottomSheet } from '../ui/BottomSheet';

interface QuickActionSheetProps {
  open: boolean;
  onClose: () => void;
}

const ACTIONS = [
  {
    label: 'Registrar gasto',
    icon: TrendingDown,
    path: '/expenses',
    state: { openForm: true },
    accentColor: '#C97B63',
  },
  {
    label: 'Registrar ingreso',
    icon: TrendingUp,
    path: '/incomes',
    state: { openForm: true },
    accentColor: '#8FA888',
  },
  {
    label: 'Ver resumen',
    icon: LayoutDashboard,
    path: '/dashboard',
    state: undefined,
    accentColor: '#5C7A99',
  },
] as const;

export function QuickActionSheet({ open, onClose }: QuickActionSheetProps) {
  const navigate = useNavigate();

  const handleAction = (path: string, state?: object) => {
    onClose();
    navigate(path, state ? { state } : undefined);
  };

  return (
    <BottomSheet open={open} onClose={onClose} title="Acción rápida">
      <div className="flex flex-col gap-3 pb-4 pt-1">
        {ACTIONS.map(({ label, icon: Icon, path, state, accentColor }) => (
          <button
            key={label}
            type="button"
            onClick={() => handleAction(path, state)}
            className="flex w-full items-center gap-4 rounded-[20px] bg-white/70 p-4 text-left shadow-sm transition-all active:scale-[0.98] hover:bg-[#F3F1EC]"
          >
            <span
              className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl"
              style={{ backgroundColor: `${accentColor}18`, color: accentColor }}
            >
              <Icon className="h-6 w-6" />
            </span>
            <span className="text-base font-medium text-finflow-dark">{label}</span>
          </button>
        ))}
      </div>
    </BottomSheet>
  );
}
