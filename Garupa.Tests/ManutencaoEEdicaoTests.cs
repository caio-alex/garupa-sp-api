using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Garupa.Application.Interfaces;
using Garupa.Domain.Entities;
using Garupa.Domain.Enums;
using Garupa.Domain.Exceptions;
using Garupa.Domain.Repositories;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Garupa.Tests;

public class ManutencaoEEdicaoTests
{
    [Fact]
    public async Task EditarUsuarioEContatoPreservaContatoObrigatorio()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/v1/usuarios/{id}", new { nome = "Nome fictício atualizado", telefone = "21987654321" })).StatusCode);
        var usuario = await client.GetFromJsonAsync<JsonElement>($"/api/v1/usuarios/{id}");
        Assert.Equal("21987654321", usuario.GetProperty("telefone").GetString());
        var contato = usuario.GetProperty("contatos")[0].GetProperty("contatoId").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/v1/usuarios/{id}/contatos/{contato}", new { nome = "Contato atualizado", telefone = "2134567890" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/v1/usuarios/{id}/contatos/{contato}")).StatusCode);
        await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/contatos", new { nome = "Segundo fictício", telefone = "1134567890" });
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/usuarios/{id}/contatos/{contato}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/v1/usuarios/{id}/contatos/{contato}")).StatusCode);
    }

    [Fact]
    public async Task TokenEhProtegidoNoBancoECoordenadasSaoRemovidasAposPrazo()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        var evento = new { eventoId = Guid.NewGuid(), usuarioId = id, tipo = "queda", origem = "celular", ocorridoEm = factory.Clock.Agora, lat = -23.55, lon = -46.63 };
        var resposta = await client.PostAsJsonAsync("/api/v1/sos", evento);
        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var token = criado.GetProperty("tokenAcompanhamento").GetString()!;
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GarupaDbContext>();
        await context.Database.OpenConnectionAsync();
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT \"TokenAcompanhamento\" FROM sos";
        var armazenado = (string)(await command.ExecuteScalarAsync())!;
        Assert.NotEqual(token, armazenado);
        Assert.StartsWith("g1:", armazenado);
        Assert.Equal(token, scope.ServiceProvider.GetRequiredService<ICriptografiaDados>().Desproteger(armazenado));
        command.CommandText = "SELECT \"TokenHash\" FROM sos";
        Assert.Equal(Sos.CalcularHash(token), await command.ExecuteScalarAsync());
        factory.Clock.Agora = factory.Clock.Agora.AddHours(12);
        var manutencao = scope.ServiceProvider.GetRequiredService<IManutencaoRepository>();
        await manutencao.ExecutarAsync(factory.Clock.Agora.UtcDateTime, default);
        var sos = await context.Sos.AsNoTracking().SingleAsync();
        Assert.Equal(EstadoSos.Encerrado, sos.Estado);
        Assert.NotNull(sos.Latitude);
        factory.Clock.Agora = factory.Clock.Agora.AddDays(30);
        await manutencao.ExecutarAsync(factory.Clock.Agora.UtcDateTime, default);
        sos = await context.Sos.AsNoTracking().SingleAsync();
        Assert.Null(sos.Latitude);
        Assert.Null(sos.Longitude);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/acompanhar/{token}")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/sos", evento)).StatusCode);
        Assert.Equal(1, await context.Sos.CountAsync());
    }

    [Fact]
    public async Task TokensLegadosSaoConvertidosSemQuebrarLinks()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GarupaDbContext>();
        var agora = factory.Clock.Agora.UtcDateTime;
        var token = Guid.NewGuid().ToString("N");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO sos (\"EventoId\", \"UsuarioId\", \"Tipo\", \"Origem\", \"Latitude\", \"Longitude\", \"Estado\", \"TokenAcompanhamento\", \"OcorridoEm\", \"CriadoEm\") VALUES ({Guid.NewGuid().ToString()}, {id}, {0}, {"celular"}, {-23.55}, {-46.63}, {0}, {token}, {agora}, {agora})");
        var criptografia = scope.ServiceProvider.GetRequiredService<ICriptografiaDados>();
        await PreparacaoBanco.ProtegerTokensAntigosAsync(context, criptografia);
        await PreparacaoBanco.ProtegerTokensAntigosAsync(context, criptografia);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/acompanhar/{token}")).StatusCode);
        Assert.Equal(Sos.CalcularHash(token), (await context.Sos.AsNoTracking().SingleAsync()).TokenHash);
    }

    [Fact]
    public async Task RepositorioRecusaFichaOuPerfilSeConsentimentoFoiRevogado()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        await Cenarios.ConsentirAsync(client, id);
        using var scope = factory.Services.CreateScope();
        var privacidade = scope.ServiceProvider.GetRequiredService<IPrivacidadeRepository>();
        Assert.True((await privacidade.ObterConsentimentoAsync(id, default))!.FichaMedica);
        await Cenarios.ConsentirAsync(client, id, ficha: false, perfil: false);
        await Assert.ThrowsAsync<ConflitoRegraException>(() => privacidade.SalvarFichaAsync(id, new FichaMedica("", "", "", ""), default));
        var perfil = new PerfilUso(id);
        perfil.Atualizar(["lazer"], [], []);
        await Assert.ThrowsAsync<ConflitoRegraException>(() => scope.ServiceProvider.GetRequiredService<IPerfilRepository>().SalvarAsync(perfil, default));
    }

    [PostgresFact]
    public async Task ExclusoesConcorrentesDeContatosNaoRemovemAmbos()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/contatos", new { nome = "Segundo fictício", telefone = "1134567890" });
        using var a = factory.Services.CreateScope();
        using var b = factory.Services.CreateScope();
        var repoA = a.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        var repoB = b.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        var userA = (await repoA.ObterPorIdAsync(id))!;
        var userB = (await repoB.ObterPorIdAsync(id))!;
        userA.RemoverContato(userA.Contatos.First().Id);
        userB.RemoverContato(userB.Contatos.Last().Id);
        await repoA.SalvarAlteracoesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repoB.SalvarAlteracoesAsync());
        using var consulta = factory.Services.CreateScope();
        Assert.Single((await consulta.ServiceProvider.GetRequiredService<IUsuarioRepository>().ObterPorIdAsync(id))!.Contatos);
    }
}
