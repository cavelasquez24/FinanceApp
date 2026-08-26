// src/components/shared/AppLayout.tsx
import { useState } from "react";
import { Outlet } from "react-router-dom";
import { Menu } from "lucide-react";
import { Sidebar } from "./Sidebar";
import { BottomNav } from "../layout/BottomNav";
import { useAuth } from "../../context/AuthContext";

export function AppLayout() {
  const { logout } = useAuth();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  return (
    <div className="flex h-screen-dynamic bg-finflow-cream">
      <Sidebar
        isOpen={isSidebarOpen}
        onClose={() => setIsSidebarOpen(false)}
        onLogout={logout}
      />

      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Barra superior — solo visible en móvil, para abrir el sidebar */}
        <header className="flex items-center gap-3 border-b border-[#EFEAE2]/70 bg-white/70 px-4 py-3 backdrop-blur-xl lg:hidden">
          <button
            onClick={() => setIsSidebarOpen(true)}
            className="rounded-lg p-2 text-finflow-muted transition-colors hover:bg-[#F3F1EC] hover:text-finflow-dark"
            aria-label="Abrir menú"
          >
            <Menu className="h-5 w-5" />
          </button>
          <span className="text-sm font-semibold text-finflow-dark">FinFlow</span>
        </header>

        {/*
          El padding inferior reserva el alto de la BottomNav (solo en móvil).
          Se le suma el safe-area inset para que el último elemento no quede
          bajo el indicador de inicio del iPhone. En escritorio manda md:pb-6.
        */}
        <main className="flex-1 overflow-y-auto p-6 pb-[calc(6rem_+_env(safe-area-inset-bottom))] md:pb-6 lg:p-8">
          <div className="mx-auto max-w-7xl">
            <Outlet />
          </div>
        </main>
      </div>

      <BottomNav onOpenMenu={() => setIsSidebarOpen(true)} />
    </div>
  );
}
