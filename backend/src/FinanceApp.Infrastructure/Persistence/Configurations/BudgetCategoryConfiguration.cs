using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class BudgetCategoryConfiguration : IEntityTypeConfiguration<BudgetCategory>
{
    public void Configure(EntityTypeBuilder<BudgetCategory> builder)
    {
        // BudgetPeriod is required and has a soft-delete filter. Applying the
        // same condition to the dependent prevents orphaned results and removes
        // EF Core warning 10622.
        builder.HasQueryFilter(category => category.BudgetPeriod.DeletedAt == null);
    }
}
