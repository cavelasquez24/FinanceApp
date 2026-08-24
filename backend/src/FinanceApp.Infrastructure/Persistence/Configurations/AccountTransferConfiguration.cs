using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class AccountTransferConfiguration : IEntityTypeConfiguration<AccountTransfer>
{
    public void Configure(EntityTypeBuilder<AccountTransfer> builder)
    {
        builder.ToTable("account_transfers");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(t => t.FromAccountId).HasColumnName("from_account_id").IsRequired();
        builder.Property(t => t.ToAccountId).HasColumnName("to_account_id").IsRequired();
        builder.Property(t => t.Amount).HasColumnName("amount").HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(t => t.TransferDate).HasColumnName("transfer_date").IsRequired();
        builder.Property(t => t.Description).HasColumnName("description").HasMaxLength(300);
        builder.Property(t => t.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.TransferGroupId).HasColumnName("transfer_group_id").IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(t => t.IsDeleted);

        builder.HasIndex(t => new { t.UserId, t.TransferDate });
        builder.HasIndex(t => t.TransferGroupId);

        builder.HasOne(t => t.FromAccount)
            .WithMany()
            .HasForeignKey(t => t.FromAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.ToAccount)
            .WithMany()
            .HasForeignKey(t => t.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
