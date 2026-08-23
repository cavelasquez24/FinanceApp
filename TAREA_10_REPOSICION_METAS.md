# TAREA 10 — Reposición Programada de Metas (`replenishment_pending`)

> **Fecha de diseño:** 23 de agosto de 2026  
> **Autor:** Claude — roles activos: Solution Architect + Full-Stack Developer + Financial Advisor  
> **Estado:** Diseño aprobado, pendiente de implementación  
> **Prioridad:** Alta — corrige un error contable activo (préstamo interno contado como pasivo externo)

---

## Contexto y problema que resuelve

### Situación actual (incorrecta)

Cuando el usuario toma dinero temporalmente de una meta (ej. fondo de emergencia), el flujo actual lo fuerza a crear una `Debt` con `LinkedSavingsGoalId`. Esto produce tres errores contables:

1. **El patrimonio baja artificialmente** — el sistema trata la reposición pendiente como un pasivo externo, igual que una deuda con el banco, cuando en realidad el dinero sigue siendo del usuario.
2. **El DTO ignora el vínculo** — `DebtCreateDto.LinkedSavingsGoalId` existe pero `DebtService.CreateAsync` no lo asigna, así que el vínculo nunca se guarda.
3. **El pago es completamente manual** — no hay incentivo ni automatismo para reponer; queda como un ítem olvidable en la lista de deudas.

### Solución aprobada

Reemplazar el concepto de "deuda personal" por **Reposición Programada** (`SavingsReplenishment`): un compromiso interno del usuario hacia su propia meta, con un **débito automático configurable por ciclo** que transfiere desde la cuenta operativa hacia la meta hasta saldar el pendiente.

### Invariante contable central

```
Tomar dinero de una meta propia → NO reduce patrimonio
Solo cambia la distribución: menos en la meta, más en la cuenta operativa
El patrimonio cambia únicamente si el dinero se CONSUME (gasto)
```

### Tabla de comportamiento esperado

| Momento | Cuenta operativa | Meta asignada | Patrimonio |
|---|---:|---:|---:|
| Estado inicial | $1.000 | $300 | $1.300 |
| Toma $100 de la meta | $1.100 | $200 | $1.300 ← sin cambio |
| Ciclo 1 — débito auto $40 | $1.060 | $240 | $1.300 ← sin cambio |
| Ciclo 2 — débito auto $40 | $1.020 | $280 | $1.300 ← sin cambio |
| Ciclo 3 — débito auto $20 | $1.000 | $300 | $1.300 ← sin cambio |

---

## Modelo de dominio

### Entidad nueva: `SavingsReplenishment`

```csharp
// backend/src/FinanceApp.Domain/Entities/SavingsReplenishment.cs

public class SavingsReplenishment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SavingsGoalId { get; set; }          // Meta que se está reponiendo
    public Guid SourceAccountId { get; set; }         // Cuenta operativa que financia el débito
    public string Name { get; set; } = string.Empty; // Ej: "Reposición — Emergencia marzo"
    public string? Notes { get; set; }

    // Montos
    public decimal AmountTaken { get; set; }          // Total tomado de la meta
    public decimal AmountReplenished { get; set; }    // Acumulado repuesto hasta hoy
    public decimal PendingAmount => AmountTaken - AmountReplenished; // Calculado

    // Programación automática
    public decimal MonthlyDebitAmount { get; set; }   // Monto a debitar cada ciclo
    public bool AutoDebitEnabled { get; set; } = true;
    public bool IsPaused { get; set; } = false;       // Pausa temporal sin cancelar

    // Ciclo objetivo (opcional — si se define, muestra ETA)
    public decimal? TargetAmount { get; set; }        // Límite/objetivo de la meta

    // Estado
    public ReplenishmentStatus Status { get; set; } = ReplenishmentStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastDebitAt { get; set; }        // Último débito automático ejecutado

    // Navegación
    public SavingsGoal SavingsGoal { get; set; } = null!;
    public FinancialAccount SourceAccount { get; set; } = null!;
    public ICollection<ReplenishmentDebit> Debits { get; set; } = new List<ReplenishmentDebit>();
}
```

