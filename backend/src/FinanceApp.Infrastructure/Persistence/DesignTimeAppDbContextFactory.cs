using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FinanceApp.Infrastructure.Persistence;

/// <summary>
/// Permite que las herramientas de EF Core (migrations add / database update)
/// construyan el AppDbContext sin arrancar FinanceApp.API. Solo se usa en
/// tiempo de diseño; en runtime el contexto sigue viniendo de la DI de
/// Program.cs.
///
/// Motivo: el tooling de EF compila el startup project completo. Atarlo a la
/// API impide generar migraciones mientras cualquier controller esté en medio
/// de una refactorización, aunque Domain e Infrastructure ya estén correctos.
/// </summary>
public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ApiProjectRelativePath = "../FinanceApp.API";

    public AppDbContext CreateDbContext(string[] args)
    {
        var apiPath = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), ApiProjectRelativePath));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(apiPath) ? apiPath : Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No se encontró ConnectionStrings:DefaultConnection para el tooling de EF Core.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
