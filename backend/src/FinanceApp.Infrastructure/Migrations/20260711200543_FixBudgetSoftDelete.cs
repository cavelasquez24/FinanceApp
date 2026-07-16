using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixBudgetSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_budget_periods_user_month_year",
                table: "budget_periods");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "budget_periods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_budget_periods_user_month_year",
                table: "budget_periods",
                columns: new[] { "user_id", "month", "year" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_budget_periods_user_month_year",
                table: "budget_periods");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "budget_periods");

            migrationBuilder.CreateIndex(
                name: "idx_budget_periods_user_month_year",
                table: "budget_periods",
                columns: new[] { "user_id", "month", "year" },
                unique: true);
        }
    }
}
