using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Garupa.Domain.Entities;
using Garupa.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Garupa.Tests;

[CollectionDefinition("Desempenho", DisableParallelization = true)]
public class ColecaoDesempenho;

[Collection("Desempenho")]
public class DesempenhoTests
{
    private record Medicao(double P50Ms, double P95Ms, double MaxMs, int MaxBytes, int MinPontos);

    [PostgresFact]
    public async Task RiscoEPlanoAtendemMetasComCargaSinteticaEmKestrel()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente(usarKestrel: true);
        client.Timeout = TimeSpan.FromSeconds(30);
        int usuario;
        try { usuario = await Cenarios.CadastrarAsync(client); }
        catch (Exception ex)
        {
            var erros = string.Join(" | ", factory.Erros.Select(e => e.GetType().Name + " " + e.Message));
            throw new InvalidOperationException($"Falha no servidor de benchmark {client.BaseAddress}. {erros}", ex);
        }
        await Cenarios.ConsentirAsync(client, usuario);
        await Cenarios.PerfilAsync(client, usuario, "entregador_app", [355030801]);
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GarupaDbContext>();
            for (var i = 0; i < 10000; i++)
                context.CelulasRisco.Add(new CelulaRisco($"benchmark-ficticio-{i:D5}",
                    -23.55 + (i % 100) * 0.00001, -46.63 + (i / 100) * 0.00001,
                    2, "util", false, 0.8 + (i % 20) * 0.01, 99, "Via fictícia para teste de carga",
                    355030801, "benchmark-sintetico"));
            await context.SaveChangesAsync();
        }
        const string riscoUrl = "/api/v1/risco?lat=-23.55&lon=-46.63&raioMetros=1000&faixaHoraria=2&tipoDia=util&chuva=false";
        var planoUrl = $"/api/v1/plano-do-turno?usuarioId={usuario}&faixaHoraria=2&tipoDia=util&chuva=false";
        var risco = await MedirAsync(client, riscoUrl, plano: false);
        var plano = await MedirAsync(client, planoUrl, plano: true);
        var root = Directory.GetParent(factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath)!.FullName;
        var destino = Path.Combine(root, ".local", "evidencias");
        Directory.CreateDirectory(destino);
        var relatorio = new
        {
            executadoEm = DateTimeOffset.UtcNow, runtime = RuntimeInformation.FrameworkDescription,
            sistema = RuntimeInformation.OSDescription, servidor = "Kestrel HTTP loopback", banco = "PostgreSQL",
            dados = "10000 células sintéticas em schema exclusivo e removido ao final",
            aquecimentosPorRota = 5, requisicoesPorRota = 100, concorrencia = 4,
            metaP95Ms = 500, metaMaxBytes = 200000, risco, plano
        };
        await File.WriteAllTextAsync(Path.Combine(destino, "desempenho-postgres.json"),
            JsonSerializer.Serialize(relatorio, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
        Assert.True(risco.P95Ms <= 500, $"p95 de risco {risco.P95Ms:F2} ms");
        Assert.True(plano.P95Ms <= 500, $"p95 do plano {plano.P95Ms:F2} ms");
        Assert.True(risco.MaxBytes <= 200000);
        Assert.True(plano.MaxBytes <= 200000);
        Assert.True(risco.MinPontos >= 100);
        Assert.True(plano.MinPontos >= 100);
    }

    private static async Task<Medicao> MedirAsync(HttpClient client, string url, bool plano)
    {
        for (var i = 0; i < 5; i++)
        {
            using var aquecimento = await client.GetAsync(url);
            aquecimento.EnsureSuccessStatusCode();
        }
        var amostras = new List<(double Ms, int Bytes, int Pontos)>();
        for (var lote = 0; lote < 25; lote++)
        {
            var resultados = await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ =>
            {
                var inicio = Stopwatch.GetTimestamp();
                using var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var duracao = Stopwatch.GetElapsedTime(inicio).TotalMilliseconds;
                using var document = JsonDocument.Parse(bytes);
                var pontos = plano ? document.RootElement.GetProperty("pontosCriticos") : document.RootElement;
                return (Ms: duracao, Bytes: bytes.Length, Pontos: pontos.GetArrayLength());
            }));
            amostras.AddRange(resultados);
        }
        var tempos = amostras.Select(a => a.Ms).Order().ToArray();
        return new Medicao(tempos[49], tempos[94], tempos[^1], amostras.Max(a => a.Bytes), amostras.Min(a => a.Pontos));
    }
}
