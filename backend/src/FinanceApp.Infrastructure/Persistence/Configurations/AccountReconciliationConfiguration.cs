using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class AccountReconciliationConfiguration : IEntityTypeConfiguration<AccountReconciliation>
{
    public void Configure(EntityTypeBuilder<AccountReconciliation> builder)
    {
        builder.ToTable("account_reconciliations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(r => r.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(r => r.ReconciliationDate).HasColumnName("reconciliation_date").IsRequired();
        builder.Property(r => r.ExpectedBalance).HasColumnName("expected_balance").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(r => r.ActualBalance).HasColumnName("actual_balance").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(r => r.Difference).HasColumnName("difference").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(r => r.AdjustmentTransactionId).HasColumnName("adjustment_transaction_id").IsRequired(false);
        builder.Property(r => r.Notes).HasColumnName("notes").HasMaxLength(500).IsRequired(false);
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at").IsRequired(false);
        builder.Ignore(r => r.IsDeleted);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Account)
            .WithMany()
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.AdjustmentTransaction)
            .WithMany()
            .HasForeignKey(r => r.AdjustmentTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.AccountId, r.ReconciliationDate })
            .HasDatabaseName("idx_account_reconciliations_account_date");

        builder.HasIndex(r => new { r.UserId, r.AccountId })
            .HasDatabaseName("idx_account_reconciliations_user_account");
    }
}
