using System.Globalization;
using System.Text;
using Garupa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace Garupa.Infrastructure.Data;

public class ImportadorRisco(GarupaDbContext context)
{
    private static readonly string[] Colunas = ["celula_id", "centro_lat", "centro_lon", "faixa_horaria",
        "tipo_dia", "chuva", "score", "percentil", "via_referencia", "distrito_id", "distrito_nome", "versao_modelo"];

    public async Task<int> CarregarAsync(string caminho, CancellationToken ct = default)
    {
        var primeiraLinha = File.ReadLines(caminho, Encoding.UTF8).FirstOrDefault() ?? throw new ArgumentException("CSV vazio.");
        using var parser = new TextFieldParser(caminho, Encoding.UTF8, true);
        parser.SetDelimiters(primeiraLinha.Contains(';') ? ";" : ",");
        parser.HasFieldsEnclosedInQuotes = true;
        var cabecalho = parser.ReadFields() ?? [];
        if (cabecalho.Length != cabecalho.Distinct().Count() || Colunas.Except(cabecalho).Any())
            throw new ArgumentException("Cabeçalho do CSV inválido.");
        var distritos = await context.Distritos.AsNoTracking().ToListAsync(ct);
        var porNome = distritos.ToDictionary(d => Normalizar(d.Nome));
        var porId = distritos.ToDictionary(d => d.Id);
        var celulas = new List<CelulaRisco>();
        var chaves = new HashSet<(string, int, string, bool)>();
        while (!parser.EndOfData)
        {
            ct.ThrowIfCancellationRequested();
            var numero = parser.LineNumber;
            try
            {
                var campos = parser.ReadFields()!;
                if (campos.Length != cabecalho.Length) throw new ArgumentException();
                var linha = cabecalho.Zip(campos).ToDictionary(p => p.First, p => p.Second);
                var id = int.Parse(linha["distrito_id"], CultureInfo.InvariantCulture);
                var nome = Normalizar(linha["distrito_nome"]);
                if (!porId.TryGetValue(id, out var distrito))
                {
                    if (!porNome.TryGetValue(nome, out distrito)) throw new ArgumentException();
                }
                if (Normalizar(distrito.Nome) != nome) throw new ArgumentException();
                if (linha["chuva"] is not ("0" or "1")) throw new ArgumentException();
                var celula = new CelulaRisco(linha["celula_id"], Numero(linha["centro_lat"]), Numero(linha["centro_lon"]),
                    int.Parse(linha["faixa_horaria"], CultureInfo.InvariantCulture), linha["tipo_dia"], linha["chuva"] == "1",
                    Numero(linha["score"]), int.Parse(linha["percentil"], CultureInfo.InvariantCulture),
                    linha["via_referencia"], distrito.Id, linha["versao_modelo"]);
                if (!chaves.Add((celula.CelulaId, celula.FaixaHoraria, celula.TipoDia, celula.Chuva)))
                    throw new ArgumentException();
                celulas.Add(celula);
                if (celulas.Count > 500000) throw new ArgumentException();
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException or MalformedLineException)
            {
                throw new ArgumentException($"Linha {numero} do CSV inválida.");
            }
        }
        if (celulas.Count == 0) throw new ArgumentException("CSV sem células.");
        // A tabela representa um snapshot do modelo. A substituição é inteiramente transacional.
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await context.CelulasRisco.ExecuteDeleteAsync(ct);
        context.CelulasRisco.AddRange(celulas);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return celulas.Count;
    }

    private static double Numero(string valor)
        => double.Parse(valor.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);

    private static string Normalizar(string valor)
        => new(valor.Normalize(NormalizationForm.FormD).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
