# Análisis crítico — Módulo gasto / retiro / reposición de metas

Fecha: 2026-08-24 · Rama: `master` @ `81a70d6`
Alcance revisado: Domain (5 entidades, 4 enums), Application (4 servicios, ~1.650 LOC),
Infrastructure (2 repos, 2 configs, 1 migración), API (3 controllers), Frontend (7 componentes,
3 hooks, 3 clientes HTTP), Tests (`SavingsReplenishmentServiceTests` 11 casos,
`EmergencyFundRestorationTests` 7, `EmergencyFundInternalRestorationTests` 1,
`SavingsGoalAllocationIntegrationTests` 3).

---

## 1. Hallazgos de código

### 1.1 Existen TRES mecanismos paralelos para el mismo concepto

El concepto es uno solo: *«saqué dinero de una meta propia y me comprometo a devolverlo»*.
Hoy está implementado tres veces, con reglas distintas:

| # | Mecanismo | Entidad | Servicio | Estado |
|---|---|---|---|---|
| A | Restauración de fondo de emergencia | `EmergencyFundRestoration.cs:10` | `EmergencyFundRestorationService.cs` (360 LOC) | Vivo, solo para `Purpose = EmergencyFund` |
| B | Reposición programada (Tarea 10) | `SavingsReplenishment.cs:12` | `SavingsReplenishmentService.cs` (346 LOC) | Vivo, para cualquier meta |
| C | «Deuda personal» ligada a meta | `Debt.LinkedSavingsGoalId` (`Debt.cs:20`) | `DebtService.cs:187`, `DebtService.cs:227` | **Muerto y roto** (ver 1.6) |

A y B modelan lo mismo con nombres, invariantes y flujos distintos:

| Aspecto | A — `EmergencyFundRestoration` | B — `SavingsReplenishment` |
|---|---|---|
| FK al retiro que lo origina | `SourceWithdrawalId` **obligatorio** (`EmergencyFundRestoration.cs:14`) | **No existe** |
| Monto tomado | `OriginalAmount`, fijado por el servicio | `AmountTaken`, lo escribe el usuario |
| Historial de abonos | `SavingsGoalContribution.EmergencyFundRestorationId` | `SavingsGoalContribution.SavingsReplenishmentId` |
| Bloquea aportes libres | Sí — `SavingsGoalService.cs:224` | No |
| Valida techo de la meta al abonar | Sí — `EmergencyFundRestorationService.cs:166` | **No** |
| Débito automático | No | Sí (`ExecuteCycleDebitsAsync`), **nunca invocado** |
| Crea el retiro atómicamente | Sí — `EmergencyFundRestorationService.cs:84-148` | **No** — dos llamadas HTTP separadas |

`TAREA_10_REPOSICION_METAS.md:22` declara explícitamente que B *reemplaza* el concepto de
deuda personal. El reemplazo se hizo, pero **no se retiró C ni se unificó con A**.

### 1.2 EL BUG REPORTADO: para registrar una reposición hay que ejecutar un retiro nuevo

`ReplenishmentCreateForm` — el único formulario que crea un plan de reposición — se renderiza
**exclusivamente** desde dentro de `WithdrawModal`, y solo después de que el retiro ya se
ejecutó contra el backend:

- `WithdrawModal.tsx:127-157` — `withdraw(...)` se dispara siempre; en `onSuccess`, si la acción
  era `"loan"`, en vez de cerrar el modal guarda `setLoanWithdrawal({...})`.
- `WithdrawModal.tsx:159-180` — recién entonces monta `<ReplenishmentCreateForm amountTaken={...}>`.
- Grep exhaustivo: `ReplenishmentCreateForm` se importa en **un solo sitio**
  (`WithdrawModal.tsx:12`); `useCreateSavingsReplenishment` se usa en **un solo sitio**
  (`ReplenishmentCreateForm.tsx:63`).

Consecuencia directa: si el dinero salió antes por cualquier otra vía —
`"transfer"` (`ReallocatedToLiquid`), un uso del fondo de emergencia, o un retiro de una sesión
anterior — **no existe ninguna ruta para registrar la reposición sobre ese retiro**. La única
puerta de entrada obliga a ejecutar `POST /savings-goals/{id}/withdrawals` otra vez, que sí
descuenta `goal.CurrentAmount -= amount` (`SavingsGoalService.cs:376`) y sí mueve el ledger
(`SavingsGoalService.cs:338-349`). Eso es literalmente «el sistema simula un nuevo retiro».

El backend no tiene la culpa: `POST /api/v1/savings-replenishments`
(`SavingsReplenishmentsController.cs:43`) acepta el plan de forma independiente y
`CreateAsync` **no mueve un solo peso** (`SavingsReplenishmentService.cs:36-72`, test
`SavingsReplenishmentServiceTests.cs:90`). **Es un defecto de superficie de UI + un vínculo de
dominio faltante**, no del motor contable.