### Enum: `ReplenishmentStatus`

```csharp
// backend/src/FinanceApp.Domain/Enums/ReplenishmentStatus.cs

public enum ReplenishmentStatus
{
    Active = 1,      // Pendiente, débitos corriendo
    Paused = 2,      // Pausado temporalmente (un ciclo)
    Completed = 3,   // AmountReplenished >= AmountTaken
    Cancelled = 4    // Cancelado manualmente por el usuario
}
```

### Entidad de historial: `ReplenishmentDebit`

```csharp
// backend/src/FinanceApp.Domain/Entities/ReplenishmentDebit.cs

public class ReplenishmentDebit
{
    public Guid Id { get; set; }
    public Guid ReplenishmentId { get; set; }
    public decimal Amount { get; set; }               // Monto real debitado ese ciclo
    public DateTime DebitDate { get; set; }           // Fecha efectiva del débito
    public DebitType Type { get; set; }               // Auto | Manual | Adjustment
    public string? Notes { get; set; }
    public Guid? AccountTransactionId { get; set; }   // Vínculo al movimiento de cuenta

    public SavingsReplenishment Replenishment { get; set; } = null!;
}

public enum DebitType
{
    Automatic = 1,   // Disparado por el sistema al inicio del ciclo
    Manual = 2,      // El usuario hizo un aporte adelantado
    Adjustment = 3   // Corrección del último débito del ciclo (cuando resta < mensual)
}
```

### Cambio en `SavingsGoal`

```csharp
// Agregar en SavingsGoal.cs
public ICollection<SavingsReplenishment> Replenishments { get; set; } = new List<SavingsReplenishment>();

// Propiedad calculada útil para el dashboard
public decimal TotalPendingReplenishment =>
    Replenishments
        .Where(r => r.Status == ReplenishmentStatus.Active || r.Status == ReplenishmentStatus.Paused)
        .Sum(r => r.PendingAmount);
```

---

## DTOs

```csharp
// SavingsReplenishmentCreateDto.cs
public class SavingsReplenishmentCreateDto
{
    public Guid SavingsGoalId { get; set; }
    public Guid SourceAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public decimal AmountTaken { get; set; }           // Cuánto tomó de la meta
    public decimal MonthlyDebitAmount { get; set; }    // Cuánto quiere reponer por ciclo
    public bool AutoDebitEnabled { get; set; } = true;
}

// SavingsReplenishmentDto.cs (respuesta)
public class SavingsReplenishmentDto
{
    public Guid Id { get; set; }
    public Guid SavingsGoalId { get; set; }
    public string SavingsGoalName { get; set; } = string.Empty;
    public Guid SourceAccountId { get; set; }
    public string SourceAccountName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public decimal AmountTaken { get; set; }
    public decimal AmountReplenished { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal MonthlyDebitAmount { get; set; }
    public decimal ProgressPercent { get; set; }       // AmountReplenished / AmountTaken * 100
    public int EstimatedCyclesRemaining { get; set; }  // PendingAmount / MonthlyDebitAmount (ceil)

    public bool AutoDebitEnabled { get; set; }
    public bool IsPaused { get; set; }
    public ReplenishmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastDebitAt { get; set; }
    public List<ReplenishmentDebitDto> Debits { get; set; } = new();
}

// SavingsReplenishmentDebitDto.cs
public class ReplenishmentDebitDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime DebitDate { get; set; }
    public DebitType Type { get; set; }
    public string? Notes { get; set; }
}

// SavingsReplenishmentPauseDto.cs
public class SavingsReplenishmentPauseDto
{
    public string? Reason { get; set; }  // Opcional: motivo de pausa
}

// SavingsReplenishmentManualDebitDto.cs
public class SavingsReplenishmentManualDebitDto
{
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
```

---

## Servicio: `SavingsReplenishmentService`

