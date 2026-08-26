# Restructuración — Retiro y Reposición de Metas ✅
Completada: 25 de agosto de 2026

## Commits
| # | Capa | Hash |
|---|---|---|
| 1 | Domain | 1126e25 |
| 2 | Infrastructure | 6e28f71 |
| 3 | Application + migración | fb1d90c |
| 4 | API | 9396b67 |
| 5 | Tests | 2b6903f |
| 6 | Frontend | ba374a2 |

---

## 1. Contexto y problema original

**Síntoma reportado:** registrar una reposición obligaba a ejecutar un retiro nuevo.
El usuario que ya había sacado dinero de una meta no tenía forma de decir "voy a
devolver esto" sin volver a retirar.

**Hallazgo real:** el síntoma era la punta de un problema estructural. Existían
**tres mecanismos paralelos** modelando el mismo concepto — préstamo interno con
compromiso de devolución:

| Mecanismo | Origen | Estado |
|---|---|---|
| `EmergencyFundRestoration` | Tarea 7 | Funcional, correcto, atado al fondo de emergencia |
| `SavingsReplenishment` | Tarea 10 | Funcional, genérico para cualquier meta |
| `Debt.LinkedSavingsGoalId` | Anterior | **Roto en runtime** |

`Debt.LinkedSavingsGoalId` intentaba modelar el préstamo interno como deuda formal.
Sus dos ramas en `DebtService` (`AddPaymentAsync`, `AddWithdrawalAsync`) invocaban
`SavingsGoalService.DepositAsync` / `WithdrawAsync` sin `IdempotencyKey`, que ambos
servicios exigen (`IDEMPOTENCY_REQUIRED`, `INVALID_IDEMPOTENCY_KEY`). Toda deuda
ligada a una meta fallaba al primer pago o desembolso.

El síntoma reportado era consecuencia de que `SavingsReplenishment` solo se podía
crear desde el flujo de dos pasos de `WithdrawModal` — retiro `TemporaryLoan` primero,
`ReplenishmentCreateForm` después. Sin retiro nuevo, no había forma de llegar al
formulario.

---

## 2. Decisión de producto

**Los préstamos internos con compromiso de devolución quedan exclusivos del fondo de
emergencia** (`SavingsGoal.Purpose == EmergencyFund`).

Las metas de ahorro generales admiten exactamente tres salidas:

| Motivo | Efecto |
|---|---|
| `Consumed` | Se gasta en su propósito, con `Expense` vinculado |
| `ReallocatedToOtherGoal` | Se reasigna a otra meta |
| `ReallocatedToLiquid` | Se libera la asignación |
| `Correction` | Ajuste administrativo con motivo obligatorio |

**Por qué.** Una meta individual es un espacio *intocable salvo para su propósito
específico*. Es precisamente esa rigidez la que sostiene la disciplina de ahorro del
usuario: si "Vacaciones" se puede convertir en un préstamo con promesa de devolución,
deja de ser una meta y pasa a ser una cuenta corriente con etiqueta. El compromiso de
devolución existe para el fondo de emergencia porque ahí sí hay un mínimo protegido
que restaurar y un evento extraordinario que lo justifica — el fondo está diseñado
para ser usado y repuesto. Una meta general no.

El fondo de emergencia conserva su flujo íntegro: `EmergencyFundUseModal` →
`CreateUseAsync` → retiro + `EmergencyFundRestoration` en la misma transacción.

---

## 3. Veredicto: remoción, no fusión

La idea original era **fusionar** `SavingsReplenishment` dentro de
`EmergencyFundRestoration`. Se descartó al acotar el alcance con la decisión de
producto anterior:

- Si el préstamo interno solo aplica al fondo de emergencia, `EmergencyFundRestoration`
  **ya cubre el 100 % del caso de uso restante**. Fusionar habría significado migrar
  campos de un mecanismo que, tras la decisión, no tenía usuarios legítimos.
- La BD de desarrollo confirmó cero filas en `savings_replenishments` y cero valores no
  nulos en las tres columnas asociadas. No había nada que migrar.

