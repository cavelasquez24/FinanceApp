# Tarea 9 — Metadata Analítica: Dashboard Financiero Avanzado

## Visión

Convertir FinFlow de un tracker transaccional en un motor de inteligencia financiera personal.
Tarea 9 agrega la capa analítica que convierte los datos ya existentes (Tareas 1–8) en patrones,
proyecciones e indicadores accionables. No introduce nuevos flujos de escritura: todo es lectura
y derivación sobre el ledger existente.

**Fases:**
- **Fase 11** — Backend analítico (snapshots, métricas, proyecciones)
- **Fase 12** — Frontend analítico (nueva página Analytics, gráficos, score)

---

## Estado del arte (después de Tarea 8)

| Entidad / Servicio | Qué ya existe | Gap analítico |
|---|---|---|
| `DashboardService` | Overview mensual, tendencia 12 meses, gastos por categoría, cash flow statement | Sin comparativa YoY, sin proyecciones, sin score |
| `CurrentDashboardService` | Ciclo actual en tiempo real | Sin benchmark histórico |
| `FinancialPositionService` | Snapshot actual de patrimonio | `HistoricalSnapshotsSupported = false` — sin timeline |
| `Expense` | `IsRecurring`, `RecurrenceType`, `Merchant` | Nunca se exponen como inteligencia analítica |
| `BudgetCategory` / `BudgetPeriod` | Presupuesto por período | Sin historial budget vs. real |
| `Debt` / `DebtPayment` | Saldo corriente + pagos | Sin proyección de payoff |
| `SavingsGoal` / `Contributions` | Saldo actual + meta | Sin ETA dinámica |
| `Investment` | `CurrentValue`, `InitialAmount` | Sin retorno acumulado en el tiempo |
| `AccountReconciliation` | Diferencia esperado vs. real | Sin tendencia de divergencia |

---

## Fase 11 — Backend Analítico

### 11.1 Nueva entidad: `NetWorthSnapshot`

Habilita el historial de patrimonio (resuelve `HistoricalSnapshotsSupported = false`).

```
Domain/Entities/NetWorthSnapshot.cs
```

```csharp
public class NetWorthSnapshot : BaseEntity
{
    public Guid UserId { get; set; }
    public DateOnly SnapshotDate { get; set; }   // Primer día del mes (o fecha manual)
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal NetWorth { get; set; }
    public decimal CashAccounts { get; set; }
    public decimal SavingsAccounts { get; set; }
    public decimal InvestmentPositions { get; set; }
    public decimal DebtLiabilities { get; set; }
    public decimal CreditCardLiabilities { get; set; }
    public SnapshotSource Source { get; set; }   // Automatic | Manual

    public User User { get; set; } = null!;
}
```

**Enum `SnapshotSource`:**
```
Domain/Enums/SnapshotSource.cs
→ Automatic, Manual
```

**Config EF (snake_case):**
```
Infrastructure/Configurations/NetWorthSnapshotConfiguration.cs
→ tabla: net_worth_snapshots
→ índice único: (user_id, snapshot_date)
```

**Trigger de snapshot:** el servicio crea un snapshot automático la primera vez que
`FinancialPositionService.GetAsync` se llama en un mes nuevo para ese usuario.
No requiere background job en esta fase — se genera on-demand con deduplicación
por `(user_id, snapshot_date)`.

---

### 11.2 Nuevo servicio: `AnalyticsService`

```
Application/Services/AnalyticsService.cs
Application/Interfaces/IAnalyticsService.cs
```

#### Métodos públicos

```csharp
// 1. Timeline de patrimonio neto (últimos N meses)
Task<NetWorthTimelineDto> GetNetWorthTimelineAsync(Guid userId, int months, CancellationToken ct);

// 2. Score de salud financiera (0–100)
Task<FinancialHealthScoreDto> GetFinancialHealthScoreAsync(Guid userId, int month, int year, CancellationToken ct);

// 3. Inteligencia de gastos: top merchants, gastos recurrentes, drift por categoría
Task<ExpenseIntelligenceDto> GetExpenseIntelligenceAsync(Guid userId, int month, int year, CancellationToken ct);

// 4. Proyección de pago de deudas
Task<DebtProjectionDto> GetDebtProjectionAsync(Guid userId, CancellationToken ct);

// 5. ETA de metas de ahorro
Task<IReadOnlyList<SavingsGoalEtaDto>> GetSavingsGoalEtaAsync(Guid userId, CancellationToken ct);

// 6. Comparativa año vs. año (YoY)
Task<YearOverYearDto> GetYearOverYearAsync(Guid userId, int year, CancellationToken ct);

// 7. Historial budget vs. real (últimos N períodos)
Task<BudgetVsActualHistoryDto> GetBudgetVsActualHistoryAsync(Guid userId, int months, CancellationToken ct);
```

