using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class SavingsReplenishmentConfiguration : IEntityTypeConfiguration<SavingsReplenishment>
{
    public void Configure(EntityTypeBuilder<SavingsReplenishment> builder)
    {
        builder.ToTable("savings_replenishments");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(r => r.SavingsGoalId).HasColumnName("savings_goal_id").IsRequired();
        builder.Property(r => r.SourceAccountId).HasColumnName("source_account_id").IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(r => r.Notes).HasColumnName("notes").HasMaxLength(500);

        builder.Property(r => r.AmountTaken).HasColumnName("amount_taken").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(r => r.AmountReplenished).HasColumnName("amount_replenished").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(r => r.MonthlyDebitAmount).HasColumnName("monthly_debit_amount").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(r => r.AutoDebitEnabled).HasColumnName("auto_debit_enabled").IsRequired();
        builder.Property(r => r.IsPaused).HasColumnName("is_paused").IsRequired();

        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.CompletedAt).HasColumnName("completed_at");
        builder.Property(r => r.LastDebitAt).HasColumnName("last_debit_at");

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(r => r.IsDeleted);
        builder.Ignore(r => r.PendingAmount);

        builder.HasOne(r => r.User).WithMany(u => u.SavingsReplenishments)
            .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.SavingsGoal).WithMany(g => g.Replenishments)
            .HasForeignKey(r => r.SavingsGoalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.SourceAccount).WithMany()
            .HasForeignKey(r => r.SourceAccountId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.SavingsGoalId).HasDatabaseName("idx_savings_replenishments_goal_id");
        builder.HasIndex(r => r.SourceAccountId).HasDatabaseName("idx_savings_replenishments_source_account_id");
        builder.HasIndex(r => new { r.UserId, r.Status })
            .HasDatabaseName("idx_savings_replenishments_user_status")
            .HasFilter("deleted_at IS NULL");
    }
}
