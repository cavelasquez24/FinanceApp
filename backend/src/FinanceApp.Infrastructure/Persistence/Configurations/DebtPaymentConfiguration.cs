using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class DebtPaymentConfiguration : IEntityTypeConfiguration<DebtPayment>
{
    public void Configure(EntityTypeBuilder<DebtPayment> builder)
    {
        builder.ToTable("debt_payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.DebtId)
            .HasColumnName("debt_id")
            .IsRequired();

        builder.Property(p => p.PaymentDate)
            .HasColumnName("payment_date")
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(15,2)")
            .IsRequired();

        builder.Property(p => p.PrincipalAmount)
            .HasColumnName("principal_amount")
            .HasColumnType("numeric(15,2)")
            .IsRequired();

        builder.Property(p => p.InterestAmount)
            .HasColumnName("interest_amount")
            .HasColumnType("numeric(15,2)")
            .HasDefaultValue(0m);

        builder.Property(p => p.Notes)
            .HasColumnName("notes")
            .IsRequired(false);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false);

        builder.Ignore(p => p.IsDeleted);

        builder.HasIndex(p => new { p.DebtId, p.PaymentDate })
            .HasDatabaseName("idx_debt_payments_debt_id_date")
            .HasFilter("deleted_at IS NULL");

        builder.HasOne(p => p.Debt)
            .WithMany(d => d.Payments)
            .HasForeignKey(p => p.DebtId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}