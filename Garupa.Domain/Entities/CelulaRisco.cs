namespace Garupa.Domain.Entities;

public class CelulaRisco
{
    public int Id { get; private set; }
    public string CelulaId { get; private set; } = string.Empty;
    public double CentroLat { get; private set; }
    public double CentroLon { get; private set; }
    public int FaixaHoraria { get; private set; }
    public string TipoDia { get; private set; } = string.Empty;
    public bool Chuva { get; private set; }
    public double Score { get; private set; }
    public int Percentil { get; private set; }
    public string ViaReferencia { get; private set; } = string.Empty;
    public int DistritoId { get; private set; }
    public string VersaoModelo { get; private set; } = string.Empty;
    private CelulaRisco() { }

    public CelulaRisco(string celulaId, double lat, double lon, int faixaHoraria, string tipoDia,
        bool chuva, double score, int percentil, string viaReferencia, int distritoId, string versaoModelo)
    {
        ContextoRisco.Validar(faixaHoraria, tipoDia);
        Posicao.Validar(lat, lon);
        if (string.IsNullOrWhiteSpace(celulaId) || celulaId.Length > 100 ||
            !double.IsFinite(score) || score is < 0 or > 1 || percentil is < 0 or > 100 ||
            string.IsNullOrWhiteSpace(viaReferencia) || viaReferencia.Length > 200 || distritoId <= 0 ||
            string.IsNullOrWhiteSpace(versaoModelo) || versaoModelo.Length > 100)
            throw new ArgumentException("Célula de risco inválida.");
        CelulaId = celulaId;
        CentroLat = lat;
        CentroLon = lon;
        FaixaHoraria = faixaHoraria;
        TipoDia = tipoDia;
        Chuva = chuva;
        Score = score;
        Percentil = percentil;
        ViaReferencia = viaReferencia;
        DistritoId = distritoId;
        VersaoModelo = versaoModelo;
    }
}

public static class Posicao
{
    public static void Validar(double lat, double lon)
    {
        if (!double.IsFinite(lat) || !double.IsFinite(lon) || lat is < -90 or > 90 || lon is < -180 or > 180)
            throw new ArgumentException("Posição inválida.");
    }

    public static double DistanciaMetros(double lat1, double lon1, double lat2, double lon2)
    {
        const double rad = Math.PI / 180;
        var a = Math.Pow(Math.Sin((lat2 - lat1) * rad / 2), 2) +
            Math.Cos(lat1 * rad) * Math.Cos(lat2 * rad) * Math.Pow(Math.Sin((lon2 - lon1) * rad / 2), 2);
        return 6371000 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
    }
}
