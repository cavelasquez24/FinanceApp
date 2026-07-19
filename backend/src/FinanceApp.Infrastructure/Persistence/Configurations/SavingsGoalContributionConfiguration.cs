using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class SavingsGoalContributionConfiguration : IEntityTypeConfiguration<SavingsGoalContribution>
{
    public void Configure(EntityTypeBuilder<SavingsGoalContribution> builder)
    {
        builder.ToTable("savings_goal_contributions");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.SavingsGoalId)
            .HasColumnName("savings_goal_id")
            .IsRequired();

        builder.Property(c => c.ContributionDate)
            .HasColumnName("contribution_date")
            .IsRequired();

        builder.Property(c => c.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(15,2)")
            .IsRequired();

        builder.Property(c => c.Notes)
            .HasColumnName("notes")
            .IsRequired(false);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(c => c.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false);

        builder.Ignore(c => c.IsDeleted);

        builder.HasIndex(c => c.SavingsGoalId)
            .HasDatabaseName("idx_savings_goal_contributions_goal_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasOne(c => c.SavingsGoal)
            .WithMany(s => s.Contributions)
            .HasForeignKey(c => c.SavingsGoalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
