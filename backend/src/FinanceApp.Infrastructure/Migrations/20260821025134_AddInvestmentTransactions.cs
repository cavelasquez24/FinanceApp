using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "investment_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    investment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_historical = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reversal_of = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_investment_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_investment_transactions_Investments_investment_id",
                        column: x => x.investment_id,
                        principalTable: "Investments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_investment_transactions_investment_transactions_reversal_of",
                        column: x => x.reversal_of,
                        principalTable: "investment_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_investment_transactions_investment_id_date",
                table: "investment_transactions",
                columns: new[] { "investment_id", "transaction_date" });

            migrationBuilder.CreateIndex(
                name: "IX_investment_transactions_reversal_of",
                table: "investment_transactions",
                column: "reversal_of");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "investment_transactions");
        }
    }
}
