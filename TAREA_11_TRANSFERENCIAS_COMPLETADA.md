# Tarea 11 — Transferencias entre Cuentas Propias ✅
Completada: 24 de agosto de 2026

## Commits
| # | Capa | Hash |
|---|---|---|
| 1 | Domain | d1369d9 |
| 2 | Infrastructure | 8a1a7a5 |
| 3 | Application | 9cf96e5 |
| 4 | API + migración | c34990a |
| 5 | Tests (8 casos) | ddc529c |
| 6 | Frontend | 68d97eb |

## Qué resuelve
- Transferencias reales entre cuentas propias con dos patas
  atómicas — suma neta cero, patrimonio invariante
- TransferGroupId para idempotencia en reintentos
- SourceType "account-transfer:out" / "account-transfer:in"
  consistente con el precedente del ledger existente
- InsufficientFundsWarning: advertencia no bloqueante
- Frontend: TransferModal + TransferHistoryPanel + limpieza
  del flujo viejo (transfer() en accounts.api.ts eliminado)
- 125/125 tests pasando

## Invariante central verificada
Una transferencia entre cuentas propias tiene dos patas
por el mismo importe. Suma neta = 0.
No genera ingreso, gasto ni cambio patrimonial.

## Pendiente (próxima sesión)
- Limpieza: retirar POST /accounts/transfers (endpoint viejo)
  y sus 6 tests de FinancialAccountTransferTests.cs —
  ya sin consumidores en frontend
- CancelAsync inalcanzable en MVP (Status siempre Completed) —
  queda listo para flujo de transferencias programadas futuro