### 1.3 El plan de reposición no está atado a ningún retiro

`SavingsReplenishment` no tiene FK al `SavingsGoalWithdrawal` que lo origina — compárese con
`EmergencyFundRestoration.cs:14`. La migración lo confirma:
`20260823231307_AddSavingsReplenishments.cs:29-72` crea `savings_replenishments` sin columna
`source_withdrawal_id`. Efectos:

- `SavingsReplenishmentService.CreateAsync` (`:40-53`) valida nombre, `AmountTaken > 0` y
  `MonthlyDebitAmount`, pero **nada contra un retiro real**. Puedo crear un plan de $10.000
  sin haber retirado nunca.
- `ReplenishmentCreateForm.tsx:116-130` — el campo «Monto tomado» viene pre-llenado con el
  retiro pero es **editable**; el usuario puede retirar $100 y declarar $500.
- Flujo no atómico: en `WithdrawModal.tsx:146-155` el retiro ya está confirmado; si el usuario
  cierra el modal o falla el `POST` del plan, queda un **retiro huérfano** — dinero fuera de la
  meta sin compromiso de devolución y **sin ninguna UI que lo muestre** (no existe vista de
  historial de retiros en `frontend/src/features/savings/`).

### 1.4 `TotalPendingReplenishment` es código muerto

`SavingsGoal.cs:36-39` define `TotalPendingReplenishment`. Pero:

- `SavingsGoalRepository.cs:17-21` (`GetByUserIdAsync`) y `:54-60`
  (`GetByIdWithHistoryAsync`) incluyen `Contributions`, `Withdrawals` y `Restorations` —
  **nunca `Replenishments`**. La colección siempre llega vacía ⇒ la propiedad siempre devuelve `0`.
- `MapToResponseDto` (`SavingsGoalService.cs:504-526`) expone `PendingRestorationAmount`,
  `OpenRestorationsCount` y `NextRestorationDate` — **no expone nada del plan de reposición**.
- Grep de `TotalPendingReplenishment` en todo el repo: solo la definición.

La tarjeta de meta muestra los planes vía una query separada
(`SavingsGoalCard.tsx:134-140` → `useSavingsReplenishmentsByGoal`), así que el síntoma no se ve;
pero cualquier regla de negocio que quisiera usar el pendiente desde `SavingsGoal` leería `0`.

### 1.5 Asimetrías de invariantes entre A y B

- **Aportes libres**: `DepositAsync` lanza `USE_RESTORATION_PAYMENT` si hay restauraciones
  abiertas (`SavingsGoalService.cs:224`) — te obliga a canalizar el aporte por la restauración.
  **No hay guardia equivalente para reposiciones abiertas** (y no podría funcionar, por 1.4).
  Resultado: en una meta con préstamo pendiente puedo aportar por fuera y el plan seguirá
  marcando el mismo pendiente para siempre.
- **Techo de la meta**: `DepositAsync` llama `EnsureCapacity` (`SavingsGoalService.cs:228`) y
  `RegisterPaymentAsync` valida `RESTORATION_EXCEEDS_GOAL` (`EmergencyFundRestorationService.cs:166`).
  `ApplyDebitAsync` (`SavingsReplenishmentService.cs:236-237`) **no valida nada**: un débito de
  reposición puede dejar `CurrentAmount > TargetAmount`.
- **Sobre-asignación de la cuenta**: `ValidateFundingAsync` verifica
  `SAVINGS_ACCOUNT_OVERALLOCATED` (`SavingsGoalService.cs:409-412`). Su clon
  `ValidateRestorationFundingAsync` (`EmergencyFundRestorationService.cs:249-260`)
  **omite esa verificación**. `ApplyDebitAsync` no valida ninguna de las dos.

### 1.6 El mecanismo C está roto en tiempo de ejecución

`DebtService.AddPaymentAsync:189-192` llama:

```csharp
new DepositDto { Amount = dto.PrincipalAmount, Notes = $"Auto: abono deuda '{debt.Name}'" }
```

`DepositDto.cs:9` — `IdempotencyKey` queda en `Guid.Empty` ⇒ `SavingsGoalService.cs:226` lanza
`IDEMPOTENCY_REQUIRED`. Y aunque pasara, `DepositDto.cs:7` deja `FundingMode = "account_transfer"`
con `SourceAccountId = null` ⇒ `SavingsGoalService.cs:427` lanza `SOURCE_ACCOUNT_REQUIRED`.

`DebtService.AddWithdrawalAsync:229-238` es idéntico: `SavingsGoalWithdrawalCreateDto` sin
`IdempotencyKey` ⇒ `SavingsGoalService.cs:273` lanza `INVALID_IDEMPOTENCY_KEY`.

