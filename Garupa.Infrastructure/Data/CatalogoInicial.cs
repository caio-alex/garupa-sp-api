using System.Text.Json;
using Garupa.Domain.Entities;

namespace Garupa.Infrastructure.Data;

internal static class CatalogoInicial
{
    public static Distrito[] Distritos()
    {
        using var stream = typeof(CatalogoInicial).Assembly.GetManifestResourceStream("Garupa.Infrastructure.Resources.distritos.json")!;
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.EnumerateArray()
            .Select(d => new Distrito(d.GetProperty("id").GetInt32(), d.GetProperty("nome").GetString()!)).ToArray();
    }

    public static Mensagem[] Mensagens()
    {
        const string dnit = "https://www.gov.br/dnit/pt-br/assuntos/noticias/motociclista-no-transito-a-atencao-precisa-ser-redobrada";
        return
        [
            new(1, "Mantenha o capacete afivelado e a viseira em boas condições.", "geral", null, dnit),
            new(2, "Com chuva, diminua a velocidade: o piso molhado reduz a aderência.", "chuva", null, dnit),
            new(3, "Use luvas, roupa de proteção e calçado fechado ao pilotar.", "geral", "trabalho_moto", dnit),
            new(4, "Na entrega com chuva, reduza a velocidade e preserve sua segurança.", "chuva", "entregador_app", dnit),
            new(5, "Mantenha atenção às condições da via e espaço para reagir.", "ponto_critico", null, dnit),
            new(6, "No passeio, use capacete afivelado e equipamento de proteção.", "domingo", "lazer", dnit)
        ];
    }
}