También se abandonó la **reposición automática** (`ScheduledAmount`,
`AutoDebitEnabled`, `MonthlyDebitAmount`, `ExecuteCycleDebitsAsync`):

- **Nunca llegó a ejecutarse en producción.** El disparador no existía — el pendiente
  documentado en Tarea 10 era literalmente "Scheduler automático para
  `ExecuteCycleDebitsAsync` (hoy se dispara manualmente vía `POST /execute-cycle`)".
  El endpoint solo se invocaba a mano.
- **No encajaba con el patrón real de uso.** La reposición es manual y deliberada,
  dentro del flujo de resolver obligaciones primero: el usuario decide cuánto repone
  cuando ya sabe qué le queda tras cubrir lo comprometido. Un débito automático por
  ciclo compite con ese orden en vez de servirlo.

`EmergencyFundRestoration` conserva `ScheduledContributionAmount` y `NextScheduledDate`,
pero como **plan declarado**, no como ejecución automática: informan el compromiso del
ciclo y marcan la restauración como vencida (`IsOverdue`). El aporte siempre lo registra
el usuario vía `RegisterPaymentAsync`.

---

## 4. Los 6 commits

### Commit 1 — Domain (`1126e25`)
- Elimina `SavingsReplenishment.cs`, `ISavingsReplenishmentRepository.cs`,
  `ReplenishmentStatus.cs`, `DebitType.cs`.
- `Debt`: fuera `LinkedSavingsGoalId`.
- `SavingsGoal`: fuera la colección `Replenishments` y la propiedad calculada
  `TotalPendingReplenishment` — nunca fue funcional, el repositorio jamás incluía la
  colección, así que siempre devolvía 0.
- `SavingsGoalContribution`: fuera `SavingsReplenishmentId`, `DebitType?` y la
  navegación asociada.
- `User`: fuera la colección `SavingsReplenishments`.
- `SavingsWithdrawalReason.TemporaryLoan`: documentada la regla — solo válido cuando
  `Purpose == EmergencyFund`.

### Commit 2 — Infrastructure (`6e28f71`)
- `AppDbContext`: fuera `DbSet<SavingsReplenishment>`.
- Elimina `SavingsReplenishmentConfiguration` y `SavingsReplenishmentRepository`.
- `DependencyInjection`: fuera el registro del repositorio y del servicio.
- `DebtConfiguration`: fuera el mapeo e índice de `linked_savings_goal_id`. No existía
  FK real en BD, solo el índice parcial `idx_debts_linked_savings_goal_id`.
- `SavingsGoalContributionConfiguration`: fuera los mapeos de
  `savings_replenishment_id` y `debit_type`, la relación y su índice.

> La migración quedó pospuesta: `dotnet ef migrations add` compila el startup project
> (API → Application), roto por diseño hasta el Commit 3.

### Commit 3 — Application + migración (`fb1d90c`)
- Elimina `SavingsReplenishmentService` (346 líneas), `ISavingsReplenishmentService`,
  `SavingsReplenishmentDtos`.
- `DebtService`: fuera las 4 ramas de `LinkedSavingsGoalId` (ver sección 5). Cae también
  la dependencia `ISavingsGoalService`, ya muerta: `Debt` deja de conocer metas de ahorro.
- `DebtCreateDto` / `DebtUpdateDto` / `DebtResponseDto`: fuera `LinkedSavingsGoalId`.
- `SavingsGoalService.WithdrawAsync`: nueva validación **`LOAN_ONLY_FROM_EMERGENCY_FUND`**.
- `CurrentDashboardService`: `CycleReplenishmentCommitment` pasa a alimentarse de
  `EmergencyFundRestoration` vía el nuevo `GetScheduledCommitmentByCycleAsync`
  — suma `Min(ScheduledContributionAmount, OutstandingAmount)` de las restauraciones
  abiertas cuya cuota vence dentro del ciclo, incluidas las vencidas. Mismo nombre de
  campo en el DTO: el frontend no requirió cambios.
- Migración `20260826013331_RemoveSavingsReplenishmentAndDebtGoalLink`, aplicada en
  desarrollo sin errores:

```
DropForeignKey  FK_savings_goal_contributions_savings_replenishments_savings_r~
DropTable       savings_replenishments
DropIndex       idx_savings_goal_contributions_replenishment_id
DropIndex       idx_debts_linked_savings_goal_id
DropColumn      savings_goal_contributions.debit_type
DropColumn      savings_goal_contributions.savings_replenishment_id
DropColumn      debts.linked_savings_goal_id
```

- Añade `DesignTimeAppDbContextFactory` en Infrastructure para desacoplar el tooling de
  EF del proyecto API.

**Conteo previo al drop (BD de desarrollo):** 0 filas en `savings_replenishments`,
0 en `debts.linked_savings_goal_id`, 0 en `savings_goal_contributions.savings_replenishment_id`
y `.debit_type`. Sin pérdida de información, sin backup necesario.

### Commit 4 — API (`9396b67`)
- Elimina `SavingsReplenishmentsController.cs`. Con él caen 9 endpoints bajo
  `/api/v1/savings-replenishments`: CRUD, `manual-debit`, `pause`, `resume`,
  `execute-cycle`.
- `Program.cs` sin cambios: Swagger usa un único documento `v1` con descubrimiento por
  convención, sin grupos ni filtros explícitos.

**Smoke test.** Primera vez que el backend entero arranca desde el Commit 1:
`POST /api/v1/auth/login` con credenciales falsas → 401 (pipeline completo vivo hasta
Postgres); `/api/v1/dashboard/current`, `/api/v1/debts`, `/api/v1/savings-goals` → 401;
`/api/v1/savings-replenishments` → 404.

### Commit 5 — Tests (`2b6903f`)
- Elimina `SavingsReplenishmentServiceTests.cs` (11 casos).
- Añade `SavingsGoalTemporaryLoanTests`, `DebtServiceLedgerTests`,
  `EmergencyFundCommitmentQueryTests` (ver sección 8).

### Commit 6 — Frontend (`ba374a2`)
- Elimina 6 archivos: `ReplenishmentCreateForm.tsx`, `ReplenishmentPanel.tsx`,
  `ReplenishmentDebitHistory.tsx`, `useSavingsReplenishments.ts`,
  `savingsReplenishment.api.ts`, `savingsReplenishment.types.ts`.
- `WithdrawModal`: fuera la acción `"loan"` en seis puntos (ver sección 6).
- `SavingsGoalCard`: fuera el hook de reposiciones y el bloque que renderizaba
  `ReplenishmentPanel`. Los indicadores del fondo de emergencia
  (`pendingRestorationAmount`, restauraciones vencidas) quedan intactos — se alimentan
  de `EmergencyFundRestoration`.
- `CurrentDashboardPage` sin cambios.

---

## 5. Hallazgo destacado — movimientos huérfanos en `DebtService`

Las ramas de `LinkedSavingsGoalId` no solo estaban rotas por falta de
`IdempotencyKey`. Hacían algo peor:

```csharp
await _debtRepository.UpdateAsync(debt, cancellationToken);
if (!debt.LinkedSavingsGoalId.HasValue)          // ← la rama
    await _accountService.SyncMovementAsync(...);
```

Cuando la deuda estaba ligada a una meta, **el pago y el desembolso no dejaban rastro
en el ledger**. El saldo de la deuda cambiaba, pero no se creaba `AccountTransaction`.

Esto viola la invariante de trazabilidad — *todo cambio de saldo tiene su
`AccountTransaction` con `SourceType` y `SourceId`, sin movimientos huérfanos*.
En este repo la fuente versionada de esa regla es `CLAUDE.md`, "Principios Financieros
No Negociables" #3; se corresponde con la regla 10 de `accounting-rules.md`, documento
que **no está en el repositorio** (verificado: no existe ningún archivo con ese nombre).
Si esa referencia debe ser citable, hay que versionarla. El saldo esperado recalculado desde el ledger habría
divergido del saldo real de forma silenciosa, que es exactamente el escenario que
la conciliación de cuentas existe para detectar y que aquí se producía por diseño.