**Toda deuda con `LinkedSavingsGoalId` es inoperable**: no admite pagos ni desembolsos.
Y nótese qué hace ese código: para «registrar el desembolso» **crea un retiro de la meta**
(`SavingsWithdrawalReason.ReallocatedToLiquid`). Es la misma confusión conceptual que el
usuario reporta, fosilizada en el módulo de deudas.

### 1.7 El «débito automático» nunca se ejecuta

`ExecuteCycleDebitsAsync` (`SavingsReplenishmentService.cs:74-122`) está implementado y probado
(4 tests). `savingsReplenishment.api.ts:80` expone `executeCycle()`. Pero:

- Grep de `executeCycle` en `frontend/src`: **solo la definición del cliente**. Ningún hook,
  ninguna página, ningún efecto lo llama.
- No hay `IHostedService`, `BackgroundService` ni cron en el backend que lo invoque.

El toggle «Activar débito automático por ciclo» (`ReplenishmentCreateForm.tsx:162-169`) promete
algo que el sistema no hace. La reposición es 100% manual vía «Abonar ahora»
(`ReplenishmentPanel.tsx:102-107`).

### 1.8 Duplicación estructural

- `EnsureLiquidAccountAsync`: idéntico en `SavingsGoalService.cs:473-485`,
  `EmergencyFundRestorationService.cs:295-307` y `SavingsReplenishmentService.cs:272-284`.
- Validación de funding: `ValidateFundingAsync` (`SavingsGoalService.cs:397-442`) vs
  `ValidateRestorationFundingAsync` (`EmergencyFundRestorationService.cs:241-273`) — casi
  iguales, con la divergencia de 1.5.
- Creación de retiro: `AddWithdrawalAsync` (`SavingsGoalService.cs:374-392`) vs el bloque
  inline de `EmergencyFundRestorationService.cs:81-126`, que re-implementa el retiro con su
  propio mapeo de `Reason` — el fondo de emergencia **nunca usa** `TemporaryLoan`
  (`EmergencyFundRestorationService.cs:90`), usa `ReallocatedToLiquid` o `Consumed`.
- Cálculo de ciclo: `GetCycleRange` duplicado en 4 servicios — ya reconocido con un `TODO`
  en `SavingsReplenishmentService.cs:322-323`.

### 1.9 Lo que SÍ está bien (no tocar)

- **Patrimonio**: `FinancialPositionService.cs:117-124` — `netWorth = activos − pasivos`, donde
  los activos son saldos reales de cuentas + inversiones. `SavingsGoalAllocations`
  (`:200`) se expone como **memo, no se suma**. Correcto: la asignación de meta es una etiqueta
  sobre saldo, no un activo. Ni `PendingAmount` ni `OutstandingAmount` figuran como pasivo.
  El principio «tomar de tu propia meta no reduce patrimonio» está bien implementado.
- **Warning de sobre-asignación**: `FinancialPositionService.cs:164-169`.
- **Atomicidad**: los tres servicios envuelven en `_unitOfWork.ExecuteInTransactionAsync`.
- **Trazabilidad**: todo movimiento pasa por `SyncTransferBetweenAccountsAsync` con
  `SourceType`/`SourceId` (`SavingsGoalService.cs:338`, `SavingsReplenishmentService.cs:263`,
  `EmergencyFundRestorationService.cs:114`).
- **Idempotencia por ciclo**: `SavingsReplenishmentService.cs:89-93`, probada en
  `SavingsReplenishmentServiceTests.cs:313`.
- **Neutralidad patrimonial de la reposición**: probada en
  `SavingsReplenishmentServiceTests.cs:283-309`.

### 1.10 Huecos de cobertura de tests

Los 11 tests de la Tarea 10 prueban el plan **aislado**. No existe ningún test que:

- ejecute `WithdrawAsync(TemporaryLoan)` — **cero tests para el retiro de préstamo temporal**;
- cubra la secuencia completa retiro → plan → débito y verifique neutralidad patrimonial
  extremo a extremo (el test `:283` arranca del plan, no del retiro);
- verifique que `AmountTaken` corresponde a un retiro real;
- verifique que un débito no rompe `TargetAmount`;
- cubra `DebtService` con `LinkedSavingsGoalId` (habría detectado 1.6 de inmediato).

---

## 2. Respuesta a las tres preguntas

### 2.1 ¿Está sobre-complicado?

**Sí, pero no donde parece.** El motor contable (retiro → ledger → asignación → aporte) es
correcto y está bien probado. Lo sobre-complicado es la **capa de compromiso**: tres máquinas
para un solo concepto (1.1), una de ellas rota (1.6), otra con débito automático que nunca se
ejecuta (1.7), y con invariantes divergentes entre sí (1.5).

Contraste concreto: registrar un uso del fondo de emergencia es **una** llamada atómica
(`POST /savings-goals/{id}/emergency-fund-uses` → retiro + gasto/transferencia + compromiso,
`EmergencyFundRestorationService.cs:42-148`). Registrar un préstamo de una meta general son
**dos** llamadas no atómicas desde el mismo modal, y la segunda es opcional. Misma operación
financiera, dos arquitecturas.

