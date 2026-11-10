using System.Data.Common;
using Garupa.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Garupa.Domain.Entities;

namespace Garupa.Infrastructure.Data;

public static class PreparacaoBanco
{
    public static async Task ExecutarAsync(GarupaDbContext context, ICriptografiaDados criptografia, CancellationToken ct = default)
    {
        await context.Database.MigrateAsync(ct);
        await ProtegerFichasAntigasAsync(context, criptografia, ct);
        await ProtegerTokensAntigosAsync(context, criptografia, ct);
    }

    public static async Task ProtegerTokensAntigosAsync(GarupaDbContext context, ICriptografiaDados criptografia, CancellationToken ct = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await context.Database.OpenConnectionAsync(ct);
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT \"Id\", \"TokenAcompanhamento\" FROM sos WHERE \"TokenHash\" IS NULL";
        var tokens = new List<(int Id, string Protegido, string Hash)>();
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var armazenado = reader.GetString(1);
                var token = armazenado.StartsWith("g1:", StringComparison.Ordinal) ? criptografia.Desproteger(armazenado) : armazenado;
                tokens.Add((reader.GetInt32(0), criptografia.Proteger(token), Sos.CalcularHash(token)));
            }
        }
        foreach (var token in tokens)
        {
            command.Parameters.Clear();
            command.CommandText = "UPDATE sos SET \"TokenAcompanhamento\"=@token, \"TokenHash\"=@hash WHERE \"Id\"=@id";
            Adicionar(command, "id", token.Id);
            Adicionar(command, "token", token.Protegido);
            Adicionar(command, "hash", token.Hash);
            await command.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }

    public static async Task ProtegerFichasAntigasAsync(GarupaDbContext context, ICriptografiaDados criptografia, CancellationToken ct = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await context.Database.OpenConnectionAsync(ct);
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT \"Id\", \"TipoSanguineo\", \"Alergias\", \"Condicoes\", \"Medicamentos\" FROM \"FichaMedica\"";
        var pendentes = new List<(int Id, string[] Campos)>();
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var campos = Enumerable.Range(1, 4).Select(reader.GetString).ToArray();
                var converter = false;
                for (var i = 0; i < campos.Length; i++)
                {
                    if (campos[i].StartsWith("g1:", StringComparison.Ordinal)) criptografia.Desproteger(campos[i]);
                    else
                    {
                        campos[i] = criptografia.Proteger(campos[i]);
                        converter = true;
                    }
                }
                if (converter) pendentes.Add((reader.GetInt32(0), campos));
            }
        }
        foreach (var ficha in pendentes)
        {
            command.Parameters.Clear();
            command.CommandText = "UPDATE \"FichaMedica\" SET \"TipoSanguineo\"=@a, \"Alergias\"=@b, " +
                "\"Condicoes\"=@c, \"Medicamentos\"=@d WHERE \"Id\"=@id";
            Adicionar(command, "id", ficha.Id);
            for (var i = 0; i < 4; i++) Adicionar(command, ((char)('a' + i)).ToString(), ficha.Campos[i]);
            await command.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }

    private static void Adicionar(DbCommand command, string nome, object valor)
    {
        var parametro = command.CreateParameter();
        parametro.ParameterName = nome;
        parametro.Value = valor;
        command.Parameters.Add(parametro);
    }
}
