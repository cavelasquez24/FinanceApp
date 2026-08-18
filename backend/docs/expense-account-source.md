# Cuenta origen en gastos

## Decisión temporal sobre tarjeta de crédito

Un gasto representa consumo y el método de pago describe el instrumento usado. Para los métodos liquidados al momento (efectivo, débito, transferencia u otro), el usuario debe seleccionar explícitamente una cuenta de tipo `Cash`, activa y propia. El gasto crea un único movimiento negativo en esa cuenta; presupuesto y saldo permanecen conceptos separados.

Las compras con `credit_card` están bloqueadas temporalmente en API y frontend. No se descuenta caja ni se crea una cuenta de efectivo para una tarjeta, porque aún no existe el pasivo transaccional que las tareas #3 y #4 modelarán.

No se requiere migración: `Expense.AccountId` y `AccountTransaction` ya existen. Los gastos históricos sin cuenta deberán elegir una cuenta activa al editarse.
