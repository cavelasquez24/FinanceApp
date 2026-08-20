# Tarea 8 — Conciliación de Cuentas

## Objetivo

Transformar la edición silenciosa de saldo en un **flujo explícito de conciliación** que garantice trazabilidad completa y coherencia patrimonial.

---

## Problema Actual

El usuario puede editar `CurrentBalance` de una cuenta desde el PUT de edición (`FinancialAccountService.UpdateAsync`). Cuando el saldo cambia, se crea un `AccountTransaction { SourceType = "account-adjustment" }` automáticamente, pero:

- **No hay contexto**: el usuario no explica por qué difiere
- **No hay aprobación**: el ajuste se aplica sin confirmación explícita
- **No hay historial estructurado**: solo existe una transacción en el ledger, no una entidad de conciliación
- **Viola trazabilidad**: el Principio #3 (trazabilidad) y #4 (saldo esperado vs real) no se cumplen formalmente
- **El propio sistema lo señala**: `SavingsGoalService` ya emite el mensaje *"Concilia la cuenta antes de aportar"*, evidenciando que el concepto existe pero no tiene flujo

---

## Arquitectura

### Nueva entidad: `AccountReconciliation`

```csharp
public class AccountReconciliation : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public DateOnly ReconciliationDate { get; set; }
    public decimal ExpectedBalance { get; set; }        // calculado por el sistema desde el ledger
    public decimal ActualBalance { get; set; }          // declarado por el usuario (saldo real del banco)
    public decimal Difference { get; set; }             // ActualBalance - ExpectedBalance
    public Guid? AdjustmentTransactionId { get; set; } // FK → AccountTransaction generado (si Difference ≠ 0)
    public string? Notes { get; set; }
    public ReconciliationStatus Status { get; set; }

    // Navegación
    public User User { get; set; } = null!;
    public FinancialAccount Account { get; set; } = null!;
    public AccountTransaction? AdjustmentTransaction { get; set; }
}
```

### Nuevo enum: `ReconciliationStatus`

```csharp
public enum ReconciliationStatus
{
    Reconciled,  // diferencia = 0 o ajuste aplicado
    Skipped      // usuario decidió omitir
}
```

### Cálculo del saldo esperado

```
ExpectedBalance = SUM(AccountTransaction.Amount WHERE AccountId = id AND DeletedAt IS NULL)
```

El sistema ya tiene `OpeningBalance` registrado como `AccountTransaction { SourceType = "account-opening" }`, por lo que la suma del ledger es suficiente.

---

## Flujo

```
1. Usuario abre modal de conciliación para una cuenta
        ↓
2. GET /api/accounts/{id}/reconciliation/preview
   → { expectedBalance, suggestedActualBalance, lastReconciliation? }
        ↓
3. Usuario ingresa el saldo real (lo que ve en el banco)
        ↓
4. POST /api/accounts/{id}/reconciliation
   Body: { actualBalance, reconciliationDate, notes? }
   →  Calcula difference = actualBalance - expectedBalance
   →  Si difference ≠ 0:
        Crea AccountTransaction { SourceType = "account-adjustment", Amount = difference }
        Actualiza FinancialAccount.CurrentBalance = actualBalance
   →  Crea AccountReconciliation { Status = Reconciled, AdjustmentTransactionId = ... }
        ↓
5. Respuesta: { reconciliation, adjustmentTransaction?, newBalance }
```

