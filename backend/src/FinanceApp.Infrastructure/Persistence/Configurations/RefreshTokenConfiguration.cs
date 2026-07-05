using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(r => r.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(r => r.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(r => r.IsRevoked)
            .HasColumnName("is_revoked")
            .HasDefaultValue(false);

        builder.Property(r => r.CreatedByIp)
            .HasColumnName("created_by_ip")
            .HasMaxLength(45)
            .IsRequired(false);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        // Propiedades calculadas — no se almacenan en BD
        builder.Ignore(r => r.IsExpired);
        builder.Ignore(r => r.IsActive);

        builder.HasIndex(r => r.TokenHash)
            .IsUnique()
            .HasDatabaseName("idx_refresh_tokens_hash");

        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("idx_refresh_tokens_user_id");
    }
}