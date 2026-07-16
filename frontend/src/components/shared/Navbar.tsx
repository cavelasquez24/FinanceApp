import { Menu, Bell } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';

interface NavbarProps {
  onMenuClick: () => void;
}

export function Navbar({ onMenuClick }: NavbarProps) {
  const { user, logout } = useAuth();

  return (
    <header className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-4 md:px-6">
      {/* Botón menú móvil */}
      <button
        onClick={onMenuClick}
        className="lg:hidden p-2 rounded-md hover:bg-gray-100"
      >
        <Menu className="w-5 h-5" />
      </button>

      {/* Espacio flexible */}
      <div className="flex-1" />

      {/* Acciones del usuario */}
      <div className="flex items-center gap-3">
        <button className="p-2 rounded-md hover:bg-gray-100 relative">
          <Bell className="w-5 h-5 text-gray-600" />
        </button>

        {/* Avatar y nombre */}
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded-full bg-primary-500 flex items-center justify-center">
            <span className="text-white text-sm font-semibold">
              {user?.firstName?.[0]}{user?.lastName?.[0]}
            </span>
          </div>
          <div className="hidden md:block">
            <p className="text-sm font-medium text-gray-900">
              {user?.firstName} {user?.lastName}
            </p>
          </div>
        </div>

        {/* Logout */}
        <button
          onClick={logout}
          className="text-sm text-gray-500 hover:text-gray-700 px-3 py-1.5 rounded-md hover:bg-gray-100"
        >
          Salir
        </button>
      </div>
    </header>
  );
}