using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Domain.Interfaces.Services;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Persistence.Repositories;
using FinanceApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace FinanceApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Base de datos ──────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(
                    typeof(AppDbContext).Assembly.FullName)
            )
        );

        // ── Repositorios ───────────────────────────────────────────────
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IIncomeRepository, IncomeRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<IReimbursementRepository, ReimbursementRepository>();
        services.AddScoped<IInvestmentRepository, InvestmentRepository>();
        services.AddScoped<ISavingsGoalRepository, SavingsGoalRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IDebtRepository, DebtRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        // La fecha civil de negocio no depende del reloj/zona del host.
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IBusinessDateProvider, EcuadorBusinessDateProvider>();
        services.AddScoped<IFinancialAccountRepository, FinancialAccountRepository>();
        services.AddScoped<IEmergencyFundRestorationRepository, EmergencyFundRestorationRepository>();
        services.AddScoped<ISavingsReplenishmentRepository, SavingsReplenishmentRepository>();
        services.AddScoped<IAccountReconciliationRepository, AccountReconciliationRepository>();
        services.AddScoped<INetWorthSnapshotRepository, NetWorthSnapshotRepository>();
        services.AddScoped<IDebtService, DebtService>();


        // ── Servicios ──────────────────────────────────────────────────
        services.AddScoped<IReimbursementService, ReimbursementService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<ICreditCardService, CreditCardService>();
        services.AddScoped<IFinancialPositionService, FinancialPositionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IInvestmentService, InvestmentService>();
        services.AddScoped<ISavingsGoalService, SavingsGoalService>();

        // Al corregir el using de arriba, esto se acoplará perfectamente al controlador
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IIncomeService, IncomeService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IFinancialAccountService, FinancialAccountService>();
        services.AddScoped<ICurrentDashboardService, CurrentDashboardService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmergencyFundRestorationService, EmergencyFundRestorationService>();
        services.AddScoped<ISavingsReplenishmentService, SavingsReplenishmentService>();
        services.AddScoped<IAccountReconciliationService, AccountReconciliationService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        return services;
    }
}