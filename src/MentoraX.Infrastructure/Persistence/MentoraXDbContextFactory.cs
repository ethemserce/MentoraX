using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MentoraX.Infrastructure.Persistence;

public sealed class MentoraXDbContextFactory : IDesignTimeDbContextFactory<MentoraXDbContext>
{
    public MentoraXDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MentoraXDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=MentoraXDb;User Id=sa;Password=MyP@ssw0rd123;TrustServerCertificate=True;MultipleActiveResultSets=true");

        return new MentoraXDbContext(optionsBuilder.Options);
    }
}
