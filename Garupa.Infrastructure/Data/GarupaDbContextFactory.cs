
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Garupa.Infrastructure.Security;

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

        var root = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(root, "Garupa API.sln"))) root = Path.GetFullPath(Path.Combine(root, ".."));
        var arquivo = Environment.GetEnvironmentVariable("Criptografia__ArquivoChave")
            ?? Path.Combine(root, ".local", "keys", "ficha-medica.key");
        var criptografia = CriptografiaDados.Carregar(Environment.GetEnvironmentVariable("Criptografia__Chave"), arquivo, true);
        return new GarupaDbContext(optionsBuilder.Options, criptografia);
    }
}
