using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MentoraX.Infrastructure.Persistence;

public sealed class MentoraXDbContextFactory : IDesignTimeDbContextFactory<MentoraXDbContext>
{
    public MentoraXDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../MentoraX.Api");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("DefaultConnection was not found.");

        var optionsBuilder = new DbContextOptionsBuilder<MentoraXDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new MentoraXDbContext(optionsBuilder.Options);
    }
}