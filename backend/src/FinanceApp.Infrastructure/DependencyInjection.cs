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


        // ── Servicios ──────────────────────────────────────────────────
        services.AddScoped<IJwtService, JwtService>();

        // Al corregir el using de arriba, esto se acoplará perfectamente al controlador
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IIncomeService, IncomeService>();


        return services;
    }
}