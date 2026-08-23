import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

// Páginas
import { LoginPage } from '../pages/LoginPage';
import { RegisterPage } from '../pages/RegisterPage';
import { DashboardPage } from '../pages/DashboardPage';
import { CurrentDashboardPage } from '../pages/CurrentDashboardPage';
import { AccountsPage } from '../pages/AccountsPage';
import { IncomesPage } from '../pages/IncomesPage';
import { ExpensesPage } from '../pages/ExpensesPage';
import { BudgetPage } from '../pages/BudgetPage';
import  InvestmentsPage  from '../pages/InvestmentsPage';
import  SavingsPage  from '../pages/SavingsPage';
import  ProfilePage  from '../pages/ProfilePage';
import  CategoriesPage  from '../pages/CategoriesPage';
import  DebtsPage  from '../pages/DebtsPage';
import TagsPage from '../pages/TagsPage';
import CreditCardsPage from '../pages/CreditCardsPage';
import ReimbursementsPage from '../pages/ReimbursementsPage';
import { AnalyticsPage } from '../pages/AnalyticsPage';

// Layout
import { AppLayout } from '../components/shared/AppLayout';

// Ruta protegida — redirige al login si no está autenticado
function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500" />
      </div>
    );
  }

  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
}

// Ruta pública — redirige al inicio si ya está autenticado
function PublicRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) return null;

  return isAuthenticated ? <Navigate to="/" replace /> : <>{children}</>;
}

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Rutas públicas */}
        <Route path="/login" element={
          <PublicRoute><LoginPage /></PublicRoute>
        } />
        <Route path="/register" element={
          <PublicRoute><RegisterPage /></PublicRoute>
        } />

        {/* Rutas protegidas — todas dentro del layout */}
        <Route path="/" element={
          <ProtectedRoute><AppLayout /></ProtectedRoute>
        }>
          {/* Raíz → resumen operativo del día */}
          <Route index element={<CurrentDashboardPage />} />

          {/* Análisis histórico */}
          <Route path="tendencias" element={<DashboardPage />} />

          {/* Diagnóstico / analytics unificado */}
          <Route path="diagnostico" element={<AnalyticsPage />} />

          {/* Redirects de rutas antiguas */}
          <Route path="dashboard" element={<Navigate to="/" replace />} />
          <Route path="analysis" element={<Navigate to="/tendencias" replace />} />
          <Route path="analytics" element={<Navigate to="/diagnostico" replace />} />

          {/* Movimientos */}
          <Route path="incomes" element={<IncomesPage />} />
          <Route path="expenses" element={<ExpensesPage />} />
          <Route path="reimbursements" element={<ReimbursementsPage />} />

          {/* Patrimonio */}
          <Route path="accounts" element={<AccountsPage />} />
          <Route path="investments" element={<InvestmentsPage />} />
          <Route path="savings" element={<SavingsPage />} />
          <Route path="debts" element={<DebtsPage />} />
          <Route path="credit-cards" element={<CreditCardsPage />} />

          {/* Configuración */}
          <Route path="budget" element={<BudgetPage />} />
          <Route path="categories" element={<CategoriesPage />} />
          <Route path="tags" element={<TagsPage />} />
          <Route path="profile" element={<ProfilePage />} />
        </Route>

        {/* Ruta por defecto */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
