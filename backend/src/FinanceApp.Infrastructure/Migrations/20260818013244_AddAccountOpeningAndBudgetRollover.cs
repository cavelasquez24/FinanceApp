using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountOpeningAndBudgetRollover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "opening_balance",
                table: "financial_accounts",
                type: "numeric(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "opening_date",
                table: "financial_accounts",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "budget_rollover_amount",
                table: "budget_periods",
                type: "numeric(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateOnly>(
                name: "budget_rollover_date",
                table: "budget_periods",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "budget_rollover_idempotency_key",
                table: "budget_periods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "budget_rollover_note",
                table: "budget_periods",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "opening_balance",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "opening_date",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "budget_rollover_amount",
                table: "budget_periods");

            migrationBuilder.DropColumn(
                name: "budget_rollover_date",
                table: "budget_periods");

            migrationBuilder.DropColumn(
                name: "budget_rollover_idempotency_key",
                table: "budget_periods");

            migrationBuilder.DropColumn(
                name: "budget_rollover_note",
                table: "budget_periods");
        }
    }
}
