using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmergencyFundRestorations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinimumProtectedAmount",
                table: "SavingsGoals",
                type: "numeric(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Purpose",
                table: "SavingsGoals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "emergency_fund_restoration_id",
                table: "savings_goal_contributions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "emergency_fund_restorations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    savings_goal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_withdrawal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linked_expense_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    acquisition_date = table.Column<DateOnly>(type: "date", nullable: false),
                    original_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    restored_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    target_restoration_date = table.Column<DateOnly>(type: "date", nullable: false),
                    scheduled_contribution_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    next_scheduled_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    completed_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emergency_fund_restorations", x => x.id);
                    table.ForeignKey(
                        name: "FK_emergency_fund_restorations_SavingsGoals_savings_goal_id",
                        column: x => x.savings_goal_id,
                        principalTable: "SavingsGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_emergency_fund_restorations_expenses_linked_expense_id",
                        column: x => x.linked_expense_id,
                        principalTable: "expenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_emergency_fund_restorations_savings_goal_withdrawals_source~",
                        column: x => x.source_withdrawal_id,
                        principalTable: "savings_goal_withdrawals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_emergency_fund_restorations_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_savings_goals_single_emergency_fund",
                table: "SavingsGoals",
                column: "Purpose",
                unique: true,
                filter: "\"Purpose\" = 1 AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_savings_goal_contributions_emergency_fund_restoration_id",
                table: "savings_goal_contributions",
                column: "emergency_fund_restoration_id");

            migrationBuilder.CreateIndex(
                name: "IX_emergency_fund_restorations_linked_expense_id",
                table: "emergency_fund_restorations",
                column: "linked_expense_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_emergency_fund_restorations_savings_goal_id",
                table: "emergency_fund_restorations",
                column: "savings_goal_id");

            migrationBuilder.CreateIndex(
                name: "IX_emergency_fund_restorations_source_withdrawal_id",
                table: "emergency_fund_restorations",
                column: "source_withdrawal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_emergency_fund_restorations_user_id_status_next_scheduled_d~",
                table: "emergency_fund_restorations",
                columns: new[] { "user_id", "status", "next_scheduled_date" });

            migrationBuilder.AddForeignKey(
                name: "FK_savings_goal_contributions_emergency_fund_restorations_emer~",
                table: "savings_goal_contributions",
                column: "emergency_fund_restoration_id",
                principalTable: "emergency_fund_restorations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_savings_goal_contributions_emergency_fund_restorations_emer~",
                table: "savings_goal_contributions");

            migrationBuilder.DropTable(
                name: "emergency_fund_restorations");

            migrationBuilder.DropIndex(
                name: "ux_savings_goals_single_emergency_fund",
                table: "SavingsGoals");

            migrationBuilder.DropIndex(
                name: "IX_savings_goal_contributions_emergency_fund_restoration_id",
                table: "savings_goal_contributions");

            migrationBuilder.DropColumn(
                name: "MinimumProtectedAmount",
                table: "SavingsGoals");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "SavingsGoals");

            migrationBuilder.DropColumn(
                name: "emergency_fund_restoration_id",
                table: "savings_goal_contributions");
        }
    }
}