---

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/accounts/{id}/reconciliation/preview` | Saldo esperado vs último conocido |
| `POST` | `/api/accounts/{id}/reconciliation` | Aplica conciliación |
| `GET` | `/api/accounts/{id}/reconciliation/history` | Historial paginado de conciliaciones |

---

## Archivos a crear / modificar

### Backend

| Archivo | Acción |
|---|---|
| `Domain/Entities/AccountReconciliation.cs` | Nueva entidad |
| `Domain/Enums/ReconciliationStatus.cs` | Nuevo enum |
| `Domain/Interfaces/Repositories/IAccountReconciliationRepository.cs` | Interface |
| `Application/DTOs/Account/ReconciliationPreviewDto.cs` | DTO preview |
| `Application/DTOs/Account/ReconciliationCreateDto.cs` | DTO request |
| `Application/DTOs/Account/ReconciliationResponseDto.cs` | DTO response |
| `Application/Services/AccountReconciliationService.cs` | Lógica: preview + apply |
| `Application/Interfaces/IAccountReconciliationService.cs` | Interface |
| `Infrastructure/Persistence/Configurations/AccountReconciliationConfiguration.cs` | Config EF |
| `Infrastructure/Persistence/Repositories/AccountReconciliationRepository.cs` | Repositorio |
| `API/Controllers/AccountReconciliationsController.cs` | Controller REST |
| Nueva migración EF | Tabla `account_reconciliations` |
| `FinancialAccountService.UpdateAsync` | Proteger edición directa de balance (o eliminarla) |

### Frontend

| Archivo | Acción |
|---|---|
| `features/accounts/components/ReconciliationModal.tsx` | Modal principal |
| `features/accounts/hooks/useReconciliation.ts` | Hook TanStack Query |
| `api/accounts.api.ts` | Métodos `previewReconciliation`, `applyReconciliation`, `getHistory` |
| `types/accounts.types.ts` | Tipos `ReconciliationPreview`, `ReconciliationResult` |

---

## DTOs clave

```typescript
// Preview
interface ReconciliationPreview {
  accountId: string;
  accountName: string;
  expectedBalance: number;       // calculado desde ledger
  currentBalance: number;        // FinancialAccount.CurrentBalance actual
  lastReconciliationDate?: string;
  lastReconciliationBalance?: number;
}

// Request
interface ReconciliationCreateDto {
  actualBalance: number;
  reconciliationDate: string;    // YYYY-MM-DD
  notes?: string;
}

// Response
interface ReconciliationResult {
  reconciliationId: string;
  difference: number;
  adjustmentCreated: boolean;
  newBalance: number;
  status: "Reconciled" | "Skipped";
}
```

---

## Decisiones Pendientes

| Decisión | Opción A | Opción B | Recomendación |
|---|---|---|---|
| ¿Se bloquea edición directa de balance? | Sí, solo vía conciliación | No, coexisten | **A** (más limpio, consistente) |
| ¿Conciliación automática en diferencia = 0? | Sí, sin ajuste | Requiere confirmación igual | **A** (UX más fluida) |
| ¿Permite conciliación negativa (saldo < esperado)? | Sí | No | **Sí** (robo, error, comisión) |
| ¿Alertas por diferencias grandes (> X%)? | Sí | No | **Futura iteración** |

---

## Testing

### Unit Tests
- `AccountReconciliationService`: calcular `ExpectedBalance` desde ledger, crear ajuste si diferencia ≠ 0, no crear ajuste si diferencia = 0
- Validaciones: cuenta inexistente, cuenta de otro usuario, `actualBalance` negativo inválido

### Integration Tests
- Flujo completo: preview → apply → verificar `AccountTransaction` generado → verificar `FinancialAccount.CurrentBalance` actualizado
- Idempotencia: misma conciliación dos veces el mismo día

### Financial Validation
- Después de conciliar, `SUM(AccountTransaction.Amount) == FinancialAccount.CurrentBalance`
- El ajuste aparece en `FinancialPositionDto.AccountAdjustments`
- `SavingsGoalService` no bloquea aportes tras conciliar correctamente

---

## Timeline estimado

| Fase | Horas |
|---|---|
| Entidad + config EF + migración | 1 h |
| Repositorio + servicio backend | 2 h |
| Controller + DTOs | 1 h |
| Frontend: modal + hook + API client | 2–3 h |
| Tests unitarios + integración | 2 h |
| QA manual + ajustes | 1 h |
| **Total** | **8–10 h** |
