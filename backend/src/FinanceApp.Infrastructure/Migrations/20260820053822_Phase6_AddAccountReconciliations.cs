using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_AddAccountReconciliations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_reconciliations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reconciliation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expected_balance = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    actual_balance = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    difference = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    adjustment_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_reconciliations", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_reconciliations_account_transactions_adjustment_tra~",
                        column: x => x.adjustment_transaction_id,
                        principalTable: "account_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_account_reconciliations_financial_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "financial_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_reconciliations_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_account_reconciliations_account_date",
                table: "account_reconciliations",
                columns: new[] { "account_id", "reconciliation_date" });

            migrationBuilder.CreateIndex(
                name: "idx_account_reconciliations_user_account",
                table: "account_reconciliations",
                columns: new[] { "user_id", "account_id" });

            migrationBuilder.CreateIndex(
                name: "IX_account_reconciliations_adjustment_transaction_id",
                table: "account_reconciliations",
                column: "adjustment_transaction_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_reconciliations");
        }
    }
}
