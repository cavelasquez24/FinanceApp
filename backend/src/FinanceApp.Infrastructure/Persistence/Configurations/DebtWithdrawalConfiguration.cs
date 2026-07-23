using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class DebtWithdrawalConfiguration : IEntityTypeConfiguration<DebtWithdrawal>
{
    public void Configure(EntityTypeBuilder<DebtWithdrawal> builder)
    {
        builder.ToTable("debt_withdrawals");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(w => w.DebtId)
            .HasColumnName("debt_id")
            .IsRequired();

        builder.Property(w => w.WithdrawalDate)
            .HasColumnName("withdrawal_date")
            .IsRequired();

        builder.Property(w => w.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(15,2)")
            .IsRequired();

        builder.Property(w => w.Notes)
            .HasColumnName("notes")
            .IsRequired(false);

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(w => w.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(w => w.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false);

        builder.Ignore(w => w.IsDeleted);

        builder.HasIndex(w => w.DebtId)
            .HasDatabaseName("idx_debt_withdrawals_debt_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasOne(w => w.Debt)
            .WithMany(d => d.Withdrawals)
            .HasForeignKey(w => w.DebtId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}