---

### 11.3 DTOs

Carpeta: `Application/DTOs/Analytics/`

#### `NetWorthTimelineDto`
```csharp
public class NetWorthTimelineDto
{
    public List<string> Labels { get; set; }          // "Ene 25", "Feb 25", ...
    public List<decimal> NetWorth { get; set; }
    public List<decimal> TotalAssets { get; set; }
    public List<decimal> TotalLiabilities { get; set; }
    public decimal NetWorthChange { get; set; }        // Δ vs. primer snapshot
    public decimal NetWorthChangePct { get; set; }
}
```

#### `FinancialHealthScoreDto`
```csharp
public class FinancialHealthScoreDto
{
    public int Score { get; set; }              // 0–100
    public string Grade { get; set; }           // A / B / C / D / F
    public HealthScoreComponents Components { get; set; }
    public List<string> Recommendations { get; set; }
}

public class HealthScoreComponents
{
    // Cada componente: valor 0-100 + peso en el score global
    public ScoreComponent SavingsRate { get; set; }       // peso 25%
    public ScoreComponent DebtToIncome { get; set; }      // peso 20%
    public ScoreComponent EmergencyFundCoverage { get; set; } // peso 20%
    public ScoreComponent ExpenseRatio { get; set; }      // peso 15%
    public ScoreComponent BudgetAdherence { get; set; }   // peso 10%
    public ScoreComponent InvestmentRate { get; set; }    // peso 10%
}

public class ScoreComponent
{
    public int Score { get; set; }
    public decimal Value { get; set; }      // Valor real (tasa, ratio)
    public decimal Benchmark { get; set; }  // Referencia saludable
    public string Label { get; set; }
    public string Status { get; set; }      // Good | Warning | Critical
}
```

**Fórmulas del score:**

| Componente | Benchmark saludable | Cálculo |
|---|---|---|
| Savings Rate | ≥ 20 % | `netSavings / income * 100` |
| Debt-to-Income | ≤ 35 % | `totalDebtPayments / income * 100` |
| Emergency Fund | ≥ 3 meses de gastos | `emergencyFundBalance / avgMonthlyExpenses` |
| Expense Ratio | ≤ 50 % | `netPersonalExpenses / income * 100` |
| Budget Adherence | ≤ 100 % ejecutado | `actual / budget * 100` (invertido) |
| Investment Rate | ≥ 10 % | `investmentContributions / income * 100` |

#### `ExpenseIntelligenceDto`
```csharp
public class ExpenseIntelligenceDto
{
    public List<TopMerchantDto> TopMerchants { get; set; }           // Top 5 por monto
    public List<RecurringExpenseDto> RecurringExpenses { get; set; }
    public List<CategoryDriftDto> CategoryDrift { get; set; }        // Δ vs. mes anterior
}

public class TopMerchantDto
{
    public string Merchant { get; set; }
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public string CategoryName { get; set; }
}

public class RecurringExpenseDto
{
    public Guid ExpenseId { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public string RecurrenceType { get; set; }   // Monthly | Weekly | etc.
    public string CategoryName { get; set; }
    public decimal AnnualImpact { get; set; }
}

public class CategoryDriftDto
{
    public string CategoryName { get; set; }
    public string CategoryColor { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal PreviousAmount { get; set; }
    public decimal DriftAmount { get; set; }
    public decimal DriftPct { get; set; }
}
```

#### `DebtProjectionDto`
```csharp
public class DebtProjectionDto
{
    public decimal TotalOutstanding { get; set; }
    public decimal AvgMonthlyPayment { get; set; }        // Promedio últimos 3 meses
    public int EstimatedPayoffMonths { get; set; }
    public DateOnly EstimatedPayoffDate { get; set; }
    public List<DebtLineProjectionDto> ByDebt { get; set; }
}

public class DebtLineProjectionDto
{
    public Guid DebtId { get; set; }
    public string Name { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AvgMonthlyPayment { get; set; }
    public int EstimatedPayoffMonths { get; set; }
    public DateOnly EstimatedPayoffDate { get; set; }
}
```

