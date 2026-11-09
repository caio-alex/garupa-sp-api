
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Garupa.Infrastructure.Data;

public class GarupaDbContextFactory
    : IDesignTimeDbContextFactory<GarupaDbContext>
{
    public GarupaDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "ConnectionStrings__GarupaDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A variável ConnectionStrings__GarupaDb não foi configurada.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<GarupaDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new GarupaDbContext(optionsBuilder.Options);
    }
}
