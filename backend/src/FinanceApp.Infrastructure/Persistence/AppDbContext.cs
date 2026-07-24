using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence;

/// <summary>
/// Contexto principal de Entity Framework Core.
/// Es el puente entre las entidades C# y PostgreSQL.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// El constructor recibe la configuración mediante
    /// inyección de dependencias desde Program.cs.
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── DbSets = Tablas ────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<BudgetPeriod> BudgetPeriods => Set<BudgetPeriod>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<DebtPayment> DebtPayments => Set<DebtPayment>();
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<InvestmentRecord> InvestmentRecords => Set<InvestmentRecord>();
    public DbSet<InvestmentContribution> InvestmentContributions => Set<InvestmentContribution>();
    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();
    public DbSet<SavingsGoalContribution> SavingsGoalContributions => Set<SavingsGoalContribution>();
    public DbSet<SavingsGoalWithdrawal> SavingsGoalWithdrawals => Set<SavingsGoalWithdrawal>();
    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
    public DbSet<AccountTransaction> AccountTransactions => Set<AccountTransaction>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DebtWithdrawal> DebtWithdrawals => Set<DebtWithdrawal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        /// Busca automáticamente todas las clases que implementen
        /// IEntityTypeConfiguration<T> en este assembly y las aplica.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>
    /// Se ejecuta antes de cada SaveChanges.
    /// Actualiza automáticamente UpdatedAt en entidades modificadas.
    /// Evita hacerlo manualmente en cada servicio.
    /// </summary>
    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        // Busca todas las entidades que heredan de BaseEntity
        // y que fueron modificadas en este contexto
        var modifiedEntries = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in modifiedEntries)
        {
            entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
