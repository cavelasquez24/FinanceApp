using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class ExpenseTagConfiguration : IEntityTypeConfiguration<ExpenseTag>
{
    public void Configure(EntityTypeBuilder<ExpenseTag> builder)
    {
        builder.ToTable("expense_tags");
        builder.HasKey(et => new { et.ExpenseId, et.TagId });
        builder.Property(et => et.ExpenseId).HasColumnName("expense_id");
        builder.Property(et => et.TagId).HasColumnName("tag_id");
        builder.Property(et => et.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.HasIndex(et => new { et.TagId, et.ExpenseId }).HasDatabaseName("idx_expense_tags_tag_expense");
        builder.HasOne(et => et.Expense).WithMany(e => e.ExpenseTags).HasForeignKey(et => et.ExpenseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(et => et.Tag).WithMany(t => t.ExpenseTags).HasForeignKey(et => et.TagId).OnDelete(DeleteBehavior.Restrict);
    }
}
