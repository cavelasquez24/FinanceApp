using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class ReimbursementConfiguration : IEntityTypeConfiguration<Reimbursement>
{
    public void Configure(EntityTypeBuilder<Reimbursement> builder)
    {
        builder.ToTable("reimbursements");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(r => r.ExpenseId).HasColumnName("expense_id");
        builder.Property(r => r.DestinationType).HasColumnName("destination_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.AccountId).HasColumnName("account_id");
        builder.Property(r => r.CreditCardId).HasColumnName("credit_card_id");
        builder.Property(r => r.Amount).HasColumnName("amount").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(r => r.Date).HasColumnName("date").HasColumnType("date").IsRequired();
        builder.Property(r => r.Person).HasColumnName("person").HasMaxLength(160);
        builder.Property(r => r.Notes).HasColumnName("notes").HasMaxLength(1000);
        builder.Property(r => r.IdempotencyKey).HasColumnName("idempotency_key").IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(r => r.IsDeleted);
        builder.HasIndex(r => new { r.UserId, r.IdempotencyKey }).IsUnique().HasFilter("deleted_at IS NULL");
        builder.HasIndex(r => new { r.UserId, r.Date }).HasFilter("deleted_at IS NULL");
        builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.Expense).WithMany(e => e.Reimbursements).HasForeignKey(r => r.ExpenseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Account).WithMany(a => a.Reimbursements).HasForeignKey(r => r.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.CreditCard).WithMany(c => c.Reimbursements).HasForeignKey(r => r.CreditCardId).OnDelete(DeleteBehavior.Restrict);
    }
}
