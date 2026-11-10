using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Garupa.Tests;

public class ImportadorRiscoTests
{
    private const string Cabecalho = "celula_id;centro_lat;centro_lon;faixa_horaria;tipo_dia;chuva;score;percentil;via_referencia;distrito_id;distrito_nome;versao_modelo\n";

    [Fact]
    public async Task CargaEhTransacionalAceitaCamposComAspasEConverteDistritoPorNome()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GarupaDbContext>();
        var importador = new ImportadorRisco(context);
        var arquivo = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(arquivo, Cabecalho + "teste;-23,55;-46,63;2;util;1;0,9;99;\"Via; fictícia\";1;Agua Rasa;teste\n");
            Assert.Equal(1, await importador.CarregarAsync(arquivo));
            context.ChangeTracker.Clear();
            var celula = await context.CelulasRisco.SingleAsync();
            Assert.Equal(355030801, celula.DistritoId);
            Assert.Equal("Via; fictícia", celula.ViaReferencia);
            Assert.Equal(0.9, celula.Score);
            await File.WriteAllTextAsync(arquivo, Cabecalho + "erro;-23.55;-46.63;2;util;1;1.5;99;Via;355030801;Água Rasa;teste\n");
            await Assert.ThrowsAsync<ArgumentException>(() => importador.CarregarAsync(arquivo));
            Assert.Equal("teste", (await context.CelulasRisco.AsNoTracking().SingleAsync()).CelulaId);
            await File.WriteAllTextAsync(arquivo, Cabecalho + "novo;-23.55;-46.63;2;util;1;0.8;97;Via;355030801;Água Rasa;teste\n");
            Assert.Equal(1, await importador.CarregarAsync(arquivo));
            Assert.Equal("novo", (await context.CelulasRisco.AsNoTracking().SingleAsync()).CelulaId);
        }
        finally { File.Delete(arquivo); }
    }
}