#### `SavingsGoalEtaDto`
```csharp
public class SavingsGoalEtaDto
{
    public Guid GoalId { get; set; }
    public string Name { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal Remaining { get; set; }
    public decimal ProgressPct { get; set; }
    public decimal AvgMonthlyContribution { get; set; }   // Promedio últimos 3 meses
    public int? EstimatedMonthsToGoal { get; set; }       // null si sin aportes
    public DateOnly? EstimatedCompletionDate { get; set; }
    public bool IsOnTrack { get; set; }                   // vs. TargetDate si existe
}
```

#### `YearOverYearDto`
```csharp
public class YearOverYearDto
{
    public int Year { get; set; }
    public int PreviousYear { get; set; }
    public List<YoYMonthDto> Months { get; set; }   // Ene–Dic
    public YoYTotalsDto Totals { get; set; }
}

public class YoYMonthDto
{
    public string MonthLabel { get; set; }
    public decimal CurrentIncome { get; set; }
    public decimal CurrentExpenses { get; set; }
    public decimal CurrentNetSavings { get; set; }
    public decimal PrevIncome { get; set; }
    public decimal PrevExpenses { get; set; }
    public decimal PrevNetSavings { get; set; }
}

public class YoYTotalsDto
{
    public decimal IncomeChangeAbs { get; set; }
    public decimal IncomeChangePct { get; set; }
    public decimal ExpensesChangeAbs { get; set; }
    public decimal ExpensesChangePct { get; set; }
    public decimal NetSavingsChangeAbs { get; set; }
    public decimal NetSavingsChangePct { get; set; }
}
```

#### `BudgetVsActualHistoryDto`
```csharp
public class BudgetVsActualHistoryDto
{
    public List<BudgetVsActualPeriodDto> Periods { get; set; }
}

public class BudgetVsActualPeriodDto
{
    public string Label { get; set; }
    public decimal Budgeted { get; set; }
    public decimal Actual { get; set; }
    public decimal Variance { get; set; }
    public decimal AdherencePct { get; set; }  // actual / budgeted * 100
}
```

---

### 11.4 Repositorios nuevos / extensiones

#### `INetWorthSnapshotRepository`
```
Domain/Interfaces/Repositories/INetWorthSnapshotRepository.cs
Infrastructure/Repositories/NetWorthSnapshotRepository.cs
```
```csharp
Task<NetWorthSnapshot?> GetByDateAsync(Guid userId, DateOnly date, CancellationToken ct);
Task<IReadOnlyList<NetWorthSnapshot>> GetRangeAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct);
Task UpsertAsync(NetWorthSnapshot snapshot, CancellationToken ct);
```

#### Extensiones en repositorios existentes
- `IExpenseRepository`
  - `GetTopMerchantsByDateRangeAsync(userId, start, end, topN, ct)` → `IReadOnlyList<(string Merchant, decimal Total, int Count, string CategoryName)>`
  - `GetRecurringByUserAsync(userId, ct)` → `IReadOnlyList<Expense>`
- `ISavingsGoalRepository`
  - `GetAvgMonthlyContributionAsync(goalId, months, ct)` → `decimal`
- `IDebtRepository`
  - `GetAvgMonthlyPaymentAsync(debtId, months, ct)` → `decimal`

---

### 11.5 Controller: `AnalyticsController`

```
API/Controllers/AnalyticsController.cs
```

```
GET /api/analytics/net-worth-timeline?months=12
GET /api/analytics/health-score?month=8&year=2026
GET /api/analytics/expense-intelligence?month=8&year=2026
GET /api/analytics/debt-projection
GET /api/analytics/savings-goals-eta
GET /api/analytics/year-over-year?year=2026
GET /api/analytics/budget-vs-actual?months=6
```

Todos los endpoints usan `[Authorize]` y extraen `userId` del claim JWT igual que los demás controllers.

---

### 11.6 Migración

```
AddNetWorthSnapshots
```

```sql
CREATE TABLE net_worth_snapshots (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES "AspNetUsers"(id),
    snapshot_date DATE NOT NULL,
    total_assets NUMERIC(18,2) NOT NULL,
    total_liabilities NUMERIC(18,2) NOT NULL,
    net_worth NUMERIC(18,2) NOT NULL,
    cash_accounts NUMERIC(18,2) NOT NULL,
    savings_accounts NUMERIC(18,2) NOT NULL,
    investment_positions NUMERIC(18,2) NOT NULL,
    debt_liabilities NUMERIC(18,2) NOT NULL,
    credit_card_liabilities NUMERIC(18,2) NOT NULL,
    source TEXT NOT NULL DEFAULT 'Automatic',
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ,
    deleted_at TIMESTAMPTZ,
    UNIQUE (user_id, snapshot_date)
);
```

