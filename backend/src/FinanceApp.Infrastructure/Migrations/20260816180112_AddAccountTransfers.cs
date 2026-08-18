using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "transfer_id",
                table: "account_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_transactions_user_id_transfer_id",
                table: "account_transactions",
                columns: new[] { "user_id", "transfer_id" },
                filter: "transfer_id IS NOT NULL AND deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_account_transactions_user_id_transfer_id",
                table: "account_transactions");

            migrationBuilder.DropColumn(
                name: "transfer_id",
                table: "account_transactions");
        }
    }
}
