import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'react-hot-toast';
import { AuthProvider } from './context/AuthContext';
import { AppRouter } from './routes/AppRouter';
import './index.css';

// Cliente de React Query — configura el comportamiento del caché
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Reintenta 1 vez si falla
      retry: 1,
      // Considera los datos frescos por 5 minutos
      staleTime: 5 * 60 * 1000,
      // Refetch cuando el usuario vuelve a la pestaña
      refetchOnWindowFocus: true,
    },
  },
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <AppRouter />
        {/* Notificaciones toast globales */}
        <Toaster
          position="top-right"
          toastOptions={{
            duration: 4000,
            style: {
              background: '#1f2937',
              color: '#f9fafb',
              borderRadius: '8px',
            },
            success: {
              iconTheme: { primary: '#22c55e', secondary: '#f9fafb' },
            },
            error: {
              iconTheme: { primary: '#ef4444', secondary: '#f9fafb' },
            },
          }}
        />
      </AuthProvider>
    </QueryClientProvider>
  </StrictMode>
);