---

## Fase 12 — Frontend Analítico

### 12.1 Estructura de archivos

```
frontend/src/
  pages/
    AnalyticsPage.tsx                  ← nueva página principal
  features/
    analytics/
      hooks/
        useAnalytics.ts                ← hook central (TanStack Query)
        useNetWorthTimeline.ts
        useHealthScore.ts
        useExpenseIntelligence.ts
        useDebtProjection.ts
        useSavingsGoalEta.ts
        useYearOverYear.ts
        useBudgetVsActual.ts
      api/
        analytics.api.ts               ← clientes HTTP
      types/
        analytics.types.ts             ← tipos TypeScript
      components/
        NetWorthChart.tsx
        FinancialHealthScore.tsx
        HealthScoreBreakdown.tsx
        ExpenseIntelligencePanel.tsx
        TopMerchantsTable.tsx
        RecurringExpensesList.tsx
        CategoryDriftChart.tsx
        DebtProjectionPanel.tsx
        SavingsGoalEtaList.tsx
        YearOverYearTable.tsx
        BudgetVsActualChart.tsx
        AnalyticsSectionShell.tsx      ← contenedor reutilizable con título + skeleton
```

---

### 12.2 Tipos TypeScript

`analytics.types.ts` mapea 1:1 los DTOs del backend. Todas las fechas como `string` (`YYYY-MM-DD`).

```typescript
// Ejemplos representativos
export interface FinancialHealthScoreDto {
  score: number;
  grade: 'A' | 'B' | 'C' | 'D' | 'F';
  components: HealthScoreComponents;
  recommendations: string[];
}

export interface ScoreComponent {
  score: number;
  value: number;
  benchmark: number;
  label: string;
  status: 'Good' | 'Warning' | 'Critical';
}
```

---

### 12.3 Hook central `useAnalytics.ts`

```typescript
// Agrega todos los hooks en un objeto único para AnalyticsPage
export function useAnalytics(month: number, year: number) {
  const netWorthTimeline = useNetWorthTimeline(12);
  const healthScore = useHealthScore(month, year);
  const expenseIntelligence = useExpenseIntelligence(month, year);
  const debtProjection = useDebtProjection();
  const savingsGoalEta = useSavingsGoalEta();
  const yoy = useYearOverYear(year);
  const budgetVsActual = useBudgetVsActual(6);

  return { netWorthTimeline, healthScore, expenseIntelligence, debtProjection, savingsGoalEta, yoy, budgetVsActual };
}
```

Cada sub-hook usa `queryKey` estable para caché independiente:
- `['analytics', 'net-worth-timeline', months]`
- `['analytics', 'health-score', month, year]`
- etc.

---

### 12.4 Layout de `AnalyticsPage`

```
┌─────────────────────────────────────────────────────────────────────┐
│  Analytics                         [Selector Mes/Año]               │
├─────────────────────────────────────────────────────────────────────┤
│  [Score de Salud Financiera — círculo grande + componentes ]        │
│   Score: 72 / 100  Grado: B                                         │
│   ┌──Ahorro──┐ ┌──Deuda──┐ ┌──EmgFund──┐ ┌──Gastos──┐ ...         │
├─────────────────────────────────────────────────────────────────────┤
│  Patrimonio Neto                    │  Comparativa Año vs. Año       │
│  [Línea: activos / pasivos / neto]  │  [Tabla mes a mes YoY]        │
├─────────────────────────────────────────────────────────────────────┤
│  Inteligencia de Gastos                                              │
│  ┌───Top Merchants───┐  ┌──Gastos Recurrentes──┐  ┌──Drift──┐      │
├─────────────────────────────────────────────────────────────────────┤
│  Proyección de Deudas               │  ETA de Metas de Ahorro       │
│  [Payoff en N meses]                │  [Lista de metas con ETA]     │
├─────────────────────────────────────────────────────────────────────┤
│  Presupuesto vs. Real (últimos 6 meses)                              │
│  [Barras agrupadas: presupuesto / real]                              │
└─────────────────────────────────────────────────────────────────────┘
```

---

### 12.5 `AnalyticsSectionShell` — contenedor estándar

