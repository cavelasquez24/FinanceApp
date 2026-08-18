using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.ToTable("credit_cards");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(c => c.CurrentBalance).HasColumnName("current_balance").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(c => c.CreditLimit).HasColumnName("credit_limit").HasColumnType("numeric(15,2)");
        builder.Property(c => c.ClosingDay).HasColumnName("closing_day").IsRequired();
        builder.Property(c => c.DueDay).HasColumnName("due_day").IsRequired();
        builder.Property(c => c.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(c => c.Notes).HasColumnName("notes").HasMaxLength(1000);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(c => c.IsDeleted);

        builder.HasIndex(c => new { c.UserId, c.Name });
        builder.HasOne(c => c.User).WithMany(u => u.CreditCards)
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
