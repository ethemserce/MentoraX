using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MentoraX.Infrastructure.Persistence;

public class MentoraXDbContextFactory : IDesignTimeDbContextFactory<MentoraXDbContext>
{
    public MentoraXDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<MentoraXDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new MentoraXDbContext(optionsBuilder.Options);
    }
}