```csharp
// backend/src/FinanceApp.Application/Services/SavingsReplenishmentService.cs

public class SavingsReplenishmentService
{
    // ── Crear un plan de reposición ──────────────────────────────────────────
    // 1. Valida que la meta exista y pertenezca al usuario
    // 2. Valida que la cuenta origen exista y pertenezca al usuario
    // 3. AmountTaken > 0; MonthlyDebitAmount > 0; MonthlyDebitAmount <= AmountTaken
    // 4. NO reduce ninguna cuenta aquí — el usuario ya tiene el dinero en la cuenta operativa
    //    (la salida de la meta ocurrió al hacer el retiro con razón "Prestamo")
    // 5. Crea SavingsReplenishment con Status = Active
    // 6. NO crea Debt — este es el cambio fundamental

    public async Task<SavingsReplenishmentDto> CreateAsync(
        Guid userId,
        SavingsReplenishmentCreateDto dto);

    // ── Ejecutar débito automático (llamado al inicio de cada ciclo) ─────────
    // 1. Obtiene todos los replenishments Active, AutoDebitEnabled=true, IsPaused=false del usuario
    // 2. Para cada uno:
    //    a. Calcula monto real = Min(MonthlyDebitAmount, PendingAmount)
    //    b. Verifica que SourceAccount tenga saldo suficiente
    //       - Si no tiene: registra alerta, NO ejecuta, NO pausa automáticamente
    //    c. En transacción atómica:
    //       - Reduce SourceAccount.CurrentBalance en monto real
    //       - Aumenta SavingsGoal.CurrentAmount en monto real
    //       - Aumenta Replenishment.AmountReplenished en monto real
    //       - Crea AccountTransaction en SourceAccount (tipo: ReplenishmentDebit)
    //       - Crea ReplenishmentDebit con Type = Automatic
    //       - Si PendingAmount == 0: Status = Completed, CompletedAt = now
    //    d. Actualiza LastDebitAt
    // 3. Retorna resumen de ejecución (cuántos procesados, cuántos sin fondos)

    public async Task<ReplenishmentCycleResultDto> ExecuteCycleDebitsAsync(Guid userId);

    // ── Débito manual adelantado ─────────────────────────────────────────────
    // El usuario quiere abonar más de lo programado este ciclo
    // Misma lógica atómica que el automático, Type = Manual

    public async Task<SavingsReplenishmentDto> ApplyManualDebitAsync(
        Guid userId,
        Guid replenishmentId,
        SavingsReplenishmentManualDebitDto dto);

    // ── Pausar / reanudar ────────────────────────────────────────────────────
    // Pausa: IsPaused = true, Status = Paused (si estaba Active)
    // Reanudar: IsPaused = false, Status = Active (si estaba Paused)

    public async Task<SavingsReplenishmentDto> PauseAsync(
        Guid userId, Guid replenishmentId, SavingsReplenishmentPauseDto dto);

    public async Task<SavingsReplenishmentDto> ResumeAsync(
        Guid userId, Guid replenishmentId);

    // ── Cancelar ─────────────────────────────────────────────────────────────
    // Status = Cancelled. El pendiente queda visible pero no genera más débitos.
    // NO revierte movimientos ya realizados.

    public async Task CancelAsync(Guid userId, Guid replenishmentId);

    // ── Consultas ────────────────────────────────────────────────────────────
    public async Task<List<SavingsReplenishmentDto>> GetByUserIdAsync(Guid userId);
    public async Task<List<SavingsReplenishmentDto>> GetByGoalIdAsync(Guid userId, Guid goalId);
    public async Task<SavingsReplenishmentDto> GetByIdAsync(Guid userId, Guid replenishmentId);
}
```

---

## API Controller

```csharp
// backend/src/FinanceApp.API/Controllers/SavingsReplenishmentsController.cs

[ApiController]
[Route("api/savings-replenishments")]
[Authorize]
public class SavingsReplenishmentsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll();                          // GET todos del usuario

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id);

    [HttpGet("goal/{goalId}")]
    public async Task<IActionResult> GetByGoal(Guid goalId);           // Replenishments de una meta

    [HttpPost]
    public async Task<IActionResult> Create(SavingsReplenishmentCreateDto dto);

    [HttpPost("{id}/manual-debit")]
    public async Task<IActionResult> ManualDebit(
        Guid id, SavingsReplenishmentManualDebitDto dto);               // Aporte adelantado

    [HttpPatch("{id}/pause")]
    public async Task<IActionResult> Pause(
        Guid id, SavingsReplenishmentPauseDto dto);

    [HttpPatch("{id}/resume")]
    public async Task<IActionResult> Resume(Guid id);

    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(Guid id);                  // Soft cancel

    // Endpoint interno para el motor de ciclos (puede ser protegido o interno)
    [HttpPost("execute-cycle")]
    public async Task<IActionResult> ExecuteCycle();                    // Disparo manual o por scheduler
}
```

