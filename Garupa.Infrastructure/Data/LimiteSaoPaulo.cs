using System.Text.Json;
using Garupa.Application.Interfaces;

namespace Garupa.Infrastructure.Data;

public class LimiteSaoPaulo : ILimiteMunicipal
{
    private readonly double[][][][] poligonos;
    public LimiteSaoPaulo()
    {
        using var stream = typeof(LimiteSaoPaulo).Assembly.GetManifestResourceStream("Garupa.Infrastructure.Resources.sao-paulo.geojson")
            ?? throw new InvalidOperationException("Limite municipal indisponível.");
        using var document = JsonDocument.Parse(stream);
        var geometria = document.RootElement.GetProperty("features")[0].GetProperty("geometry");
        var coordenadas = geometria.GetProperty("coordinates").GetRawText();
        poligonos = geometria.GetProperty("type").GetString() switch
        {
            "Polygon" => [JsonSerializer.Deserialize<double[][][]>(coordenadas)!],
            "MultiPolygon" => JsonSerializer.Deserialize<double[][][][]>(coordenadas)!,
            _ => throw new InvalidOperationException("Geometria municipal inválida.")
        };
    }
    public bool Contem(double lat, double lon)
        => poligonos.Any(p => ContemAnel(p[0], lon, lat) && !p.Skip(1).Any(a => ContemAnel(a, lon, lat)));
    private static bool ContemAnel(double[][] anel, double x, double y)
    {
        var dentro = false;
        for (int i = 0, j = anel.Length - 1; i < anel.Length; j = i++)
        {
            var a = anel[i];
            var b = anel[j];
            var cruzamento = (x - a[0]) * (b[1] - a[1]) - (y - a[1]) * (b[0] - a[0]);
            if (Math.Abs(cruzamento) < 1e-12 && x >= Math.Min(a[0], b[0]) && x <= Math.Max(a[0], b[0]) &&
                y >= Math.Min(a[1], b[1]) && y <= Math.Max(a[1], b[1])) return true;
            if ((a[1] > y) != (b[1] > y) && x < (b[0] - a[0]) * (y - a[1]) / (b[1] - a[1]) + a[0])
                dentro = !dentro;
        }
        return dentro;
    }
}
