using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class NetWorthSnapshotConfiguration : IEntityTypeConfiguration<NetWorthSnapshot>
{
    public void Configure(EntityTypeBuilder<NetWorthSnapshot> builder)
    {
        builder.ToTable("net_worth_snapshots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.SnapshotDate).HasColumnName("snapshot_date").IsRequired();
        builder.Property(s => s.TotalAssets).HasColumnName("total_assets").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(s => s.TotalLiabilities).HasColumnName("total_liabilities").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(s => s.NetWorth).HasColumnName("net_worth").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(s => s.CashAccounts).HasColumnName("cash_accounts").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(s => s.SavingsAccounts).HasColumnName("savings_accounts").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(s => s.InvestmentPositions).HasColumnName("investment_positions").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(s => s.DebtLiabilities).HasColumnName("debt_liabilities").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(s => s.CreditCardLiabilities).HasColumnName("credit_card_liabilities").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(s => s.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at").IsRequired(false);
        builder.Ignore(s => s.IsDeleted);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.UserId, s.SnapshotDate })
            .IsUnique()
            .HasDatabaseName("idx_net_worth_snapshots_user_date");
    }
}
