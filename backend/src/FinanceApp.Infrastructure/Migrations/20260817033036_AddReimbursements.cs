using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReimbursements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reimbursements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expense_id = table.Column<Guid>(type: "uuid", nullable: true),
                    destination_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credit_card_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    person = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reimbursements", x => x.id);
                    table.ForeignKey(
                        name: "FK_reimbursements_credit_cards_credit_card_id",
                        column: x => x.credit_card_id,
                        principalTable: "credit_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reimbursements_expenses_expense_id",
                        column: x => x.expense_id,
                        principalTable: "expenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reimbursements_financial_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "financial_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reimbursements_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reimbursements_account_id",
                table: "reimbursements",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_reimbursements_credit_card_id",
                table: "reimbursements",
                column: "credit_card_id");

            migrationBuilder.CreateIndex(
                name: "IX_reimbursements_expense_id",
                table: "reimbursements",
                column: "expense_id");

            migrationBuilder.CreateIndex(
                name: "IX_reimbursements_user_id_date",
                table: "reimbursements",
                columns: new[] { "user_id", "date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_reimbursements_user_id_idempotency_key",
                table: "reimbursements",
                columns: new[] { "user_id", "idempotency_key" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reimbursements");
        }
    }
}
