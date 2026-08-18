using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSavingsGoalOperationCorrelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "operation_id",
                table: "savings_goal_withdrawals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "operation_id",
                table: "savings_goal_contributions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_savings_goal_withdrawals_operation_id",
                table: "savings_goal_withdrawals",
                column: "operation_id",
                filter: "operation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_savings_goal_contributions_operation_id",
                table: "savings_goal_contributions",
                column: "operation_id",
                filter: "operation_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_savings_goal_withdrawals_operation_id",
                table: "savings_goal_withdrawals");

            migrationBuilder.DropIndex(
                name: "idx_savings_goal_contributions_operation_id",
                table: "savings_goal_contributions");

            migrationBuilder.DropColumn(
                name: "operation_id",
                table: "savings_goal_withdrawals");

            migrationBuilder.DropColumn(
                name: "operation_id",
                table: "savings_goal_contributions");
        }
    }
}