El indicador más duro de sobre-complicación no es el volumen de código sino la **divergencia**:
cada regla nueva (techo de meta, sobre-asignación, bloqueo de aportes libres) hay que
implementarla dos o tres veces, y de hecho se implementó de forma incompleta en cada sitio.

### 2.2 ¿Es correcto mantener un control de «deuda»?

**El control es correcto; la palabra «deuda» no.**

Por qué no es deuda: no hay acreedor externo, no hay pasivo exigible, y el patrimonio no cambia
(`TAREA_10_REPOSICION_METAS.md:24-30`). Tratarlo como `Debt` fue un error ya diagnosticado
(`TAREA_10_REPOSICION_METAS.md:16`) y el código lo confirma: si `LinkedSavingsGoalId` funcionara,
`FinancialPositionService` restaría el saldo de esa deuda del patrimonio — dinero propio contado
como pasivo, patrimonio subvaluado en el monto del préstamo.

Por qué el control sí es necesario: sin él, `goal.CurrentAmount` baja y **nada** recuerda que
debe volver a subir. La meta se degrada silenciosamente. El compromiso es un mecanismo de
**disciplina y visibilidad**, no de contabilidad patrimonial.

**Modelado correcto — una sola entidad**, unificando A y B:

```
SavingsRestitution : BaseEntity          // renombre de SavingsReplenishment
  UserId, SavingsGoalId
  SourceWithdrawalId   Guid   REQUERIDO  ← el vínculo que hoy falta
  SourceAccountId      Guid              // cuenta que financia la devolución
  Name, Notes
  AmountTaken          decimal REQUERIDO // == SourceWithdrawal.Amount, inmutable
  AmountRestored       decimal
  ScheduledAmount      decimal           // débito por ciclo
  NextScheduledDate    DateOnly?         // (de EmergencyFundRestoration)
  TargetDate           DateOnly?         // (de EmergencyFundRestoration)
  AutoDebitEnabled, IsPaused, Status, CompletedAt, LastDebitAt
  PendingAmount => Max(AmountTaken - AmountRestored, 0)
```

Invariantes que la entidad debe garantizar:

| # | Invariante | Dónde se rompe hoy |
|---|---|---|
| I1 | `SourceWithdrawalId` referencia un `SavingsGoalWithdrawal` existente, de la misma meta, con `Reason ∈ {TemporaryLoan, ReallocatedToLiquid, Consumed}` | No existe la FK (1.3) |
| I2 | `AmountTaken == SourceWithdrawal.Amount`, inmutable tras crear | `ReplenishmentCreateForm.tsx:116` lo deja editable |
| I3 | Un retiro tiene **como máximo un** compromiso abierto (índice único filtrado) | No existe |
| I4 | `0 ≤ AmountRestored ≤ AmountTaken`; al igualar ⇒ `Completed` | OK (`SavingsReplenishment.cs:41`, `:255-259`) |
| I5 | Todo abono incrementa `SavingsGoal.CurrentAmount` en el mismo monto y en la misma transacción | OK (`SavingsReplenishmentService.cs:236-261`) |
| I6 | Un abono no puede dejar `CurrentAmount > TargetAmount` | **Roto** (1.5) |
| I7 | El compromiso **nunca** aparece como pasivo en `FinancialPositionService` | OK |
| I8 | Con compromisos abiertos, un aporte libre a la meta se rechaza o se imputa al compromiso | Solo para el fondo de emergencia (`SavingsGoalService.cs:224`) |
| I9 | Retiro y compromiso se crean en la misma transacción o ninguno | **Roto** para metas generales (1.3) |
| I10 | El pendiente del ciclo se refleja como compromiso del disponible | OK (`CurrentDashboardService.cs:85-87`) |

Y **`Debt.LinkedSavingsGoalId` desaparece** (mecanismo C): es la modelación incorrecta, está
rota, y su reemplazo ya existe.

### 2.3 ¿Cómo deben comportarse los balances?

Tres magnitudes distintas — hoy el código las distingue bien, pero la UI las mezcla:

1. **Saldo real** — `FinancialAccount.CurrentBalance`. Único activo. Solo se mueve con
   `SyncMovementAsync` / `SyncTransferBetweenAccountsAsync`.
2. **Asignación** — `SavingsGoal.CurrentAmount`. Etiqueta sobre el saldo de la cuenta de ahorro.
   **No es un activo.**
3. **Patrimonio** — `Σ cuentas + inversiones − deudas − tarjetas`
   (`FinancialPositionService.cs:117-124`). No incluye asignaciones.

**Invariante estructural:** para toda cuenta de ahorro `S`:
`Σ CurrentAmount de metas respaldadas por S ≤ S.CurrentBalance`. La diferencia es *saldo sin
asignar*. Ya se valida en aportes (`SavingsGoalService.cs:409-419`) y se advierte en
`FinancialPositionService.cs:164-169`; falta validarlo en débitos de reposición.

