import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link } from 'react-router-dom';
import { toast } from 'react-hot-toast';
import { Wallet } from 'lucide-react';
import { registerSchema, type RegisterFormData } from '../features/auth/schemas/register.schema';
import { Button, Input, Card } from '../components/ui';
import { useAuth } from '../context/AuthContext';

export function RegisterPage() {
  const { register: registerUser } = useAuth();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  });

  const onSubmit = async (data: RegisterFormData) => {
    try {
      await registerUser(data);
      toast.success('¡Cuenta creada exitosamente!');
    } catch (error: any) {
      const errorMessage = error.response?.data?.error?.message || 'Error al crear la cuenta';
      toast.error(errorMessage);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-[#FBF9F4] px-4 py-10 sm:px-6 lg:px-8">
      <Card className="w-full max-w-md !rounded-[28px] border border-[#EFEAE2] bg-white/70 p-8 shadow-xl shadow-[#2C2A29]/5 backdrop-blur-md">
        <div className="mb-8 flex flex-col items-center text-center">
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-[#2C2A29]">
            <Wallet className="h-6 w-6 text-[#FBF9F4]" strokeWidth={2} />
          </div>
          <h1 className="font-serif text-2xl font-medium text-[#2C2A29]">
            Crear Cuenta
          </h1>
          <p className="mt-2 text-sm text-[#7C756E]">
            Comienza a tomar el control de tus finanzas hoy
          </p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Input
              label="Nombre"
              placeholder="Juan"
              error={errors.firstName?.message}
              {...register('firstName')}
            />
            <Input
              label="Apellido"
              placeholder="Pérez"
              error={errors.lastName?.message}
              {...register('lastName')}
            />
          </div>

          <Input
            label="Correo Electrónico"
            type="email"
            placeholder="ejemplo@correo.com"
            error={errors.email?.message}
            {...register('email')}
          />

          <Input
            label="Contraseña"
            type="password"
            placeholder="••••••••"
            error={errors.password?.message}
            {...register('password')}
          />

          <Input
            label="Confirmar Contraseña"
            type="password"
            placeholder="••••••••"
            error={errors.confirmPassword?.message}
            {...register('confirmPassword')}
          />

          <Button type="submit" className="mt-6 w-full" isLoading={isSubmitting}>
            Registrarse
          </Button>
        </form>

        <div className="mt-6 border-t border-[#EFEAE2] pt-6">
          <p className="text-center text-sm text-[#7C756E]">
            ¿Ya tienes una cuenta?{' '}
            <Link
              to="/login"
              className="font-medium text-[#5C7A99] transition-colors hover:text-[#4D6884]"
            >
              Inicia sesión aquí
            </Link>
          </p>
        </div>
      </Card>
    </div>
  );
}