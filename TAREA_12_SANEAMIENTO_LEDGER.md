# Tarea 12 — Saneamiento del Ledger y Cuentas Predeterminadas

> Estado: **bloque A cerrado y desplegado** el 2026-08-27 (commit `7111678`). La
> migración `EnforceSingleDefaultAccountPerType` ya corrió contra Neon en el deploy
> de Render: queda una sola cuenta predeterminada por tipo y el índice único está
> en su sitio. Diagnóstico cerrado el 2026-08-26. **Pendiente: bloque B (datos).**

## Defectos confirmados

1. **`GetOrCreateDefaultEntityAsync`** (`FinancialAccountService.cs:359-391`)
   crea cuentas con `IsDefault = true` sin llamar a `ClearDefaultAsync`, y no
   escribe la fila `account-opening` en el ledger. Para `Investment` siembra
   `CurrentBalance = GetTotalCurrentValueAsync()` sin respaldo en el ledger, lo
   que produce un descuadre silencioso garantizado.

2. **`GetDefaultAsync`** (`FinancialAccountRepository.cs:22-31`) es un
   `FirstOrDefaultAsync` **sin `OrderBy`**. Con varias cuentas predeterminadas
   por tipo, PostgreSQL devuelve una no determinista que puede cambiar entre
   ejecuciones según el plan de consulta.

3. **`FinancialAccountConfiguration.cs:29`** solo tiene `HasIndex(UserId, Type)`
   sin `IsUnique`: nada impide más de una cuenta predeterminada por tipo.

4. **`GetOpeningAndAdjustmentTotalsAsync`**
   (`FinancialAccountRepository.cs:69`) filtra solo `'account-opening'`, pero en
   la base los saldos iniciales están bajo `savings-opening` (7.829,80) e
   `investment-opening` (13.014,48) — source types huérfanos que ya no existen
   en el código. Son invisibles para `FinancialPositionService`, que reporta
   `AccountOpeningBalances = 0` con ~20.844 en aperturas reales.

5. **`AccountReconciliationService.ApplyAsync`** hace tres `SaveChanges` sueltos
   sin transacción (líneas 77, 79 y 96). El servicio no tiene `IUnitOfWork`
   inyectado, así que no hay rollback posible.

6. **`UpdateAsync`** (`FinancialAccountService.cs:128-148`) sigue permitiendo el
   ajuste silencioso de saldo que `TAREA_8_CONCILIACION.md` quería eliminar, y
   calcula la diferencia contra la caché `current_balance` en vez de contra el
   ledger.

## Estado de los datos en producción (Neon)

> Rehecho el 2026-08-27 tras aplicar el bloque A. **El diagnóstico del 26 estaba
> equivocado en su premisa central** y esta sección lo corrige.

Hay **cuatro usuarios**, y todo está aislado por `user_id`:

| Usuario | Email | Qué es |
|---|---|---|
| `a6bb89be` | carlosadmin@gmail.com | **La cuenta viva.** Creada el 6 de agosto |
| `ce2839d8` | carloshopex2000@gmail.com | Primer intento, con los datos del año migrados. **No se toca** |
| `6443ed98` | admin@admin.com | Prueba |
| `bb450bac` | test@test.com | Prueba |

- **No había cuentas predeterminadas duplicadas.** Las "4 por tipo" eran una por
  usuario en cuatro usuarios distintos. El índice único entró sin degradar nada
  y el `UPDATE` de normalización no tocó ninguna fila.
- **El espejo de ±4.368,93 y el enredo del 30 de julio son de `ce2839d8`**, no de
  la cuenta viva: tres aperturas repetidas del mismo fondo, un ACUMULAC importado
  dos veces (4.241,03 el 2025-01-17 y otra vez el 2026-06-25) y ajustes manuales
  para cuadrar. No contamina nada de `a6bb89be`. Se deja como está; si algún día
  se migran esos datos a la cuenta viva, hay que sanear su ledger antes.
- **En la cuenta viva** todo cuadra al céntimo salvo `Efectivo` (`aced52f8`), al
  que le falta su apertura: saldo 17,20 con ledger en −11,45, o sea 28,65.
- **No existe ni una fila `account-opening` en toda la base.** Las aperturas viven
  bajo `savings-opening` (7.829,80) e `investment-opening` (13.014,48), source
  types huérfanos. En la cuenta viva son tres filas que suman 5.933,83.
- `account_reconciliations` tiene **una fila**: el +243,33 de `Billetera Operativa`
  del 27 de agosto, hecho desde la app tras el deploy del bloque A. Confirma que
  el flujo nuevo funciona en producción. El usuario lo da por erróneo.
- Pendiente de datos: el `Portafolio` de la cuenta viva marca 4.737,12 en saldo y
  ledger, pero el usuario dice 3.737,12. Mil dólares exactos de diferencia.

## Plan

| # | Acción | Bloque |
|---|---|---|
| 1 | ✅ Índice único `(user_id, type)` filtrado por `is_default AND deleted_at IS NULL` | A — código |
| 2 | ✅ `ORDER BY` determinista en `GetDefaultAsync` | A — código |
| 3 | ✅ `GetOrCreateDefaultEntityAsync`: limpiar defaults previos + escribir apertura | A — código |
| 4 | ✅ Quitar el ajuste silencioso de `UpdateAsync` | A — código |
| 5 | ✅ Envolver `ApplyAsync` en `ExecuteInTransactionAsync` | A — código |
| 6 | ~~Consolidar cuentas gemelas~~ — **no aplica**: no había duplicados | B — datos |
| 7 | Normalizar los `*-opening` huérfanos a `account-opening` | B — datos |
| 8 | Escribir la apertura que falta en `Efectivo` (28,65) | B — datos |
| 9 | Aclarar los 1.000 del `Portafolio` y revertir el +243,33 | B — datos |
| 10 | Módulo de reportes | C — solo después de B |