Escenario base: Efectivo **$1.000**, Cuenta de ahorro **$600**, Meta asignada **$600**,
Patrimonio **$1.600**, sin asignar **$0**.

| Operación | Efectivo | Cta. ahorro | Asignado | Sin asignar | Patrimonio | Pendiente compromiso |
|---|---:|---:|---:|---:|---:|---:|
| **Base** | 1.000 | 600 | 600 | 0 | 1.600 | 0 |
| **Aportar** $100 · `account_transfer` | **900** | **700** | **700** | 0 | 1.600 | 0 |
| **Retirar** $100 · `TemporaryLoan` → efectivo | **1.000** | **600** | **600** | 0 | 1.600 | **100** |
| **Reponer** $40 (débito auto o manual) | **960** | **640** | **640** | 0 | 1.600 | **60** |
| **Reponer** $60 (cierra el plan) | **900** | **700** | **700** | 0 | 1.600 | **0** |

Reglas por operación:

**Aportar** — patrimonio invariable siempre.
`account_transfer`: efectivo −X, ahorro +X, asignado +X, sin-asignar =.
`existing_balance`: nada se mueve físicamente; asignado +X, sin-asignar −X. Requiere
sin-asignar ≥ X (`SavingsGoalService.cs:416-419`). Debe respetar `TargetAmount`
(`EnsureCapacity`, `SavingsGoalService.cs:228`).

**Retirar** — el efecto depende del motivo, y esto es lo que la UI debe hacer explícito:

| `Reason` | Saldo real | Asignado | Patrimonio | ¿Genera compromiso? |
|---|---|---|---|---|
| `TemporaryLoan` | ahorro −X, destino +X | −X | **=** | **Sí, obligatorio** |
| `ReallocatedToLiquid` (con destino) | ahorro −X, destino +X | −X | **=** | Opcional |
| `ReallocatedToLiquid` (liberar) | sin cambio | −X | **=** | No |
| `ReallocatedToOtherGoal` | sin cambio | origen −X, destino +X | **=** | No |
| `Consumed` | ahorro −X (+ `Expense`) | −X | **−X** | No (el dinero se gastó) |
| `Correction` | sin cambio | −X | **=** | No |

`Consumed` es la **única** operación de este módulo que mueve el patrimonio, porque es la única
que crea un `Expense` (`SavingsGoalService.cs:301-312`). Ese es el criterio para toda la UI:
**si crea un gasto, el patrimonio baja; si no, no.**

**Reponer** — inverso exacto del retiro `TemporaryLoan`, y por eso **nunca** debe crear un
`Expense` ni un `SavingsGoalWithdrawal`: cuenta origen −X, cuenta de ahorro +X, asignado +X,
patrimonio invariable, `PendingAmount −X`. Hoy el backend hace exactamente esto
(`SavingsReplenishmentService.cs:228-270`); es la **UI** la que fuerza a pasar por un retiro
para llegar al formulario.

**Caso especial — reponer un `Consumed`**: si el dinero se gastó, la reposición no «deshace» el
gasto. El patrimonio bajó en X con el gasto y **no vuelve a subir** con la reposición
(cuenta origen −X, ahorro +X). Lo que se restituye es la *asignación*, no el patrimonio.
Este es el caso del fondo de emergencia con `useMode = "expense"` y hoy funciona bien; conviene
que la UI lo diga con esas palabras.

---

## 3. Veredicto

> **Restructuración acotada.** No es un bug puntual, pero tampoco es un rediseño del modelo
> patrimonial. La abstracción «compromiso de reposición» es la correcta; lo que está mal es
> que está **triplicada**, **desvinculada** del retiro que la origina, y **enterrada** dentro
> del flujo de retiro en la UI.

Justificación:

1. **No es un bug puntual.** Un parche de UI (añadir un botón «Registrar reposición» en la
   tarjeta de meta) haría desaparecer el síntoma en una hora, pero dejaría intacto que
   `AmountTaken` no valida contra ningún retiro (1.3), que quedan retiros huérfanos invisibles
   (1.3), y que existe un tercer mecanismo roto (1.6). El síntoma reaparecería por otra vía.
2. **No es una restructuración del modelo contable.** `FinancialPositionService`,
   `SyncTransferBetweenAccountsAsync`, el ledger de `AccountTransaction` y la separación
   saldo/asignación/patrimonio están correctos y probados. **No se toca nada de eso.**
3. **El diagnóstico de raíz** es que la Tarea 10 introdujo B como reemplazo de C
   (`TAREA_10_REPOSICION_METAS.md:22`) pero: no eliminó C, no unificó con A, y omitió la FK
   `SourceWithdrawalId` que A sí tiene. `TAREA_10_REPOSICION_METAS.md:534-541` («Qué NO hace
   esta tarea») lo dejó explícitamente fuera de alcance. Esta es esa deuda técnica cobrándose.

