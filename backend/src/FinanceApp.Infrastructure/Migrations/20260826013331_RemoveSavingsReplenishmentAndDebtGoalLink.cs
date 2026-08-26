using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSavingsReplenishmentAndDebtGoalLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_savings_goal_contributions_savings_replenishments_savings_r~",
                table: "savings_goal_contributions");

            migrationBuilder.DropTable(
                name: "savings_replenishments");

            migrationBuilder.DropIndex(
                name: "idx_savings_goal_contributions_replenishment_id",
                table: "savings_goal_contributions");

            migrationBuilder.DropIndex(
                name: "idx_debts_linked_savings_goal_id",
                table: "debts");

            migrationBuilder.DropColumn(
                name: "debit_type",
                table: "savings_goal_contributions");

            migrationBuilder.DropColumn(
                name: "savings_replenishment_id",
                table: "savings_goal_contributions");

            migrationBuilder.DropColumn(
                name: "linked_savings_goal_id",
                table: "debts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "debit_type",
                table: "savings_goal_contributions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "savings_replenishment_id",
                table: "savings_goal_contributions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "linked_savings_goal_id",
                table: "debts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "savings_replenishments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    savings_goal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_replenished = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    amount_taken = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    auto_debit_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    completed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false),
                    last_debit_at = table.Column<DateOnly>(type: "date", nullable: true),
                    monthly_debit_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_savings_replenishments", x => x.id);
                    table.ForeignKey(
                        name: "FK_savings_replenishments_SavingsGoals_savings_goal_id",
                        column: x => x.savings_goal_id,
                        principalTable: "SavingsGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_savings_replenishments_financial_accounts_source_account_id",
                        column: x => x.source_account_id,
                        principalTable: "financial_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_savings_replenishments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_savings_goal_contributions_replenishment_id",
                table: "savings_goal_contributions",
                column: "savings_replenishment_id",
                filter: "savings_replenishment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_debts_linked_savings_goal_id",
                table: "debts",
                column: "linked_savings_goal_id",
                filter: "linked_savings_goal_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_savings_replenishments_goal_id",
                table: "savings_replenishments",
                column: "savings_goal_id");

            migrationBuilder.CreateIndex(
                name: "idx_savings_replenishments_source_account_id",
                table: "savings_replenishments",
                column: "source_account_id");

            migrationBuilder.CreateIndex(
                name: "idx_savings_replenishments_user_status",
                table: "savings_replenishments",
                columns: new[] { "user_id", "status" },
                filter: "deleted_at IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_savings_goal_contributions_savings_replenishments_savings_r~",
                table: "savings_goal_contributions",
                column: "savings_replenishment_id",
                principalTable: "savings_replenishments",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