---

## Migración de base de datos

```sql
-- Tabla principal
CREATE TABLE "SavingsReplenishments" (
    "Id"                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"              UUID NOT NULL REFERENCES "AspNetUsers"("Id"),
    "SavingsGoalId"       UUID NOT NULL REFERENCES "SavingsGoals"("Id"),
    "SourceAccountId"     UUID NOT NULL REFERENCES "FinancialAccounts"("Id"),
    "Name"                VARCHAR(200) NOT NULL,
    "Notes"               TEXT,
    "AmountTaken"         NUMERIC(15,2) NOT NULL CHECK ("AmountTaken" > 0),
    "AmountReplenished"   NUMERIC(15,2) NOT NULL DEFAULT 0 CHECK ("AmountReplenished" >= 0),
    "MonthlyDebitAmount"  NUMERIC(15,2) NOT NULL CHECK ("MonthlyDebitAmount" > 0),
    "AutoDebitEnabled"    BOOLEAN NOT NULL DEFAULT TRUE,
    "IsPaused"            BOOLEAN NOT NULL DEFAULT FALSE,
    "TargetAmount"        NUMERIC(15,2),
    "Status"              INTEGER NOT NULL DEFAULT 1,
    "CreatedAt"           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CompletedAt"         TIMESTAMPTZ,
    "LastDebitAt"         TIMESTAMPTZ
);

-- Historial de débitos
CREATE TABLE "ReplenishmentDebits" (
    "Id"                     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ReplenishmentId"        UUID NOT NULL REFERENCES "SavingsReplenishments"("Id"),
    "Amount"                 NUMERIC(15,2) NOT NULL CHECK ("Amount" > 0),
    "DebitDate"              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "Type"                   INTEGER NOT NULL DEFAULT 1,
    "Notes"                  TEXT,
    "AccountTransactionId"   UUID REFERENCES "AccountTransactions"("Id")
);

-- Índices
CREATE INDEX "IX_SavingsReplenishments_UserId" ON "SavingsReplenishments"("UserId");
CREATE INDEX "IX_SavingsReplenishments_SavingsGoalId" ON "SavingsReplenishments"("SavingsGoalId");
CREATE INDEX "IX_SavingsReplenishments_Status" ON "SavingsReplenishments"("Status");
CREATE INDEX "IX_ReplenishmentDebits_ReplenishmentId" ON "ReplenishmentDebits"("ReplenishmentId");
```

---

## Cambio en `CurrentDashboardService` — disponible del ciclo

El dashboard debe mostrar el total de débitos automáticos programados para el ciclo actual como un **compromiso comprometido**, para que el usuario sepa cuánto de su disponible ya está reservado para reposiciones:

```csharp
// En CurrentDashboardService.cs — agregar al cálculo de disponible

var cycleReplenishmentCommitment = await _replenishmentRepository
    .GetActiveByUserIdAsync(userId)
    .Where(r => r.AutoDebitEnabled && !r.IsPaused)
    .SumAsync(r => Math.Min(r.MonthlyDebitAmount, r.PendingAmount));

// Este valor aparece en el dashboard como:
// "Comprometido en reposiciones este ciclo: $X"
// Y se resta del disponible libre
```

---

## Flujo completo de usuario (UX)

### Caso 1 — Crear una reposición

1. Usuario va a **Metas → [Meta específica] → Retiro**
2. Elige razón: "Préstamo temporal" (antes solo existía esto con deuda manual)
3. Al completar el retiro, aparece inmediatamente el panel: **"¿Cómo quieres reponer este dinero?"**
4. Formulario de reposición:
   - Nombre del plan (pre-llenado: "Reposición — [Nombre meta] [Mes]")
   - Monto tomado: `$100` (pre-llenado del retiro)
   - Cuenta origen del débito: desplegable de cuentas operativas
   - Débito por ciclo: `$40` (campo libre)
   - Auto-cálculo inmediato: **"Repones en 3 ciclos aprox."**
   - Toggle: Débito automático activado ✓
