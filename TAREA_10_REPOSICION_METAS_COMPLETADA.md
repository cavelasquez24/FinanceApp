# Tarea 10 — Reposición Programada de Metas ✅
Completada: 23 de agosto de 2026

## Commits
| # | Capa | Hash |
|---|---|---|
| 1 | Domain | 73871dd |
| 2 | Infrastructure | 90cbcb4 |
| 3 | Application | ea465c0 |
| 4 | API + migración | cc54494 |
| 5 | Tests (11 casos) | d82e206 |
| 6 | Frontend | a41ca7e |

## Qué resuelve
- Reemplaza "deuda personal" (que reducía patrimonio erróneamente)
  por SavingsReplenishment: compromiso interno neutro en patrimonio
- Débito automático por ciclo desde cuenta operativa hacia la meta
- Flujo de 2 pasos en WithdrawModal: retiro TemporaryLoan →
  ReplenishmentCreateForm prellenado
- CycleReplenishmentCommitment visible en CurrentDashboard
- 117/117 tests pasando

## Invariante central verificada
Tomar dinero de una meta propia NO reduce patrimonio.
Solo redistribuye entre cuenta operativa y meta asignada.

## Pendiente nice-to-have (fuera de alcance de esta tarea)
- Scheduler automático para ExecuteCycleDebitsAsync
  (hoy se dispara manualmente vía POST /execute-cycle)
- Notificaciones push cuando hay fondos insuficientes
- Editar MonthlyDebitAmount de un plan activo
