using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNetWorthSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "net_worth_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_assets = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_liabilities = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    net_worth = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    cash_accounts = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    savings_accounts = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    investment_positions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    debt_liabilities = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    credit_card_liabilities = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_net_worth_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_net_worth_snapshots_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_net_worth_snapshots_user_date",
                table: "net_worth_snapshots",
                columns: new[] { "user_id", "snapshot_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "net_worth_snapshots");
        }
    }
}
