using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// IEntityTypeConfiguration<User> le dice a EF Core cómo
/// mapear la entidad User a la tabla users en PostgreSQL.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // ── Tabla ──────────────────────────────────────────────────────
        builder.ToTable("users");

        // ── Clave primaria ─────────────────────────────────────────────
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()"); // PostgreSQL genera el UUID

        // ── Propiedades ────────────────────────────────────────────────
        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired()
            .HasDefaultValue("USD");

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false); // NULL = activo

        // ── Propiedades ignoradas (no se almacenan en BD) ──────────────
        builder.Ignore(u => u.FullName);
        builder.Ignore(u => u.IsDeleted);

        // ── Índices ────────────────────────────────────────────────────
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("idx_users_email")
            .HasFilter("deleted_at IS NULL"); // índice parcial: solo usuarios activos

        // ── Relaciones ─────────────────────────────────────────────────
        builder.HasMany(u => u.Incomes)
            .WithOne(i => i.User)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade); // si se borra el usuario, se borran sus ingresos

        builder.HasMany(u => u.Expenses)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Categories)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.BudgetPeriods)
            .WithOne(b => b.User)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Investments)
            .WithOne(i => i.User)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.SavingsGoals)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}