Corregido en el Commit 3: `SyncMovementAsync` queda **incondicional** en
`AddPaymentAsync` y `AddWithdrawalAsync`.

**No había ningún test de `DebtService`.** `DebtServiceLedgerTests` es cobertura nueva,
no solo de regresión: verifica que cada operación emite exactamente un movimiento con
`SourceType` (`debt-payment` / `debt-withdrawal`), importe con signo correcto, y
`SourceId` apuntando al `DebtPayment` o `DebtWithdrawal` persistido.

---

## 6. Hallazgo en UI — la opción estaba al revés

`WithdrawModal` ofrecía "Préstamo temporal a mí mismo" en el escenario equivocado, en
ambas direcciones:

- **Inalcanzable para el fondo de emergencia.** `SavingsGoalCard` envuelve el botón
  "Retirar" en `{!isEmergencyFund && (...)}`. El fondo muestra "Usar fondo" y abre
  `EmergencyFundUseModal`. `WithdrawModal` nunca se abre para el fondo.
- **Disponible en metas generales**, donde nunca debió estarlo.

Es decir: la única opción de préstamo estaba ofrecida exactamente donde no aplica y
ausente donde sí. Se eliminó **sin condicional por `purpose`** — no había nada que
preservar. El flujo de préstamo del fondo de emergencia vive intacto y sin duplicar en
`EmergencyFundUseModal` → `useCreateEmergencyFundUse`.

`WithdrawModal` es además el **único punto de entrada** al retiro
(`SavingsPage.tsx:237`), por lo que `LOAN_ONLY_FROM_EMERGENCY_FUND` es hoy inalcanzable
desde la UI y no requiere traducción a mensaje. La validación permanece como defensa
del dominio ante llamadas directas a la API.

---

## 7. Invariantes garantizadas

Verificadas contra el código actual de `EmergencyFundRestorationService`,
`EmergencyFundRestoration` y `EmergencyFundRestorationConfiguration`:

| # | Invariante | Garantía |
|---|---|---|
| I1 | El compromiso referencia un retiro real de la misma meta | FK `SourceWithdrawalId` con `OnDelete: Restrict` (`EmergencyFundRestorationConfiguration.cs:41-42`); `CreateUseAsync` la puebla con el retiro que acaba de crear |
| I2 | El monto comprometido es inmutable y derivado del retiro | `OriginalAmount` y `withdrawal.Amount` se asignan del mismo `dto.FundedAmount`; no hay campo editable independiente |
| I3 | Un retiro tiene **como máximo un** compromiso | `HasIndex(r => r.SourceWithdrawalId).IsUnique()` — a nivel BD |
| I4 | `0 ≤ RestoredAmount ≤ OriginalAmount`; al igualar ⇒ `Completed` | `EmergencyFundRestoration.ApplyPayment` rechaza `amount <= 0 \|\| amount > OutstandingAmount` y cierra al llegar a cero |
| I5 | Todo aporte incrementa `SavingsGoal.CurrentAmount` en el mismo monto y transacción | `RegisterPaymentAsync`: `restoration.SavingsGoal.CurrentAmount += dto.Amount` dentro de `ExecuteInTransactionAsync` |
| I6 | Un aporte no puede dejar `CurrentAmount > TargetAmount` | `RESTORATION_EXCEEDS_GOAL` |
| I7 | El compromiso **nunca** aparece como pasivo en `FinancialPositionService` | El servicio no referencia restauraciones en absoluto |
| I8 | Con compromisos abiertos, un aporte libre se rechaza | `USE_RESTORATION_PAYMENT` (`SavingsGoalService.cs:224`) |
| I9 | Retiro y compromiso se crean en la misma transacción o ninguno | `CreateUseAsync` completo bajo `_unitOfWork.ExecuteInTransactionAsync` |
| I10 | El pendiente del ciclo se refleja como compromiso del disponible | `GetScheduledCommitmentByCycleAsync` → `CycleReplenishmentCommitment`, mostrado como compromiso separado sin restarse de `BudgetAvailable` |