```tsx
interface AnalyticsSectionShellProps {
  title: string;
  children: React.ReactNode;
  isLoading?: boolean;
  className?: string;
}
```

Equivalente al rol que cumple `SavingsModalShell` en el módulo de metas, pero para secciones de analytics.
Incluye skeleton loader cuando `isLoading = true`.

---

### 12.6 Navegación

Agregar enlace "Analytics" al sidebar existente junto a "Dashboard" y "Dashboard Actual".

---

## Secuencia de implementación

| Paso | Qué hacer | Archivo(s) clave |
|---|---|---|
| 1 | Entidad + config EF `NetWorthSnapshot` | `Domain/Entities/`, `Infrastructure/Configurations/` |
| 2 | Migración `AddNetWorthSnapshots` | `dotnet ef migrations add` |
| 3 | `INetWorthSnapshotRepository` + implementación | `Domain/Interfaces/`, `Infrastructure/Repositories/` |
| 4 | Extensiones en repositorios existentes | `IExpenseRepository`, `IDebtRepository`, `ISavingsGoalRepository` |
| 5 | DTOs en `Application/DTOs/Analytics/` | 7 DTOs principales |
| 6 | `IAnalyticsService` + `AnalyticsService` | `Application/` |
| 7 | Registro DI en `Program.cs` | `FinanceApp.API/Program.cs` |
| 8 | `AnalyticsController` | `API/Controllers/` |
| 9 | Tipos TypeScript `analytics.types.ts` | `frontend/src/features/analytics/types/` |
| 10 | `analytics.api.ts` — clientes HTTP | `frontend/src/features/analytics/api/` |
| 11 | Hooks por endpoint (7 hooks) | `frontend/src/features/analytics/hooks/` |
| 12 | Componentes de visualización (11 componentes) | `frontend/src/features/analytics/components/` |
| 13 | `AnalyticsPage` + enlace en sidebar | `frontend/src/pages/` |

---

## Decisiones de diseño (ADRs implícitos)

### ADR-9.1: Snapshots on-demand, no scheduled job
El snapshot de patrimonio se crea la primera vez que se llama `GetNetWorthTimelineAsync`
en un mes nuevo. El índice único `(user_id, snapshot_date)` garantiza idempotencia.
No se introduce ningún background job en esta fase para mantener el stack simple.

### ADR-9.2: Score calculado en tiempo real, no persistido
El `FinancialHealthScore` se recalcula en cada request a partir de los datos existentes.
No se almacena porque su valor cambia con cada transacción y el cálculo es ligero.

### ADR-9.3: Proyecciones basadas en promedio móvil de 3 meses
Para `DebtProjection` y `SavingsGoalEta`, el ritmo de pago/aporte se estima como el
promedio de los últimos 3 meses con actividad. Si hay cero meses con actividad,
el campo `EstimatedPayoffMonths` devuelve `null` (sin proyección).

### ADR-9.4: AnalyticsPage no reemplaza DashboardPage
Analytics es una vista complementaria de inteligencia; el dashboard existente
mantiene su rol operacional de ciclo corriente. Ambas viven en el sidebar.

### ADR-9.5: Todos los endpoints son GET, sin escritura
Tarea 9 no introduce ninguna nueva entidad de escritura (salvo `NetWorthSnapshot`
que se escribe de forma automática y transparente). Ningún endpoint es POST/PUT/DELETE.

---

## Checklist de cierre

- [ ] Migración aplicada y snapshot table existe en BD
- [ ] GET /api/analytics/net-worth-timeline devuelve datos
- [ ] GET /api/analytics/health-score devuelve score y componentes
- [ ] GET /api/analytics/expense-intelligence devuelve top merchants + recurrentes
- [ ] GET /api/analytics/debt-projection devuelve ETA por deuda
- [ ] GET /api/analytics/savings-goals-eta devuelve ETA por meta
- [ ] GET /api/analytics/year-over-year devuelve tabla YoY
- [ ] GET /api/analytics/budget-vs-actual devuelve historial 6 meses
- [ ] `AnalyticsPage` renderiza sin errores
- [ ] Score de salud financiera muestra grade y 6 componentes
- [ ] Gráfico de patrimonio neto muestra línea temporal
- [ ] Top merchants y gastos recurrentes se listan correctamente
- [ ] Proyección de deudas muestra fecha estimada de pago
- [ ] ETA de metas es coherente con el ritmo de aportes actual
- [ ] Navegación Analytics visible en sidebar
