import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link } from 'react-router-dom';
import { toast } from 'react-hot-toast';
import {
  Mail,
  Lock,
  Eye,
  EyeOff,
  Wallet,
  Skull,
  ArrowUpRight,
  ArrowDownRight,
  PiggyBank,
} from 'lucide-react';
import { loginSchema, type LoginFormData } from '../features/auth/schemas/login.schema';
import { Button, Input } from '../components/ui';
import { useAuth } from '../context/AuthContext';

/**
 * Chip de icono con tinte de color + fondo suave, pensado para
 * simular un tratamiento "duotone" ligero e integrarse dentro
 * de tarjetas de vidrio sin depender de assets extra.
 */
function IconTile({
  icon: Icon,
  tone,
}: {
  icon: typeof Wallet;
  tone: 'income' | 'expense' | 'savings';
}) {
  const toneStyles: Record<typeof tone, string> = {
    income: 'bg-finflow-blue/10 text-finflow-blue',
    expense: 'bg-finflow-rust/10 text-finflow-rust',
    savings: 'bg-finflow-green/10 text-finflow-green',
  };

  return (
    <span
      className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-xl ${toneStyles[tone]}`}
    >
      <Icon className="h-4.5 w-4.5" strokeWidth={2.25} />
    </span>
  );
}

/** Curva spline suave con relleno degradado — elemento de firma del panel derecho. */
function SplineSparkline() {
  return (
    <svg viewBox="0 0 240 80" className="h-16 w-full" preserveAspectRatio="none" aria-hidden="true">
      <defs>
        <linearGradient id="sparklineFill" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#5C7A99" stopOpacity="0.28" />
          <stop offset="100%" stopColor="#5C7A99" stopOpacity="0" />
        </linearGradient>
      </defs>
      <path
        d="M0,55 C20,50 30,30 50,32 C70,34 80,55 100,50 C120,45 130,15 150,18 C170,21 180,40 200,35 C215,31 225,20 240,15 L240,80 L0,80 Z"
        fill="url(#sparklineFill)"
      />
      <path
        d="M0,55 C20,50 30,30 50,32 C70,34 80,55 100,50 C120,45 130,15 150,18 C170,21 180,40 200,35 C215,31 225,20 240,15"
        fill="none"
        stroke="#5C7A99"
        strokeWidth="2.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

export function LoginPage() {
  const { login } = useAuth();
  const [showPassword, setShowPassword] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormData) => {
    try {
      await login(data);
      toast.success('¡Bienvenido de vuelta!');
    } catch (error: any) {
      const errorMessage = error.response?.data?.error?.message || 'Error al iniciar sesión';
      toast.error(errorMessage);
    }
  };

  return (
    <div className="flex min-h-dvh bg-finflow-cream">
      {/* Panel izquierdo — formulario */}
      <div className="flex w-full flex-col justify-center px-6 py-12 sm:px-12 lg:w-1/2 lg:px-20 xl:px-24">
        <div className="mx-auto w-full max-w-sm">
          <div className="mb-10 flex items-center gap-2.5">
            <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-finflow-dark">
              <Skull className="h-5 w-5 text-finflow-cream" strokeWidth={2} aria-hidden="true" />
            </div>
            <span className="text-xl font-semibold tracking-tight text-finflow-dark">
              FinFlow
            </span>
          </div>

          <h1 className="font-serif text-3xl font-medium leading-tight text-finflow-dark">
            Bienvenido de vuelta
          </h1>
          <p className="mt-2 text-sm text-finflow-muted">
            Ingresa a tu cuenta para gestionar tus finanzas
          </p>

          <form onSubmit={handleSubmit(onSubmit)} className="mt-8 space-y-5">
            <Input
              label="Correo Electrónico"
              type="email"
              placeholder="ejemplo@correo.com"
              leftIcon={<Mail className="h-4 w-4" />}
              error={errors.email?.message}
              {...register('email')}
            />

            <Input
              label="Contraseña"
              type={showPassword ? 'text' : 'password'}
              placeholder="••••••••"
              leftIcon={<Lock className="h-4 w-4" />}
              rightAction={
                <button
                  type="button"
                  tabIndex={-1}
                  onClick={() => setShowPassword((v) => !v)}
                  className="text-finflow-muted transition-colors hover:text-finflow-dark"
                  aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                >
                  {showPassword ? (
                    <EyeOff className="h-4 w-4" />
                  ) : (
                    <Eye className="h-4 w-4" />
                  )}
                </button>
              }
              error={errors.password?.message}
              {...register('password')}
            />

            <div className="flex items-center justify-between text-sm">
              <label className="flex items-center gap-2 text-finflow-muted">
                <input
                  type="checkbox"
                  className="h-4 w-4 rounded border-[#EFEAE2] accent-finflow-blue focus:ring-2 focus:ring-finflow-blue/30"
                />
                <span>Recordarme</span>
              </label>

              <span
                className="cursor-not-allowed text-finflow-muted/60"
                title="Próximamente"
              >
                ¿Olvidaste tu contraseña?
              </span>
            </div>

            <Button
              type="submit"
              className="w-full !bg-finflow-dark !text-finflow-cream hover:!bg-[#1F1E1D]"
              size="lg"
              isLoading={isSubmitting}
            >
              Iniciar Sesión
            </Button>
          </form>

          <p className="mt-8 text-center text-sm text-finflow-muted">
            ¿No tienes una cuenta?{' '}
            <Link to="/register" className="font-medium text-finflow-blue hover:text-[#48607A]">
              Regístrate aquí
            </Link>
          </p>
        </div>
      </div>

      {/* Panel derecho — bento hero, oculto en mobile/tablet */}
      <div className="relative hidden overflow-hidden bg-[#F5F1E8] lg:flex lg:w-1/2">
        {/* Blobs de color de marca, muy sutiles */}
        <div className="absolute -left-24 -top-24 h-80 w-80 rounded-full bg-finflow-blue/10 blur-3xl" />
        <div className="absolute bottom-0 right-0 h-96 w-96 rounded-full bg-finflow-rust/10 blur-3xl" />
        <div className="absolute left-1/3 top-2/3 h-64 w-64 rounded-full bg-finflow-green/10 blur-3xl" />

        <div className="relative z-10 flex w-full flex-col justify-center px-16 py-16">
          <h2 className="max-w-md font-serif text-3xl font-medium leading-tight text-finflow-dark">
            Toma el control de tus finanzas personales
          </h2>
          <p className="mt-3 max-w-sm text-sm text-finflow-muted">
            Ingresos, gastos, presupuestos e inversiones en un solo lugar.
          </p>

          {/* Composición bento */}
          <div className="mt-10 grid w-full max-w-md grid-cols-2 gap-4">
            {/* Tarjeta grande — patrimonio neto */}
            <div className="col-span-2 rounded-[28px] border border-[#EFEAE2]/70 bg-white/60 p-6 shadow-[0_8px_30px_rgba(44,42,41,0.06)] backdrop-blur-xl">
              <p className="text-xs font-medium text-finflow-muted">Patrimonio neto</p>
              <p className="mt-1 text-3xl font-semibold text-finflow-dark">$12,480.50</p>
              <div className="mt-3">
                <SplineSparkline />
              </div>
            </div>

            {/* Tarjeta pequeña — ingresos */}
            <div className="rounded-[24px] border border-[#EFEAE2]/70 bg-white/60 p-4 shadow-[0_8px_30px_rgba(44,42,41,0.05)] backdrop-blur-xl">
              <IconTile icon={ArrowUpRight} tone="income" />
              <p className="mt-3 text-xs text-finflow-muted">Ingresos</p>
              <p className="text-lg font-semibold text-finflow-dark">$5,000</p>
            </div>

            {/* Tarjeta pequeña — gastos */}
            <div className="rounded-[24px] border border-[#EFEAE2]/70 bg-white/60 p-4 shadow-[0_8px_30px_rgba(44,42,41,0.05)] backdrop-blur-xl">
              <IconTile icon={ArrowDownRight} tone="expense" />
              <p className="mt-3 text-xs text-finflow-muted">Gastos</p>
              <p className="text-lg font-semibold text-finflow-dark">$1,850</p>
            </div>

            {/* Tarjeta ancha — meta de ahorro */}
            <div className="col-span-2 rounded-[24px] border border-[#EFEAE2]/70 bg-white/60 p-4 shadow-[0_8px_30px_rgba(44,42,41,0.05)] backdrop-blur-xl">
              <div className="flex items-center gap-3">
                <IconTile icon={PiggyBank} tone="savings" />
                <div className="flex-1">
                  <div className="flex items-center justify-between text-xs">
                    <span className="text-finflow-muted">Meta de ahorro</span>
                    <span className="font-medium text-finflow-green">68%</span>
                  </div>
                  <div className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-[#EFEAE2]">
                    <div className="h-full w-[68%] rounded-full bg-finflow-green" />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}