El trabajo cabe en el patrón de 6 commits del proyecto, en dos fases. **La Fase 1 sola elimina
el síntoma reportado**; la Fase 2 elimina la causa.

---

## 4. Roadmap

### FASE 1 — Vincular retiro y compromiso (elimina el síntoma)

#### Commit 1 — Domain
- `SavingsReplenishment.cs`: añadir `SourceWithdrawalId` (`Guid`, requerido) y navegación
  `SourceWithdrawal`. Espejo exacto de `EmergencyFundRestoration.cs:14,30`.
- `SavingsGoalWithdrawal.cs`: añadir navegación inversa `Replenishment` (`SavingsReplenishment?`).
- `SavingsReplenishment.cs`: método de dominio `ApplyDebit(decimal, DateOnly)` que encapsule
  I4/I6 — hoy la lógica vive suelta en `SavingsReplenishmentService.cs:253-259`. Espejo de
  `EmergencyFundRestoration.ApplyPayment` (`:53-70`).
- `SavingsWithdrawalReason.cs`: documentar que `TemporaryLoan` **exige** compromiso.

#### Commit 2 — Infrastructure
- `SavingsReplenishmentConfiguration.cs`: mapear `source_withdrawal_id` (requerido,
  `OnDelete: Restrict`); **índice único filtrado** sobre `source_withdrawal_id`
  `WHERE deleted_at IS NULL AND status IN ('Active','Paused')` → invariante I3 a nivel BD.
- `SavingsGoalRepository.cs:17-21` y `:54-60`: añadir
  `.Include(s => s.Replenishments.Where(r => r.DeletedAt == null))` → desbloquea
  `TotalPendingReplenishment` (1.4).
- `ISavingsReplenishmentRepository` + impl: `GetOpenByWithdrawalIdAsync(Guid, Guid, ct)`.
- `ISavingsGoalRepository` + impl: `GetWithdrawalByIdAsync(Guid withdrawalId, Guid userId, ct)`
  y `GetLoanWithdrawalsWithoutPlanAsync(Guid userId, ct)` — para listar retiros huérfanos.
- Migración `LinkReplenishmentToWithdrawal`. **Backfill obligatorio**: para cada plan existente
  buscar el `SavingsGoalWithdrawal` de la misma meta con `Amount == AmountTaken` y fecha más
  cercana anterior a `created_at`; los que no casen quedan con `source_withdrawal_id` nulo →
  la columna se crea *nullable*, se ejecuta el backfill, y **solo entonces** se aplica
  `SET NOT NULL` en una segunda migración. Si el backfill no es 100%, se documenta y la columna
  queda nullable con validación en Application (ver Riesgos).

#### Commit 3 — Application
- `SavingsReplenishmentCreateDto`: reemplazar `AmountTaken` (input del usuario) por
  `SourceWithdrawalId`. `AmountTaken` pasa a ser **derivado** del retiro → cierra I2.
  Añadir `IdempotencyKey` (hoy `CreateAsync` no tiene ninguna).
- `SavingsReplenishmentService.CreateAsync` (`:36-72`): cargar el retiro, validar propiedad,
  meta coincidente, `Reason ∈ {TemporaryLoan, ReallocatedToLiquid, Consumed}`, que no tenga ya
  un plan abierto (I3), y `AmountTaken = withdrawal.Amount`.
- `SavingsReplenishmentService.ApplyDebitAsync` (`:228-270`): delegar en
  `plan.ApplyDebit(...)`; añadir validación de techo de meta (I6) y de sobre-asignación
  reutilizando la lógica de `SavingsGoalService.cs:405-419`.
- `SavingsGoalService.WithdrawAsync`: para `Reason = TemporaryLoan`, aceptar el plan en el mismo
  DTO (`ReplenishmentPlan` opcional anidado) y crearlo **dentro de la misma transacción**
  (`SavingsGoalService.cs:268` ya envuelve todo en `RunInTransactionAsync`) → cierra I9.
  Mantener el endpoint independiente para retiros preexistentes.
- `SavingsGoalService.DepositAsync` (`:224`): extender la guardia a compromisos abiertos —
  con `Replenishments` ya incluido, `goal.TotalPendingReplenishment > 0` ⇒ nuevo código
  `USE_REPLENISHMENT_DEBIT` → cierra I8.
- `MapToResponseDto` (`:504-526`): exponer `PendingReplenishmentAmount` y
  `OpenReplenishmentsCount`.
- Extraer `EnsureLiquidAccountAsync` a un helper compartido — elimina la triplicación de 1.8.

#### Commit 4 — API
- `SavingsReplenishmentsController.cs:43` — el `POST` ahora exige `sourceWithdrawalId`.
- **Nuevo** `GET /api/v1/savings-goals/{id}/withdrawals?withoutPlan=true` en
  `SavingsGoalsController` → devuelve los retiros elegibles para vincular un plan.
  **Este endpoint es el que elimina el bug reportado**: permite registrar una reposición sobre
  un retiro que ya ocurrió, sin ejecutar uno nuevo.
