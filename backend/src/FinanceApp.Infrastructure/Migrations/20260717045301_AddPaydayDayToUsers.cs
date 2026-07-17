using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaydayDayToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "payday_day",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_payday_day",
                table: "users",
                sql: "payday_day IS NULL OR (payday_day >= 1 AND payday_day <= 31)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_users_payday_day",
                table: "users");

            migrationBuilder.DropColumn(
                name: "payday_day",
                table: "users");
        }
    }
}
