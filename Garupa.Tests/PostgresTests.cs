using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Garupa.Domain.Entities;
using Garupa.Domain.Enums;
using Garupa.Domain.Repositories;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Garupa.Tests;

public class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GARUPA_TEST_POSTGRES")))
            Skip = "Execute scripts/Test-Postgres.ps1 para validar com PostgreSQL local.";
    }
}

public class PostgresTests
{
    [PostgresFact]
    public async Task ReenviosConcorrentesDevolvemMesmoSos()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var cadastro = await client.PostAsJsonAsync("/api/v1/usuarios", new
        {
            nome = "Pessoa fictícia", telefone = "11987654321",
            contato = new { nome = "Contato fictício", telefone = "1134567890" }
        });
        Assert.Equal(HttpStatusCode.Created, cadastro.StatusCode);
        var usuario = await cadastro.Content.ReadFromJsonAsync<JsonElement>();
        var evento = new
        {
            usuarioId = usuario.GetProperty("usuarioId").GetInt32(),
            eventoId = Guid.NewGuid().ToString(), tipo = "queda", origem = "celular",
            ocorridoEm = factory.Clock.Agora.AddTicks(-1), lat = -23.55, lon = -46.63
        };
        var respostas = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => client.PostAsJsonAsync("/api/v1/sos", evento)));
        var ids = new HashSet<int>();
        var tokens = new HashSet<string>();
        foreach (var resposta in respostas)
        {
            using (resposta)
            {
                Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
                var sos = await resposta.Content.ReadFromJsonAsync<JsonElement>();
                ids.Add(sos.GetProperty("sosId").GetInt32());
                tokens.Add(sos.GetProperty("tokenAcompanhamento").GetString()!);
            }
        }
        Assert.Single(ids);
        Assert.Single(tokens);
        using var scope = factory.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<GarupaDbContext>().Sos.CountAsync());
    }

    [PostgresFact]
    public async Task RepositorioRecuperaConflitoUnicoEProtegeTransicoesConcorrentes()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<GarupaDbContext>();
        var second = secondScope.ServiceProvider.GetRequiredService<GarupaDbContext>();
        var usuario = new Usuario("Pessoa fictícia", "11987654321");
        usuario.AdicionarContato(new ContatoEmergencia("Contato fictício", "1134567890"));
        first.Usuarios.Add(usuario);
        await first.SaveChangesAsync();
        var evento = Guid.NewGuid().ToString();
        var agora = factory.Clock.Agora.UtcDateTime;
        var firstRepository = firstScope.ServiceProvider.GetRequiredService<ISosRepository>();
        var secondRepository = secondScope.ServiceProvider.GetRequiredService<ISosRepository>();
        var criado = await firstRepository.AdicionarAsync(new Sos(evento, usuario.Id,
            TipoEmergencia.Queda, "celular", -23.55, -46.63, agora, agora), default);
        var reenviado = await secondRepository.AdicionarAsync(new Sos(evento, usuario.Id,
            TipoEmergencia.Queda, "celular", -23.55, -46.63, agora, agora), default);
        Assert.Equal(criado.Id, reenviado.Id);
        Assert.Equal(criado.TokenAcompanhamento, reenviado.TokenAcompanhamento);
        criado.Cancelar(agora);
        reenviado.Encerrar(agora);
        await firstRepository.SalvarAsync(default);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondRepository.SalvarAsync(default));
        await second.Entry(reenviado).ReloadAsync();
        Assert.Equal(EstadoSos.Cancelado, reenviado.Estado);
    }
}
