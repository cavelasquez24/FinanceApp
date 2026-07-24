using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();

        builder.Property(e => e.AccountId)
            .HasColumnName("account_id")
            .IsRequired(false);

        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(15,2)")
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(e => e.Date)
            .HasColumnName("date")
            .IsRequired();

        // Almacena el enum PaymentMethod como string
        builder.Property(e => e.PaymentMethod)
            .HasColumnName("payment_method")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PaymentMethod.Cash);

        builder.Property(e => e.IsRecurring)
            .HasColumnName("is_recurring")
            .HasDefaultValue(false);

        // Almacena el enum RecurrenceType como string, puede ser NULL
        builder.Property(e => e.RecurrenceType)
            .HasColumnName("recurrence_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(e => e.Notes)
            .HasColumnName("notes")
            .IsRequired(false);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false);

        builder.Ignore(e => e.IsDeleted);

        builder.HasIndex(e => new { e.UserId, e.Date })
            .HasDatabaseName("idx_expenses_user_id_date")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(e => e.CategoryId)
            .HasDatabaseName("idx_expenses_category_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}