5. Confirmar → crea `SavingsReplenishment`

### Caso 2 — Vista del progreso

En la tarjeta de la meta aparece:

```
[Meta: Fondo Emergencia]
Asignado:  $200 / $300
─────────────────────────────
Reposición activa: $100 pendientes
Débito auto: $40/ciclo · ~3 ciclos restantes
[Pausar] [Abonar ahora] [Cancelar]
```

### Caso 3 — Inicio de ciclo (débito automático)

Al detectar el inicio del nuevo ciclo (mismo mecanismo que usa `BudgetService` para ciclos):
- El sistema ejecuta `ExecuteCycleDebitsAsync`
- Transfiere $40 de la cuenta operativa → meta
- El usuario ve en el feed de la meta: "Débito automático de reposición: $40 ✓"
- Si no hay fondos: notificación suave en el dashboard: "Sin fondos suficientes para débito de reposición de [Meta]"

### Caso 4 — Aporte adelantado

Usuario recibe un extra de dinero y quiere reponer más rápido:
- Botón **"Abonar ahora"** en el panel de reposición
- Ingresa monto libre (ej: $60 para cerrar de una vez)
- Crea `ReplenishmentDebit` con `Type = Manual`
- Si `AmountReplenished >= AmountTaken` → Status = Completed automáticamente

---

## Plan de commits (por capa arquitectónica)

### Commit 1 — Domain
```
feat(domain): SavingsReplenishment entity, ReplenishmentDebit, enums y migración

- Entities/SavingsReplenishment.cs
- Entities/ReplenishmentDebit.cs
- Enums/ReplenishmentStatus.cs
- Enums/DebitType.cs
- Migrations/[timestamp]_AddSavingsReplenishment.cs
```

### Commit 2 — Infrastructure
```
feat(infra): repositorio y configuración EF para SavingsReplenishment

- Repositories/SavingsReplenishmentRepository.cs (interface + impl)
- Persistence/Configurations/SavingsReplenishmentConfiguration.cs
- Persistence/Configurations/ReplenishmentDebitConfiguration.cs
- Registro en AppDbContext y DI
```

### Commit 3 — Application
```
feat(application): SavingsReplenishmentService con ciclo completo

- DTOs/SavingsReplenishment/*.cs (Create, Dto, ManualDebit, Pause, CycleResult)
- Services/SavingsReplenishmentService.cs
  - CreateAsync — plan sin afectar deuda
  - ExecuteCycleDebitsAsync — motor atómico
  - ApplyManualDebitAsync
  - PauseAsync / ResumeAsync / CancelAsync
  - GetBy* queries
- Actualiza CurrentDashboardService para incluir cycleReplenishmentCommitment
```

### Commit 4 — API
```
feat(api): SavingsReplenishmentsController con todos los endpoints

- Controllers/SavingsReplenishmentsController.cs
- Registro de rutas en Program.cs
```

### Commit 5 — Tests
```
test: SavingsReplenishment — invariantes contables y ciclo completo

- Tests/SavingsReplenishmentTests.cs

Casos cubiertos:
1. Crear plan → AmountReplenished = 0, Status = Active, patrimonio sin cambio
2. Débito automático ciclo 1 → AmountReplenished += 40, cuenta opera -= 40, meta += 40
3. Débito automático ciclo 2 → mismo comportamiento
4. Último débito (resto < mensual) → usa Min(monthly, pending), Status = Completed
5. Sin fondos → NO ejecuta débito, NO cambia estado, genera alerta
6. Aporte manual → adelanta progreso, completa si llega al total
7. Pausa → IsPaused = true, ciclo NO ejecuta débito
8. Reanudar → IsPaused = false, ciclo vuelve a ejecutar
9. Cancelar → Status = Cancelled, ciclo no lo procesa
10. Patrimonio = cuenta operativa + meta asignada, nunca doble conteo
11. Idempotencia: ExecuteCycleDebitsAsync dos veces el mismo ciclo no duplica
```

