using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "credit_card_id",
                table: "expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "idempotency_key",
                table: "expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "credit_cards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    current_balance = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    credit_limit = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    closing_day = table.Column<int>(type: "integer", nullable: false),
                    due_day = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_cards", x => x.id);
                    table.ForeignKey(
                        name: "FK_credit_cards_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "credit_card_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    principal_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    card_balance_after = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    commission_expense_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    voided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    void_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    void_idempotency_key = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_card_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_credit_card_payments_credit_cards_credit_card_id",
                        column: x => x.credit_card_id,
                        principalTable: "credit_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_credit_card_payments_expenses_commission_expense_id",
                        column: x => x.commission_expense_id,
                        principalTable: "expenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_credit_card_payments_financial_accounts_source_account_id",
                        column: x => x.source_account_id,
                        principalTable: "financial_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_card_payments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "credit_card_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_card_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_credit_card_transactions_credit_cards_credit_card_id",
                        column: x => x.credit_card_id,
                        principalTable: "credit_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_credit_card_transactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_expenses_credit_card_id",
                table: "expenses",
                column: "credit_card_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_user_id_idempotency_key",
                table: "expenses",
                columns: new[] { "user_id", "idempotency_key" },
                unique: true,
                filter: "deleted_at IS NULL AND idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_payments_commission_expense_id",
                table: "credit_card_payments",
                column: "commission_expense_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_payments_credit_card_id_idempotency_key",
                table: "credit_card_payments",
                columns: new[] { "credit_card_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_payments_source_account_id",
                table: "credit_card_payments",
                column: "source_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_payments_user_id",
                table: "credit_card_payments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_transactions_credit_card_id_date",
                table: "credit_card_transactions",
                columns: new[] { "credit_card_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_transactions_user_id_source_type_source_id",
                table: "credit_card_transactions",
                columns: new[] { "user_id", "source_type", "source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_credit_cards_user_id_name",
                table: "credit_cards",
                columns: new[] { "user_id", "name" });

            migrationBuilder.AddForeignKey(
                name: "FK_expenses_credit_cards_credit_card_id",
                table: "expenses",
                column: "credit_card_id",
                principalTable: "credit_cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expenses_credit_cards_credit_card_id",
                table: "expenses");

            migrationBuilder.DropTable(
                name: "credit_card_payments");

            migrationBuilder.DropTable(
                name: "credit_card_transactions");

            migrationBuilder.DropTable(
                name: "credit_cards");

            migrationBuilder.DropIndex(
                name: "IX_expenses_credit_card_id",
                table: "expenses");

            migrationBuilder.DropIndex(
                name: "IX_expenses_user_id_idempotency_key",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "credit_card_id",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "expenses");
        }
    }
}
