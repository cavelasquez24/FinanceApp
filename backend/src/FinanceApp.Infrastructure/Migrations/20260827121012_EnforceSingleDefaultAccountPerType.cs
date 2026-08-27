using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleDefaultAccountPerType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los datos existentes tienen varias cuentas predeterminadas por tipo, así
            // que el índice único no se puede crear sin normalizar antes. Sobrevive la
            // cuenta que de verdad opera: activa, con más movimientos en el ledger y,
            // en empate, la de mayor saldo y más antigua. Las demás pierden la marca
            // (no se borran ni se tocan sus saldos: eso es el bloque B de la tarea 12).
            migrationBuilder.Sql(@"
                WITH ranked AS (
                    SELECT a.id,
                           ROW_NUMBER() OVER (
                               PARTITION BY a.user_id, a.type
                               ORDER BY a.is_active DESC,
                                        (SELECT COUNT(*)
                                           FROM account_transactions t
                                          WHERE t.account_id = a.id
                                            AND t.deleted_at IS NULL) DESC,
                                        ABS(a.current_balance) DESC,
                                        a.created_at,
                                        a.id
                           ) AS rn
                      FROM financial_accounts a
                     WHERE a.is_default AND a.deleted_at IS NULL
                )
                UPDATE financial_accounts AS target
                   SET is_default = FALSE,
                       updated_at = NOW()
                  FROM ranked
                 WHERE target.id = ranked.id
                   AND ranked.rn > 1;
            ");

            migrationBuilder.CreateIndex(
                name: "ux_financial_accounts_default_per_type",
                table: "financial_accounts",
                columns: new[] { "user_id", "type" },
                unique: true,
                filter: "is_default AND deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_financial_accounts_default_per_type",
                table: "financial_accounts");
        }
    }
}
