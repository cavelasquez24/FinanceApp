using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class EmergencyFundRestorationConfiguration : IEntityTypeConfiguration<EmergencyFundRestoration>
{
    public void Configure(EntityTypeBuilder<EmergencyFundRestoration> builder)
    {
        builder.ToTable("emergency_fund_restorations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(r => r.SavingsGoalId).HasColumnName("savings_goal_id").IsRequired();
        builder.Property(r => r.SourceWithdrawalId).HasColumnName("source_withdrawal_id").IsRequired();
        builder.Property(r => r.LinkedExpenseId).HasColumnName("linked_expense_id");
        builder.Property(r => r.Description).HasColumnName("description").HasMaxLength(200).IsRequired();
        builder.Property(r => r.AcquisitionDate).HasColumnName("acquisition_date").IsRequired();
        builder.Property(r => r.OriginalAmount).HasColumnName("original_amount").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(r => r.RestoredAmount).HasColumnName("restored_amount").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(r => r.TargetRestorationDate).HasColumnName("target_restoration_date").IsRequired();
        builder.Property(r => r.ScheduledContributionAmount).HasColumnName("scheduled_contribution_amount").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(r => r.NextScheduledDate).HasColumnName("next_scheduled_date").IsRequired();
        builder.Property(r => r.ScheduledSourceAccountId).HasColumnName("scheduled_source_account_id");
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.CompletedDate).HasColumnName("completed_date");
        builder.Property(r => r.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(r => r.IsDeleted);
        builder.Ignore(r => r.OutstandingAmount);
        builder.Ignore(r => r.EstimatedCompletionDate);
        builder.Ignore(r => r.IsOverdue);

        builder.HasOne(r => r.User).WithMany(u => u.EmergencyFundRestorations)
            .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.SavingsGoal).WithMany(g => g.Restorations)
            .HasForeignKey(r => r.SavingsGoalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.SourceWithdrawal).WithOne()
            .HasForeignKey<EmergencyFundRestoration>(r => r.SourceWithdrawalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.LinkedExpense).WithOne()
            .HasForeignKey<EmergencyFundRestoration>(r => r.LinkedExpenseId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.SourceWithdrawalId).IsUnique();
        builder.HasIndex(r => r.LinkedExpenseId);
        builder.HasIndex(r => new { r.UserId, r.Status, r.NextScheduledDate });
    }
}
