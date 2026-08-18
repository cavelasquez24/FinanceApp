using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class CreditCardPaymentConfiguration : IEntityTypeConfiguration<CreditCardPayment>
{
    public void Configure(EntityTypeBuilder<CreditCardPayment> builder)
    {
        builder.ToTable("credit_card_payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(p => p.CreditCardId).HasColumnName("credit_card_id").IsRequired();
        builder.Property(p => p.SourceAccountId).HasColumnName("source_account_id").IsRequired();
        builder.Property(p => p.PrincipalAmount).HasColumnName("principal_amount").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(p => p.CommissionAmount).HasColumnName("commission_amount").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(p => p.CardBalanceAfter).HasColumnName("card_balance_after").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(p => p.CommissionExpenseId).HasColumnName("commission_expense_id");
        builder.Property(p => p.PaymentDate).HasColumnName("payment_date").HasColumnType("date").IsRequired();
        builder.Property(p => p.Notes).HasColumnName("notes").HasMaxLength(1000);
        builder.Property(p => p.IdempotencyKey).HasColumnName("idempotency_key").IsRequired();
        builder.Property(p => p.VoidedAt).HasColumnName("voided_at");
        builder.Property(p => p.VoidReason).HasColumnName("void_reason").HasMaxLength(500);
        builder.Property(p => p.VoidIdempotencyKey).HasColumnName("void_idempotency_key");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(p => p.IsDeleted);
        builder.Ignore(p => p.IsVoided);

        builder.HasIndex(p => new { p.CreditCardId, p.IdempotencyKey }).IsUnique();
        builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.CreditCard).WithMany(c => c.Payments)
            .HasForeignKey(p => p.CreditCardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.SourceAccount).WithMany()
            .HasForeignKey(p => p.SourceAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.CommissionExpense).WithMany()
            .HasForeignKey(p => p.CommissionExpenseId).OnDelete(DeleteBehavior.SetNull);
    }
}
