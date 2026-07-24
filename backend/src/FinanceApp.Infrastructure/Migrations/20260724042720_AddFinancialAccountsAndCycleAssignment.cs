using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialAccountsAndCycleAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "account_id",
                table: "incomes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "assigned_cycle_start",
                table: "incomes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "account_id",
                table: "expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "financial_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    current_balance = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_financial_accounts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    source_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_transactions_financial_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "financial_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_transactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_incomes_account_id",
                table: "incomes",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_account_id",
                table: "expenses",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_transactions_account_id_date",
                table: "account_transactions",
                columns: new[] { "account_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_account_transactions_user_id_source_type_source_id",
                table: "account_transactions",
                columns: new[] { "user_id", "source_type", "source_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_financial_accounts_user_id_type",
                table: "financial_accounts",
                columns: new[] { "user_id", "type" });

            migrationBuilder.AddForeignKey(
                name: "FK_expenses_financial_accounts_account_id",
                table: "expenses",
                column: "account_id",
                principalTable: "financial_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_incomes_financial_accounts_account_id",
                table: "incomes",
                column: "account_id",
                principalTable: "financial_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expenses_financial_accounts_account_id",
                table: "expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_incomes_financial_accounts_account_id",
                table: "incomes");

            migrationBuilder.DropTable(
                name: "account_transactions");

            migrationBuilder.DropTable(
                name: "financial_accounts");

            migrationBuilder.DropIndex(
                name: "IX_incomes_account_id",
                table: "incomes");

            migrationBuilder.DropIndex(
                name: "IX_expenses_account_id",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "account_id",
                table: "incomes");

            migrationBuilder.DropColumn(
                name: "assigned_cycle_start",
                table: "incomes");

            migrationBuilder.DropColumn(
                name: "account_id",
                table: "expenses");
        }
    }
}