- `POST /savings-goals/{id}/withdrawals` acepta el plan anidado (retiro + compromiso atómicos).

#### Commit 5 — Tests
Añadir a `SavingsReplenishmentServiceTests` (hoy 11 casos):

| Test | Verifica |
|---|---|
| `CreateAsync_WithoutSourceWithdrawal_Throws` | I1 |
| `CreateAsync_AmountTakenIsDerivedFromWithdrawal_IgnoringDtoValue` | I2 |
| `CreateAsync_SecondPlanOnSameWithdrawal_Throws` | I3 |
| `ApplyDebit_ExceedingTargetAmount_Throws` | I6 |
| `ApplyDebit_ExceedingSavingsAccountBacking_Throws` | invariante estructural |
| `WithdrawThenReplenish_FullCycle_NeverChangesNetWorth` | ciclo completo desde el retiro |
| `WithdrawTemporaryLoan_WithInlinePlan_IsAtomic` | I9 |
| `WithdrawTemporaryLoan_WithoutPlan_LeavesWithdrawalDiscoverable` | no hay huérfanos ciegos |
| `DepositAsync_WithOpenReplenishment_ThrowsUseReplenishmentDebit` | I8 |

Modificar: todos los `NewPlan(...)` del fixture (`SavingsReplenishmentServiceTests.cs:46-71`)
necesitan un `SourceWithdrawalId`; `FakeSavingsGoalRepository` necesita almacenar `Withdrawals`.
Añadir a `SavingsGoalAllocationIntegrationTests` un caso de retiro `TemporaryLoan`
(hoy **cero** cobertura de ese `Reason`).

#### Commit 6 — Frontend
- **Nuevo `RegisterReplenishmentModal`**, accesible desde `SavingsGoalCard` cuando existan
  retiros sin plan: lista los retiros elegibles (fecha, monto, motivo), el usuario elige uno,
  y el formulario pre-llena `amountTaken` en **modo solo-lectura**.
  → *elimina el bug reportado*.
- `ReplenishmentCreateForm.tsx:116-130`: «Monto tomado» pasa a `readOnly`, alimentado por el
  retiro seleccionado.
- `WithdrawModal.tsx:127-180`: el flujo `"loan"` pasa a ser **una sola llamada** con el plan
  anidado; el paso 2 deja de ser un segundo `POST` opcional.
- `SavingsGoalCard.tsx`: badge «N retiro(s) sin plan de reposición» con CTA al nuevo modal.
- Copys de `WithdrawModal.tsx:40-70`: explicitar el efecto patrimonial de cada acción
  («no cambia tu patrimonio» / «reduce tu patrimonio») según la tabla de 2.3.

---

### FASE 2 — Unificación (elimina la causa)

#### Commit 7 — Retirar el mecanismo C
- Eliminar `Debt.LinkedSavingsGoalId` (`Debt.cs:20`), los tres DTOs
  (`DebtCreateDto.cs:16`, `DebtUpdateDto.cs:14`, `DebtResponseDto.cs:22`) y las cuatro ramas de
  `DebtService.cs:187-193,209-213,227-239,254-258`.
- Migración: las deudas con `linked_savings_goal_id` no nulo se migran a
  `SavingsReplenishment` **o** se marcan para revisión manual (son deudas hoy inoperables, 1.6).
- Tests nuevos: `DebtService` sin la rama de meta; el pago siempre genera `SyncMovementAsync`.

#### Commit 8 — Fusionar A dentro de B
- `EmergencyFundRestoration` se absorbe en `SavingsReplenishment` (renombrado a
  `SavingsRestitution`), que ya tiene todas las piezas salvo `NextScheduledDate`/`TargetDate`.
- `SavingsGoalContribution` (`:16` y `:25`) pasa de dos FKs a **una**:
  `SavingsRestitutionId`. Migración de datos: copiar `emergency_fund_restoration_id` a la
  columna unificada.
- `EmergencyFundRestorationService` se reduce a un decorador sobre el servicio unificado que
  añade la regla específica del fondo: `MinimumProtectedAmount` y compromiso **obligatorio**
  (`SavingsGoalService.cs:278`).
- El uso del fondo de emergencia pasa a usar `SavingsGoalService.WithdrawAsync` con
  `Reason = TemporaryLoan` o `Consumed` en vez de re-implementar el retiro
  (`EmergencyFundRestorationService.cs:81-126`).
- Consolidar `ValidateFundingAsync` / `ValidateRestorationFundingAsync` en un único
  `SavingsFundingValidator` (cierra la divergencia de 1.5).