### Commit 6 — Frontend
```
feat(frontend): ReplenishmentPanel, formulario de creación y vista de progreso

- features/savings/components/ReplenishmentCreateForm.tsx
  - Formulario post-retiro: monto, cuenta, débito/ciclo
  - Auto-cálculo de ETA en tiempo real
- features/savings/components/ReplenishmentPanel.tsx
  - Progreso visual (barra + montos)
  - Acciones: Pausar / Reanudar / Abonar ahora / Cancelar
- features/savings/components/ReplenishmentDebitHistory.tsx
  - Lista de débitos con fecha, monto y tipo (Auto / Manual)
- Integrar panel en SavingsGoalDetailPage o en el card de la meta
- Actualizar CurrentDashboardPage para mostrar "Comprometido en reposiciones"
- Tipos TypeScript: SavingsReplenishmentDto, ReplenishmentDebitDto, etc.
- savingsReplenishmentApi.ts (endpoints CRUD + manual-debit + pause + resume)
```

---

## Invariantes que debe cumplir esta tarea

| Invariante | Verificación |
|---|---|
| Crear una reposición NO reduce ningún saldo | `SourceAccount.Balance` antes == después |
| El débito automático es atómico | Si falla el aporte a la meta, revierte la salida de la cuenta |
| `AmountReplenished` nunca supera `AmountTaken` | Validación en servicio + constraint DB |
| `PendingAmount` siempre = `AmountTaken - AmountReplenished` | Propiedad calculada, no persistida |
| El ciclo no ejecuta débitos si `IsPaused = true` | Test #7 |
| El ciclo no duplica si se llama dos veces | Test #11 — idempotencia por `LastDebitAt` del ciclo |
| Patrimonio no cambia al crear ni al ejecutar débitos | Test #10 |
| Una reposición cancelada nunca aparece como deuda | No existe `Debt` en este flujo |

---

## Qué NO hace esta tarea

- No modifica la entidad `Debt` ni `DebtService` — son deudas externas y permanecen intactas
- No elimina el `LinkedSavingsGoalId` de `Debt` en esta tarea (eso va en la limpieza de deuda externa)
- No implementa el scheduler automático de ciclos — el botón "Ejecutar ciclo" es suficiente para MVP; el scheduler se agrega después
- No modifica las metas existentes ni hace migración de datos de reposiciones previas

---

## Prompt de onboarding para la próxima sesión

Copiar al inicio de la sesión en Claude Code:

```
Contexto del proyecto FinFlow — Tarea 10: Reposición Programada de Metas

Stack: .NET 10, PostgreSQL 16, EF Core 10.0.4, React 18 + TypeScript + Vite

Objetivo de esta sesión: implementar SavingsReplenishment — un plan de reposición
con débito automático por ciclo para cuando el usuario toma dinero temporalmente
de una meta propia. Reemplaza el concepto incorrecto de "deuda personal" que reducía
patrimonio erróneamente.

Invariante central: tomar dinero de una meta propia NO reduce patrimonio.
Solo redistribuye entre cuenta operativa y meta asignada.

Diseño completo en: TAREA_10_REPOSICION_METAS.md

Orden de commits:
1. Domain: SavingsReplenishment, ReplenishmentDebit, enums, migración
2. Infrastructure: repositorio, configuraciones EF, DI
3. Application: SavingsReplenishmentService completo + update CurrentDashboardService
4. API: SavingsReplenishmentsController
5. Tests: 11 casos de invariantes contables
6. Frontend: ReplenishmentCreateForm, ReplenishmentPanel, DebitHistory, tipos TS, api client

Tests actuales: 106 passing — no deben romperse.
Modelo Sonnet recomendado para .NET.

Comenzar leyendo TAREA_10_REPOSICION_METAS.md y luego revisar:
- SavingsGoal.cs y SavingsGoalService.cs (base sobre la que se construye)
- CurrentDashboardService.cs (donde se agrega cycleReplenishmentCommitment)
- AccountTransactionConfiguration.cs (patrón de idempotencia a replicar)
```
