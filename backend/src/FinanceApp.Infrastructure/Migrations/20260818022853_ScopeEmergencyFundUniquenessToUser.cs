using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ScopeEmergencyFundUniquenessToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavingsGoals_UserId",
                table: "SavingsGoals");

            migrationBuilder.DropIndex(
                name: "ux_savings_goals_single_emergency_fund",
                table: "SavingsGoals");

            migrationBuilder.CreateIndex(
                name: "ux_savings_goals_single_emergency_fund",
                table: "SavingsGoals",
                columns: new[] { "UserId", "Purpose" },
                unique: true,
                filter: "\"Purpose\" = 1 AND \"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_savings_goals_single_emergency_fund",
                table: "SavingsGoals");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsGoals_UserId",
                table: "SavingsGoals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ux_savings_goals_single_emergency_fund",
                table: "SavingsGoals",
                column: "Purpose",
                unique: true,
                filter: "\"Purpose\" = 1 AND \"DeletedAt\" IS NULL");
        }
    }
}
