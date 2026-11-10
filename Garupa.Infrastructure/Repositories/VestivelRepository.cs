using System.Data.Common;
using Garupa.Domain.Entities;
using Garupa.Domain.Repositories;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Garupa.Infrastructure.Repositories;

public class VestivelRepository(GarupaDbContext context) : IVestivelRepository
{
    public Task<Vestivel?> PorUsuarioAsync(int usuarioId, CancellationToken ct)
        => context.Vestiveis.AsNoTracking().SingleOrDefaultAsync(v => v.UsuarioId == usuarioId, ct);
    public Task<Vestivel?> PorDispositivoAsync(string dispositivoId, CancellationToken ct)
        => context.Vestiveis.AsNoTracking().SingleOrDefaultAsync(v => v.DispositivoId == dispositivoId, ct);

    public async Task ParearAsync(Vestivel vestivel, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await LimparPareamentoAsync(vestivel.UsuarioId, ct);
        context.Vestiveis.Add(vestivel);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
    public async Task DesparearAsync(int usuarioId, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await LimparPareamentoAsync(usuarioId, ct);
        await transaction.CommitAsync(ct);
    }
    private async Task LimparPareamentoAsync(int usuarioId, CancellationToken ct)
    {
        await context.EventosVestivel.Where(e => e.UsuarioId == usuarioId).ExecuteDeleteAsync(ct);
        await context.AlertasVestivel.Where(e => e.UsuarioId == usuarioId).ExecuteDeleteAsync(ct);
        await context.Vestiveis.Where(v => v.UsuarioId == usuarioId).ExecuteDeleteAsync(ct);
    }
    public async Task AdicionarEventoAsync(EventoVestivel evento, CancellationToken ct)
    {
        context.EventosVestivel.Add(evento);
        await context.SaveChangesAsync(ct);
    }
    public async Task AdicionarAlertaAsync(AlertaVestivel alerta, CancellationToken ct)
    {
        context.AlertasVestivel.Add(alerta);
        await context.SaveChangesAsync(ct);
    }
    public async Task<IReadOnlyList<EventoPendente>> ConsumirEventosAsync(int usuarioId, DateTime agora, CancellationToken ct)
        => (await ConsumirAsync(false, usuarioId, null, agora, ct))
            .Select(e => new EventoPendente(e.Id, e.Tipo, e.OcorridoEm)).ToArray();
    public async Task<IReadOnlyList<AlertaPendente>> ConsumirAlertasAsync(string dispositivoId, DateTime agora, CancellationToken ct)
        => (await ConsumirAsync(true, 0, dispositivoId, agora, ct))
            .Select(e => new AlertaPendente(e.Id, e.Tipo, e.OcorridoEm)).ToArray();

    private async Task<List<(int Id, string Tipo, DateTime OcorridoEm)>> ConsumirAsync(bool alerta,
        int usuarioId, string? dispositivoId, DateTime agora, CancellationToken ct)
    {
        // UPDATE RETURNING entrega cada registro a um único consumidor, inclusive em chamadas simultâneas.
        var tabela = alerta ? "alertas_vestivel" : "eventos_vestivel";
        var filtro = alerta ? "\"DispositivoId\" = @dispositivo AND \"OcorridoEm\" > @limite" : "\"UsuarioId\" = @usuario";
        var bloqueio = context.Database.IsNpgsql() ? " FOR UPDATE SKIP LOCKED" : "";
        await context.Database.OpenConnectionAsync(ct);
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = $"UPDATE {tabela} SET \"ConsumidoEm\" = @agora WHERE \"Id\" IN " +
            $"(SELECT \"Id\" FROM {tabela} WHERE {filtro} AND \"ConsumidoEm\" IS NULL ORDER BY \"Id\" LIMIT 100{bloqueio}) " +
            "RETURNING \"Id\", \"Tipo\", \"OcorridoEm\"";
        Parametro(command, "agora", agora);
        if (alerta)
        {
            Parametro(command, "dispositivo", dispositivoId!);
            Parametro(command, "limite", agora.AddSeconds(-30));
        }
        else Parametro(command, "usuario", usuarioId);
        var resultado = new List<(int, string, DateTime)>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            resultado.Add((reader.GetInt32(0), reader.GetString(1), DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc)));
        return resultado.OrderBy(e => e.Item1).ToList();
    }
    private static void Parametro(DbCommand command, string nome, object valor)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = nome;
        parameter.Value = valor;
        command.Parameters.Add(parameter);
    }
}
