using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDebtWithdrawalAndSavingsGoalLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "linked_savings_goal_id",
                table: "debts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "debt_withdrawals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    debt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    withdrawal_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_debt_withdrawals", x => x.id);
                    table.ForeignKey(
                        name: "FK_debt_withdrawals_debts_debt_id",
                        column: x => x.debt_id,
                        principalTable: "debts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_debts_linked_savings_goal_id",
                table: "debts",
                column: "linked_savings_goal_id",
                filter: "linked_savings_goal_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_debt_withdrawals_debt_id",
                table: "debt_withdrawals",
                column: "debt_id",
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "debt_withdrawals");

            migrationBuilder.DropIndex(
                name: "idx_debts_linked_savings_goal_id",
                table: "debts");

            migrationBuilder.DropColumn(
                name: "linked_savings_goal_id",
                table: "debts");
        }
    }
}