#### Commit 9 — Activar el débito automático
- `IHostedService` diario que llame `ExecuteCycleDebitsAsync` por usuario, **o** invocarlo de
  forma perezosa al cargar el dashboard del ciclo (idempotente por diseño,
  `SavingsReplenishmentService.cs:89-93`).
- Superficie de fondos insuficientes: `ReplenishmentCycleResultDto.InsufficientFunds` ya se
  construye (`:102-108`) pero nadie lo consume.
- Consolidar `GetCycleRange` en un `ICycleCalculator` — cierra el `TODO` de
  `SavingsReplenishmentService.cs:322-323` y los 4 duplicados.

---

## 5. Riesgos y qué se rompe

### Riesgos de datos (los serios)

| # | Riesgo | Mitigación |
|---|---|---|
| R1 | **Backfill de `source_withdrawal_id`.** Los planes existentes no tienen retiro asociado; el emparejamiento por monto+fecha es heurístico y puede fallar o casar mal. | Migración en tres pasos: `ADD COLUMN NULL` → backfill + reporte de no-casados → `SET NOT NULL` **solo si** el reporte da 0. Si no da 0: dejar nullable, tratar los nulos como *legacy* en Application y bloquear la creación de nuevos planes sin FK. |
| R2 | **Fusión de `SavingsGoalContribution`.** Colapsar dos FKs en una es irreversible sin backup. | Conservar `emergency_fund_restoration_id` como columna obsoleta durante un ciclo; no borrarla en el mismo commit que crea la nueva. Backup previo obligatorio. |
| R3 | **Deudas con `LinkedSavingsGoalId` en producción.** Hoy son inoperables (1.6); al eliminar el campo se pierde la intención registrada. | Antes de la migración, `SELECT` de esas deudas y decisión caso por caso. Si el set está vacío, R3 desaparece. |

### Rupturas de contrato

- **API — breaking**: `POST /api/v1/savings-replenishments` cambia `amountTaken` por
  `sourceWithdrawalId`. Solo lo consume `ReplenishmentCreateForm.tsx:86-96` (frontend propio),
  así que el impacto real es nulo si ambos se despliegan juntos.
- **API — cambio de forma**: `SavingsGoalResponseDto` gana campos (aditivo, seguro).
- **Fase 2 — breaking mayor**: desaparecen `POST /savings-goals/{id}/emergency-fund-uses` y
  `POST /emergency-fund-restorations/{id}/payments`
  (`EmergencyFundRestorationsController.cs:29,38`), con sus 3 componentes de UI
  (`EmergencyFundUseModal`, `EmergencyFundRestorationsModal`, `RestorationPaymentModal`) y el
  hook `useEmergencyFundRestorations`.

### Tests que se rompen

- Los **11** de `SavingsReplenishmentServiceTests`: el fixture `NewPlan`
  (`:46-71`) y `FakeSavingsGoalRepository` requieren `SourceWithdrawalId` + almacenamiento de
  retiros. Ruptura de compilación, arreglo mecánico.
- En Fase 2, los **8** de `EmergencyFundRestorationTests` +
  `EmergencyFundInternalRestorationTests` se reescriben contra el servicio unificado.
- Los de `SavingsGoalAllocationIntegrationTests` y `SavingsGoalDeletionProtectionTests`
  siguen pasando (no tocan reposiciones), pero `DeleteAsync` con
  `HasOpenRestorations` (`SavingsGoalService.cs:183`) debe extenderse a compromisos abiertos —
  si no, se puede archivar una meta con un préstamo pendiente.

### Regresiones a vigilar

- **Guardia I8 demasiado agresiva.** Bloquear aportes libres cuando hay un compromiso abierto
  replica el comportamiento del fondo de emergencia (`SavingsGoalService.cs:224`), pero en una
  meta general puede resultar molesto: si tengo un préstamo de $100 abierto y quiero aportar
  $500 de ahorro nuevo, el sistema me obliga a saldar primero. **Recomendación**: en vez de
  rechazar, imputar automáticamente el aporte al compromiso hasta cubrirlo y el excedente como
  aporte libre. Es más trabajo pero evita una fricción que el usuario ya sufre en el fondo de
  emergencia.
- **Validación de techo (I6) sobre planes existentes.** Si hoy existe un plan cuyo
  `AmountTaken` supera el espacio libre de la meta (posible, porque nunca se validó), al añadir
  I6 sus débitos empezarán a fallar. Detectar y reportar antes de desplegar.
- **Índice único de I3 sobre datos sucios**: si el backfill de R1 asigna dos planes al mismo
  retiro, la creación del índice falla. Validar antes de crearlo.

### Lo que NO se rompe

`FinancialPositionService`, `AccountTransaction`, `SyncMovementAsync`,
`SyncTransferBetweenAccountsAsync`, `TransferService` (Tarea 11), `ExpenseService`,
`InvestmentService`, y el cálculo de patrimonio. Ninguna fase toca el motor contable —
solo la capa de compromiso que se apoya sobre él.
