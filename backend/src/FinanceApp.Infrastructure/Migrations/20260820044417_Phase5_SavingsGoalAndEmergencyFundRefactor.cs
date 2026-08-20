using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase5_SavingsGoalAndEmergencyFundRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SavingsAccountId",
                table: "SavingsGoals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "destination_account_id",
                table: "savings_goal_withdrawals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_account_id",
                table: "savings_goal_contributions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavingsGoals_SavingsAccountId",
                table: "SavingsGoals",
                column: "SavingsAccountId",
                filter: "\"SavingsAccountId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_savings_goal_withdrawals_destination_account_id",
                table: "savings_goal_withdrawals",
                column: "destination_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_savings_goal_contributions_source_account_id",
                table: "savings_goal_contributions",
                column: "source_account_id");

            migrationBuilder.AddForeignKey(
                name: "FK_savings_goal_contributions_financial_accounts_source_accoun~",
                table: "savings_goal_contributions",
                column: "source_account_id",
                principalTable: "financial_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_savings_goal_withdrawals_financial_accounts_destination_acc~",
                table: "savings_goal_withdrawals",
                column: "destination_account_id",
                principalTable: "financial_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SavingsGoals_financial_accounts_SavingsAccountId",
                table: "SavingsGoals",
                column: "SavingsAccountId",
                principalTable: "financial_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_savings_goal_contributions_financial_accounts_source_accoun~",
                table: "savings_goal_contributions");

            migrationBuilder.DropForeignKey(
                name: "FK_savings_goal_withdrawals_financial_accounts_destination_acc~",
                table: "savings_goal_withdrawals");

            migrationBuilder.DropForeignKey(
                name: "FK_SavingsGoals_financial_accounts_SavingsAccountId",
                table: "SavingsGoals");

            migrationBuilder.DropIndex(
                name: "IX_SavingsGoals_SavingsAccountId",
                table: "SavingsGoals");

            migrationBuilder.DropIndex(
                name: "IX_savings_goal_withdrawals_destination_account_id",
                table: "savings_goal_withdrawals");

            migrationBuilder.DropIndex(
                name: "IX_savings_goal_contributions_source_account_id",
                table: "savings_goal_contributions");

            migrationBuilder.DropColumn(
                name: "SavingsAccountId",
                table: "SavingsGoals");

            migrationBuilder.DropColumn(
                name: "destination_account_id",
                table: "savings_goal_withdrawals");

            migrationBuilder.DropColumn(
                name: "source_account_id",
                table: "savings_goal_contributions");
        }
    }
}
