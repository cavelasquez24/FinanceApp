using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class IncomeConfiguration : IEntityTypeConfiguration<Income>
{
    public void Configure(EntityTypeBuilder<Income> builder)
    {
        builder.ToTable("incomes");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(i => i.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(i => i.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();

        builder.Property(i => i.AccountId)
            .HasColumnName("account_id")
            .IsRequired(false);

        // NUMERIC(15,2) en PostgreSQL = precisión exacta para dinero
        builder.Property(i => i.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(15,2)")
            .IsRequired();

        builder.Property(i => i.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired(false);
        builder.Property(i => i.AssignedCycleStart)
            .HasColumnName("assigned_cycle_start")
            .IsRequired(false);


        builder.Property(i => i.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.Property(i => i.Source)
            .HasColumnName("source")
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(i => i.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false);

        builder.Ignore(i => i.IsDeleted);

        // Índice compuesto: la consulta más común filtra por usuario Y fecha
        builder.HasIndex(i => new { i.UserId, i.Date })
            .HasDatabaseName("idx_incomes_user_id_date")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(i => i.CategoryId)
            .HasDatabaseName("idx_incomes_category_id")
            .HasFilter("deleted_at IS NULL");

        // Relación con Category (sin cascade porque la categoría
        // no debe borrarse si tiene ingresos asociados)
        builder.HasOne(i => i.Category)
            .WithMany(c => c.Incomes)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Account)
            .WithMany()
            .HasForeignKey(i => i.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}