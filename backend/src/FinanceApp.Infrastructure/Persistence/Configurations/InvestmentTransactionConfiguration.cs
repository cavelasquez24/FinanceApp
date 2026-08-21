using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class InvestmentTransactionConfiguration : IEntityTypeConfiguration<InvestmentTransaction>
{
    public void Configure(EntityTypeBuilder<InvestmentTransaction> builder)
    {
        builder.ToTable("investment_transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.InvestmentId)
            .HasColumnName("investment_id")
            .IsRequired();

        builder.Property(t => t.TransactionType)
            .HasColumnName("transaction_type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(t => t.TransactionDate)
            .HasColumnName("transaction_date")
            .IsRequired();

        builder.Property(t => t.IsHistorical)
            .HasColumnName("is_historical")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(t => t.GroupId)
            .HasColumnName("group_id")
            .IsRequired(false);

        builder.Property(t => t.ReversalOf)
            .HasColumnName("reversal_of")
            .IsRequired(false);

        builder.Property(t => t.Notes)
            .HasColumnName("notes")
            .IsRequired(false);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(t => t.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false);

        builder.Ignore(t => t.IsDeleted);

        builder.HasIndex(t => new { t.InvestmentId, t.TransactionDate })
            .HasDatabaseName("idx_investment_transactions_investment_id_date");

        builder.HasOne(t => t.Investment)
            .WithMany(i => i.Transactions)
            .HasForeignKey(t => t.InvestmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<InvestmentTransaction>()
            .WithMany()
            .HasForeignKey(t => t.ReversalOf)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
