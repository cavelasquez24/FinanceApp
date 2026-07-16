using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class BudgetPeriodConfiguration : IEntityTypeConfiguration<BudgetPeriod>
{
    public void Configure(EntityTypeBuilder<BudgetPeriod> builder)
    {
        builder.ToTable("budget_periods");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(b => b.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(b => b.Month)
            .HasColumnName("month")
            .IsRequired();

        builder.Property(b => b.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(b => b.TotalLimit)
            .HasColumnName("total_limit")
            .HasColumnType("numeric(15,2)")
            .IsRequired(false);

        builder.Property(b => b.Notes)
            .HasColumnName("notes")
            .IsRequired(false);

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(b => b.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.Ignore(b => b.IsDeleted);
        builder.Property(b => b.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.HasIndex(b => new { b.UserId, b.Month, b.Year })
            .IsUnique()
            .HasDatabaseName("idx_budget_periods_user_month_year")
            .HasFilter("deleted_at IS NULL");

        builder.HasMany(b => b.BudgetCategories)
            .WithOne(bc => bc.BudgetPeriod)
            .HasForeignKey(bc => bc.BudgetPeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(b => b.DeletedAt == null);
    }
}