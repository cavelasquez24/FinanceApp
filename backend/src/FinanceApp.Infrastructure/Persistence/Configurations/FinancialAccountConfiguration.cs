using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
{
    public void Configure(EntityTypeBuilder<FinancialAccount> builder)
    {
        builder.ToTable("financial_accounts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(a => a.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.CurrentBalance).HasColumnName("current_balance").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(a => a.IsDefault).HasColumnName("is_default").IsRequired();
        builder.Property(a => a.IsSystem).HasColumnName("is_system").IsRequired();
        builder.Property(a => a.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(a => a.IsDeleted);

        builder.HasIndex(a => new { a.UserId, a.Type });
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