Antes del refactor, I1, I2, I3, I6 y I9 estaban rotas o inexistentes para
`SavingsReplenishment`. Al quedar `EmergencyFundRestoration` como único mecanismo, las
diez se cumplen.

---

## 8. Tests

| | Total |
|---|---|
| Baseline real | **125** |
| Retirados (`SavingsReplenishmentServiceTests`) | −11 |
| Nuevos | +6 |
| **Final** | **120 — 0 fallos** |

> La cifra de 117 citada en `TAREA_10_REPOSICION_METAS_COMPLETADA.md` quedó desfasada:
> la Tarea 11 (Transferencias) añadió tests después. Baseline verificado con `git stash`.

**Nuevos:**

| Test | Cubre |
|---|---|
| `WithdrawAsync_TemporaryLoan_OnNonEmergencyGoal_Throws` | `LOAN_ONLY_FROM_EMERGENCY_FUND` + estado intacto: `CurrentAmount` igual, cero `SavingsGoalWithdrawals`, cero `AccountTransactions`, cero llamadas al ledger |
| `WithdrawAsync_TemporaryLoan_OnEmergencyFundGoal_Succeeds` | Caso positivo: saldo baja, retiro registrado con `DestinationAccountId`, una transferencia ahorro→efectivo |
| `AddPaymentAsync_AlwaysRecordsLedgerMovement` | Sección 5 |
| `AddWithdrawalAsync_AlwaysRecordsLedgerMovement` | Sección 5 |
| `GetScheduledCommitmentByCycleAsync_IncludesDueAndOverdue_ExcludesLaterCycles` | Filtro `NextScheduledDate <= cycleEnd`, cuotas vencidas incluidas |
| `GetScheduledCommitmentByCycleAsync_CapsByOutstanding_AndIgnoresClosed` | Tope por pendiente, restauraciones cerradas excluidas |

Se introdujo `RecordingAccountService`, un doble de `IFinancialAccountService` que
registra las llamadas en vez de descartarlas. Es lo que permite afirmar que el ledger
**no** se tocó en el caso negativo — con los stubs existentes eso era inobservable.

**Estado de build:** Domain, Application, Infrastructure y API en 0 errores.
Frontend: `tsc -b` y `vite build` limpios.

---

## 9. Pendientes fuera de alcance

Ninguno causado por este refactor. Documentados para no perderlos:

- **Colisión de `schemaId` en Swagger.** Existen dos clases `AccountTransferCreateDto`
  (`DTOs/Account/FinancialAccountDtos.cs:48` y `DTOs/Transfer/AccountTransferDtos.cs:3`),
  ambas de Tarea 11. Swashbuckle usa el nombre corto como `schemaId` y falla:
  `GET /swagger/v1/swagger.json` devuelve **500** y la UI de Swagger carga vacía.
  Fix conocido, una línea en `AddSwaggerGen`:
  ```csharp
  options.CustomSchemaIds(t => t.FullName!.Replace("+", "."));
  ```
  Alternativa: renombrar uno de los dos DTOs.

- **3 errores de ESLint preexistentes**, regla `react-hooks/set-state-in-effect`, en
  `CreateGoalModal.tsx:53`, `DepositModal.tsx:46` y `RestorationPaymentModal.tsx:55`.
  Los archivos tocados en este refactor (`WithdrawModal`, `SavingsGoalCard`) están
  limpios.

- **Verificación visual manual pendiente.** El refactor se validó estáticamente —
  el array `actions` de `WithdrawModal` tiene 5 entradas sin `"loan"`, y
  `SavingsGoalCard` impide que una meta con `purpose === "emergency_fund"` llegue a ese
  modal. Falta confirmar en navegador: abrir el retiro sobre una meta general y ver que
  "Préstamo temporal" no aparece, y abrir el fondo de emergencia y ver que su camino de
  préstamo sigue intacto.

- **`DesignTimeAppDbContextFactory`** (`Infrastructure/Persistence/`) se añadió fuera del
  alcance literal del Commit 3, para desacoplar el tooling de EF Core del proyecto API.
  Permite generar y aplicar migraciones aunque un controller esté en medio de una
  refactorización. Decisión revisable.
