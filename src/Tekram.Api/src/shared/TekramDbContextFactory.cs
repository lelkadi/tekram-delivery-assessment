namespace Tekram.Api.src.shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Design-time factory for EF Core tooling (dotnet ef migrations add).
/// Uses the configured connection string without running the full app startup.
/// </summary>
public class TekramDbContextFactory : IDesignTimeDbContextFactory<TekramDbContext>
{
    public TekramDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<TekramDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

        return new TekramDbContext(optionsBuilder.Options);
    }
}
