import { useState, useEffect } from 'react';
import {
  useProfile,
  useUpdateProfile,
  useChangeCurrency,
  useChangePassword,
} from '../features/profile/hooks/useProfile';
import { Button, Card, CardHeader, Input, Spinner } from '../components/ui';

const selectClassName =
  'w-full max-w-xs rounded-xl border border-[#EFEAE2] bg-white/70 px-3 py-2.5 text-sm text-[#2C2A29] backdrop-blur-sm transition-colors focus:border-[#5C7A99] focus:outline-none focus:ring-2 focus:ring-[#5C7A99]/20';

export default function ProfilePage() {
  const { data: profile, isLoading } = useProfile();

  // State for Update Profile
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const updateProfileMutation = useUpdateProfile();

  // State for Currency
  const [currencyCode, setCurrencyCode] = useState('');
  const currencyMutation = useChangeCurrency();

  // State for Password
  const [passwords, setPasswords] = useState({ current: '', new: '', confirm: '' });
  const passwordMutation = useChangePassword();

  useEffect(() => {
    if (profile) {
      setFirstName(profile.firstName);
      setLastName(profile.lastName);
      setCurrencyCode(profile.currencyCode);
    }
  }, [profile]);

  if (isLoading) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <div className="flex flex-col items-center gap-3 text-[#7C756E]">
          <Spinner />
          <span className="text-sm">Cargando perfil...</span>
        </div>
      </div>
    );
  }

  const handleUpdateProfile = (e: React.FormEvent) => {
    e.preventDefault();
    updateProfileMutation.mutate({ firstName, lastName });
  };

  const handleChangeCurrency = (e: React.FormEvent) => {
    e.preventDefault();
    currencyMutation.mutate({ currencyCode });
  };

  const handleChangePassword = (e: React.FormEvent) => {
    e.preventDefault();
    passwordMutation.mutate(
      {
        currentPassword: passwords.current,
        newPassword: passwords.new,
        confirmNewPassword: passwords.confirm,
      },
      {
        onSuccess: () => setPasswords({ current: '', new: '', confirm: '' }),
      }
    );
  };

  return (
    <div className="max-w-3xl space-y-6">
      <h1 className="font-serif text-2xl font-medium text-[#2C2A29]">Configuración de Perfil</h1>

      {/* Actualizar Datos Personales */}
      <Card>
        <CardHeader title="Datos Personales" />
        <form onSubmit={handleUpdateProfile} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Nombre"
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              required
            />
            <Input
              label="Apellido"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              required
            />
          </div>
          <Input label="Email" type="email" value={profile?.email} disabled />
          <Button type="submit" isLoading={updateProfileMutation.isPending}>
            Guardar Cambios
          </Button>
        </form>
      </Card>

      {/* Preferencias de Moneda */}
      <Card>
        <CardHeader title="Preferencias" />
        <form onSubmit={handleChangeCurrency} className="space-y-4">
          <div className="flex flex-col gap-1.5">
            <label className="text-sm font-medium text-[#2C2A29]">Moneda Principal</label>
            <select
              value={currencyCode}
              onChange={(e) => setCurrencyCode(e.target.value)}
              className={selectClassName}
            >
              <option value="USD">USD - Dólar Estadounidense</option>
              <option value="EUR">EUR - Euro</option>
              <option value="COP">COP - Peso Colombiano</option>
              <option value="MXN">MXN - Peso Mexicano</option>
              {/* Añadir más según el contrato */}
            </select>
          </div>
          <Button type="submit" variant="secondary" isLoading={currencyMutation.isPending}>
            Actualizar Moneda
          </Button>
        </form>
      </Card>

      {/* Cambiar Contraseña */}
      <Card>
        <CardHeader title="Seguridad" />
        <form onSubmit={handleChangePassword} className="max-w-md space-y-4">
          <Input
            label="Contraseña Actual"
            type="password"
            value={passwords.current}
            onChange={(e) => setPasswords({ ...passwords, current: e.target.value })}
            required
          />
          <Input
            label="Nueva Contraseña"
            type="password"
            value={passwords.new}
            onChange={(e) => setPasswords({ ...passwords, new: e.target.value })}
            required
          />
          <Input
            label="Confirmar Nueva Contraseña"
            type="password"
            value={passwords.confirm}
            onChange={(e) => setPasswords({ ...passwords, confirm: e.target.value })}
            required
          />
          <Button type="submit" variant="danger" isLoading={passwordMutation.isPending}>
            Cambiar Contraseña
          </Button>
        </form>
      </Card>
    </div>
  );
}