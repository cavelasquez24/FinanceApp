using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class CreditCardTransactionConfiguration : IEntityTypeConfiguration<CreditCardTransaction>
{
    public void Configure(EntityTypeBuilder<CreditCardTransaction> builder)
    {
        builder.ToTable("credit_card_transactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(t => t.CreditCardId).HasColumnName("credit_card_id").IsRequired();
        builder.Property(t => t.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(t => t.Amount).HasColumnName("amount").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(t => t.Date).HasColumnName("date").HasColumnType("date").IsRequired();
        builder.Property(t => t.Description).HasColumnName("description").HasMaxLength(300).IsRequired();
        builder.Property(t => t.SourceType).HasColumnName("source_type").HasMaxLength(50).IsRequired();
        builder.Property(t => t.SourceId).HasColumnName("source_id").IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(t => t.IsDeleted);

        builder.HasIndex(t => new { t.UserId, t.SourceType, t.SourceId }).IsUnique();
        builder.HasIndex(t => new { t.CreditCardId, t.Date });
        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.CreditCard).WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CreditCardId).OnDelete(DeleteBehavior.Cascade);
    }
}
