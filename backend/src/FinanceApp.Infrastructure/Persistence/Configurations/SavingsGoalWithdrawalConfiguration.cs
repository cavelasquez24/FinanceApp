using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class SavingsGoalWithdrawalConfiguration : IEntityTypeConfiguration<SavingsGoalWithdrawal>
{
    public void Configure(EntityTypeBuilder<SavingsGoalWithdrawal> builder)
    {
        builder.ToTable("savings_goal_withdrawals");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(w => w.SavingsGoalId)
            .HasColumnName("savings_goal_id")
            .IsRequired();

        builder.Property(w => w.WithdrawalDate)
            .HasColumnName("withdrawal_date")
            .IsRequired();

        builder.Property(w => w.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(15,2)")
            .IsRequired();

        // FK opcional, sin navegación obligatoria: Expense pertenece a otro
        // agregado. Solo se puebla cuando Reason = Consumed.
        builder.Property(w => w.LinkedExpenseId)
            .HasColumnName("linked_expense_id")
            .IsRequired(false);

        // Enum como string, snake_case en BD (ADR-008), mismo patrón que DebtType
        builder.Property(w => w.Reason)
            .HasColumnName("reason")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(w => w.Notes)
            .HasColumnName("notes")
            .IsRequired(false);

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(w => w.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(w => w.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false);

        builder.Ignore(w => w.IsDeleted);

        builder.HasIndex(w => w.SavingsGoalId)
            .HasDatabaseName("idx_savings_goal_withdrawals_goal_id")
            .HasFilter("deleted_at IS NULL");

        // Índice para futura reconciliación (sección 7 del spec):
        // localizar withdrawals ya vinculados a un Expense
        builder.HasIndex(w => w.LinkedExpenseId)
            .HasDatabaseName("idx_savings_goal_withdrawals_linked_expense_id")
            .HasFilter("linked_expense_id IS NOT NULL AND deleted_at IS NULL");

        builder.HasOne(w => w.SavingsGoal)
            .WithMany(s => s.Withdrawals)
            .HasForeignKey(w => w.SavingsGoalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
