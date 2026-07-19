using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class InvestmentContributionConfiguration : IEntityTypeConfiguration<InvestmentContribution>
{
    public void Configure(EntityTypeBuilder<InvestmentContribution> builder)
    {
        builder.ToTable("investment_contributions");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.InvestmentId)
            .HasColumnName("investment_id")
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

        builder.HasIndex(c => c.InvestmentId)
            .HasDatabaseName("idx_investment_contributions_investment_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasOne(c => c.Investment)
            .WithMany(i => i.Contributions)
            .HasForeignKey(c => c.InvestmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
