using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.Property(t => t.NormalizedName).HasColumnName("normalized_name").HasMaxLength(50).IsRequired();
        builder.Property(t => t.Color).HasColumnName("color").HasMaxLength(7);
        builder.Property(t => t.MergedIntoTagId).HasColumnName("merged_into_tag_id");
        builder.Property(t => t.LastUsedAt).HasColumnName("last_used_at");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(t => t.IsDeleted);

        builder.HasIndex(t => new { t.UserId, t.NormalizedName })
            .IsUnique().HasDatabaseName("idx_tags_user_normalized_name")
            .HasFilter("deleted_at IS NULL");
        builder.HasIndex(t => new { t.UserId, t.LastUsedAt })
            .HasDatabaseName("idx_tags_user_last_used")
            .HasFilter("deleted_at IS NULL");

        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.MergedIntoTag).WithMany().HasForeignKey(t => t.MergedIntoTagId).OnDelete(DeleteBehavior.Restrict);
    }
}