El bloque B quedó reducido a la cuenta viva: los puntos 7 a 9. El script está en
`scratchpad/b2-saneamiento.sql` (una transacción, con `ROLLBACK` puesto a
propósito para correrlo en seco antes de confirmar), pero **sus pasos 2, 3 y 4
apuntan a `ce2839d8` y hay que quitarlos** — esa cuenta no se toca.

**Antes de escribir:** backup de la base en Neon.

---

## Bloque A — qué se hizo (2026-08-27)

Migración `20260827121012_EnforceSingleDefaultAccountPerType`:

- **Normaliza antes de indexar.** Un `UPDATE` con `ROW_NUMBER()` deja una sola
  predeterminada por `(user_id, type)` entre las cuentas no borradas. Sobrevive
  la activa con más movimientos en el ledger; en empate, la de mayor saldo
  absoluto y más antigua. A las demás solo les quita `is_default`: no borra
  cuentas ni toca saldos (eso sigue siendo el bloque B).
- **Crea el índice único** `ux_financial_accounts_default_per_type` sobre
  `(user_id, type) WHERE is_default AND deleted_at IS NULL`. Convive con el
  índice de búsqueda `IX_financial_accounts_user_id_type`, que no se toca.

Código:

- `GetDefaultAsync` ordena por `Id` antes del `First`. Se eligió `Id` y no
  `created_at` porque SQLite (el proveedor de los tests) no ordena por
  `DateTimeOffset`; cuál de las duplicadas conserva la marca lo decide la
  migración, no esta consulta.
- `GetOrCreateDefaultEntityAsync` limpia los defaults previos del mismo tipo
  (incluidos los de cuentas inactivas, invisibles para `GetDefaultAsync` pero
  que sí chocan con el índice) y escribe su fila `account-opening` de 0.
- **Se eliminó la siembra de saldo desde inversiones.** La cuenta de inversión
  nace en 0: su saldo lo construyen los movimientos, no un agregado calculado.
- `UpdateAsync` ya no genera `account-adjustment`: si el DTO trae un saldo
  distinto, lanza `BALANCE_CHANGE_REQUIRES_RECONCILIATION`. El frontend solo
  usa ese endpoint para renombrar y marcar predeterminada, reenviando el saldo
  tal cual, así que no cambia nada para el usuario.
- `AccountReconciliationService` recibe `IUnitOfWork` y `ApplyAsync` corre
  entero dentro de `ExecuteInTransactionAsync`.

Tests nuevos en `backend/tests/FinanceApp.UnitTests/AccountDefaultLedgerTests.cs`
(SQLite en memoria): resolución estable con duplicados heredados, apertura
escrita y default único al crear la cuenta, rechazo del ajuste silencioso, y
conciliación con y sin fallo (rollback del ajuste y del saldo). `dotnet test`:
125/125 en verde.

**Aplicado el 2026-08-27** vía push a master (Render ejecuta `Database.Migrate()`
al arrancar, `Program.cs:109`). Deploy correcto. De las 4 predeterminadas por tipo
sobrevivió la que eligió el `ROW_NUMBER()`; si alguna no fuera la cuenta que de
verdad opera, se corrige desde la app con "Usar como cuenta predeterminada" y el
índice se encarga de que la otra pierda la marca.

---

## Cómo consultar Neon

La cadena de conexión **no está en el repo** (`appsettings.json` apunta a
localhost) y no hay `psql` instalado. Se lee de la variable de entorno
`FINFLOW_DB`, que se fija con `setx FINFLOW_DB "Host=...neon.tech;..."`.

Para leer hay un pequeño runner en el scratchpad de la sesión
(`scratchpad/dbq`, proyecto .NET con Npgsql): `dotnet run --project dbq
<archivo.sql>`. Rechaza cualquier SQL que escriba salvo que se le pase
`--write`. Si el scratchpad ya no existe, se rehace en dos minutos.

## Prompt para reanudar

```
Lee TAREA_12_SANEAMIENTO_LEDGER.md en la raíz del repo. El bloque A está hecho
y desplegado; toca el bloque B (datos en Neon).

Orden acordado:
1. Diagnóstico de solo lectura: descuadre por cuenta, composición del ledger por
   source_type y las filas *-opening huérfanas. El diagnóstico del 26 de agosto
   es de antes de la migración, así que hay que rehacer la foto.
2. Backup de Neon antes de cualquier escritura.
3. Consolidar cuentas gemelas: las vacías se desactivan; si alguna tiene
   movimientos, se pasa a la superviviente con una transferencia real, no con un
   UPDATE.
4. Normalizar los savings-opening / investment-opening huérfanos a
   account-opening, para que FinancialPositionService los cuente como saldo
   inicial y no como flujo. Enséñame los números antes de escribir.
5. Reparar los descuadres con conciliaciones explícitas desde la app (ya son
   atómicas), no con SQL, para que quede el rastro en account_reconciliations.

Muéstrame cada paso antes de pasar al siguiente. Nada de escrituras sin backup.
```
