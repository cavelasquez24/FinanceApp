# FinFlow — Contexto de Desarrollo

## Visión del Producto

FinFlow es una herramienta de **análisis financiero profesional + estilo de vida** diseñada para dar control total sobre el patrimonio personal. No es una app de gastos: es un sistema de inteligencia financiera que conecta ingresos, egresos, deudas, inversiones, metas de ahorro y fondo de emergencia en un modelo patrimonial coherente.

**Repositorio:** https://github.com/cavelasquez24/FinanceApp

---

## Tech Stack

| Capa | Tecnología |
|---|---|
| Backend | .NET 10, C#, Clean Architecture |
| ORM | Entity Framework Core 10 |
| Base de datos | PostgreSQL 16 |
| Frontend | React + TypeScript + Vite |
| Estilos | Tailwind CSS |
| Estado | TanStack Query (React Query) |
| Auth | JWT + Refresh Tokens |

**Estructura del proyecto:**
```
backend/
  src/
    FinanceApp.Domain/        # Entidades, enums, interfaces de dominio
    FinanceApp.Application/   # Servicios, DTOs, interfaces de repositorio
    FinanceApp.Infrastructure/ # EF Core, repositorios, migraciones
    FinanceApp.API/           # Controllers, Program.cs
  tests/
    FinanceApp.UnitTests/
frontend/
  src/
    features/                 # Módulos por dominio (savings, expenses, etc.)
    components/shared/        # Componentes reutilizables
    api/                      # Clientes HTTP por dominio
    types/                    # Tipos TypeScript
    pages/                    # Páginas principales
```

---

## Status del Proyecto

### Tareas completadas (1–7)

| # | Tarea | Estado |
|---|---|---|
| 1 | Autenticación (JWT + Refresh) | ✅ |
| 2 | Módulo de Gastos + Categorías + Tags | ✅ |
| 3 | Módulo de Ingresos | ✅ |
| 4 | Módulo de Deudas | ✅ |
| 5 | Módulo de Inversiones | ✅ |
| 6 | Cuentas Financieras + Ledger (AccountTransactions) | ✅ |
| 7 | Metas de Ahorro + Fondo de Emergencia + Restauraciones | ✅ |

### Tareas pendientes

| # | Tarea | Prioridad |
|---|---|---|
| 8 | Conciliación de cuentas | Alta |
| 9 | Dashboard financiero avanzado / reportes | Media |

---

## Roles Simultáneos

En cada sesión se asumen tres roles concurrentes:

### 1. Arquitecto de Software
- Mantener Clean Architecture (Domain → Application → Infrastructure → API)
- Garantizar que cada módulo siga el mismo patrón: Entidad → Config EF → Repositorio → Servicio → DTO → Controller
- Decisiones de diseño guiadas por ADRs implícitos (convenciones snake_case en BD, enum-as-string, soft delete con `DeletedAt`)
- Revisión de migraciones antes de aplicar

### 2. Full-Stack Developer
- Backend: servicios sin lógica en controllers, repositorios sin lógica de negocio
- Frontend: componentes pequeños y reutilizables, hooks por dominio, tipos TypeScript estrictos
- Idempotencia en operaciones críticas (transferencias, aportes, retiros)
- `SyncMovementAsync` como método central para mover el ledger

### 3. Asesor Financiero
- Todo cambio debe ser coherente con el modelo patrimonial
- Principios no negociables (ver sección siguiente)

---

## Principios Financieros No Negociables

1. **Integridad patrimonial** — Todo movimiento debe ser neutral en patrimonio o justificado. Una transferencia entre cuentas no crea ni destruye riqueza.

2. **Atomicidad** — Operaciones que afectan múltiples entidades (cuenta + meta + ledger) se ejecutan en una sola transacción de BD.

3. **Trazabilidad completa** — Cada cambio de saldo tiene su `AccountTransaction` con `SourceType` y `SourceId`. Sin movimientos huérfanos.

4. **Saldo esperado vs saldo real** — El sistema siempre puede recalcular el saldo esperado desde el ledger. La diferencia con el saldo real genera un ajuste explícito (no silencioso).

5. **Soft delete** — Nada se borra físicamente. `DeletedAt` marca el borrado lógico.

6. **Idempotencia** — Operaciones críticas aceptan `idempotencyKey` para evitar duplicados ante reintentos.

7. **Fondo de emergencia protegido** — `MinimumProtectedAmount` en `SavingsGoal` con `Purpose = EmergencyFund` garantiza que no se puede retirar por debajo del mínimo sin crear una `EmergencyFundRestoration`.

---

## Convenciones de Código

### Backend
- Tablas en `snake_case`, columnas en `snake_case` (excepto `SavingsGoals` que usa PascalCase por herencia)
- Enums como string en BD (`HasConversion<string>()`)
- Repositorios no hacen `.SaveChanges()` — lo hace el servicio o UnitOfWork
- Excepciones de dominio: `DomainException(code, message)` — el código se devuelve al frontend

### Frontend
- Un hook por dominio (`useSavings`, `useEmergencyFundRestorations`)
- Modales con `SavingsModalShell` como contenedor estándar
- Tipos en `*.types.ts`, clientes HTTP en `*.api.ts`
- Fechas como `string` (`YYYY-MM-DD`) en la capa de transporte

---

## Comandos frecuentes

```bash
# Crear migración
dotnet ef migrations add <Nombre> --project backend/src/FinanceApp.Infrastructure --startup-project backend/src/FinanceApp.API

# Aplicar migraciones
dotnet ef database update --project backend/src/FinanceApp.Infrastructure --startup-project backend/src/FinanceApp.API

# Build frontend
cd frontend && npm run build

# Tests
dotnet test backend/tests/FinanceApp.UnitTests
```
