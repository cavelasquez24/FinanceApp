using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class DebtConfiguration : IEntityTypeConfiguration<Debt>
{
    public void Configure(EntityTypeBuilder<Debt> builder)
    {
        builder.ToTable("debts");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(d => d.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        // Almacena el enum DebtType como string, igual que InvestmentType (ADR-008)
        builder.Property(d => d.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Creditor)
            .HasColumnName("creditor")
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(d => d.OriginalAmount)
            .HasColumnName("original_amount")
            .HasColumnType("numeric(15,2)")
            .IsRequired();

        builder.Property(d => d.CurrentBalance)
            .HasColumnName("current_balance")
            .HasColumnType("numeric(15,2)")
            .IsRequired();

        builder.Property(d => d.InterestRate)
            .HasColumnName("interest_rate")
            .HasColumnType("numeric(5,2)")
            .IsRequired(false);

        builder.Property(d => d.MinimumPayment)
            .HasColumnName("minimum_payment")
            .HasColumnType("numeric(15,2)")
            .IsRequired(false);

        builder.Property(d => d.DueDay)
            .HasColumnName("due_day")
            .IsRequired(false);

        builder.Property(d => d.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(d => d.TargetPayoffDate)
            .HasColumnName("target_payoff_date")
            .IsRequired(false);

        builder.Property(d => d.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(d => d.Notes)
            .HasColumnName("notes")
            .IsRequired(false);

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(d => d.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false);

        builder.Ignore(d => d.IsDeleted);
        builder.Ignore(d => d.AmountPaid);
        builder.Ignore(d => d.PaidPercentage);
        builder.Ignore(d => d.IsPaidOff);

        builder.HasIndex(d => d.UserId)
            .HasDatabaseName("idx_debts_user_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}