using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class SavingsGoalEmergencyFundConfiguration : IEntityTypeConfiguration<SavingsGoal>
{
    public void Configure(EntityTypeBuilder<SavingsGoal> builder)
    {
        builder.Property(g => g.MinimumProtectedAmount).HasColumnType("numeric(15,2)");
        builder.HasIndex(g => new { g.UserId, g.Purpose })
            .IsUnique()
            .HasDatabaseName("ux_savings_goals_single_emergency_fund")
            .HasFilter("\"Purpose\" = 1 AND \"DeletedAt\" IS NULL");
    }
}
