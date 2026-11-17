using System.Globalization;
using System.Text.Encodings.Web;
using Garupa.Domain.Entities;

namespace Garupa.Api.Documentacao;

public static class PaginaAcompanhamento
{
    public static string Renderizar(Sos? sos)
    {
        var estado = sos?.Estado.ToString().ToLowerInvariant();
        var titulo = estado switch
        {
            "ativo" => "Pedido de socorro ativo",
            "cancelado" => "Pedido de socorro cancelado",
            "encerrado" => "Pedido de socorro encerrado",
            _ => "Acompanhamento indisponível"
        };
        var conteudo = sos is null
            ? "<p>Este link não foi encontrado ou já expirou.</p>"
            : Detalhes(sos);
        return $$"""
            <!doctype html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="referrer" content="no-referrer">
              <title>{{titulo}} | Garupa SP</title>
              <style>
                :root { color-scheme: light; font-family: system-ui, sans-serif; color: #17233a; background: #f4f6fa; }
                body { margin: 0; }
                main { max-width: 640px; margin: 8vh auto; padding: 24px; }
                .marca { color: #315ccb; font-weight: 750; letter-spacing: .04em; }
                .cartao { background: white; border: 1px solid #dfe5f0; border-radius: 20px; padding: 32px; box-shadow: 0 12px 36px #1a2b4912; }
                h1 { font-size: clamp(1.7rem, 5vw, 2.3rem); line-height: 1.15; margin: 12px 0 24px; }
                p { line-height: 1.6; } dt { color: #5c6980; font-size: .9rem; margin-top: 20px; } dd { margin: 5px 0 0; font-weight: 600; }
                a { display: inline-block; background: #315ccb; color: white; padding: 13px 19px; border-radius: 10px; text-decoration: none; margin-top: 24px; }
                a:hover { background: #2348aa; } .rodape { color: #5c6980; font-size: .9rem; }
              </style>
            </head>
            <body><main><p class="marca">GARUPA SP</p><section class="cartao"><h1>{{titulo}}</h1>{{conteudo}}</section><p class="rodape">Atualize a página para consultar o estado mais recente.</p></main></body>
            </html>
            """;
    }

    private static string Detalhes(Sos sos)
    {
        var horario = HtmlEncoder.Default.Encode(sos.OcorridoEm.ToUniversalTime().ToString("dd/MM/yyyy 'às' HH:mm 'UTC'", CultureInfo.InvariantCulture));
        if (sos.Latitude is not double lat || sos.Longitude is not double lon)
            return $"<dl><dt>Horário do evento</dt><dd>{horario}</dd><dt>Localização</dt><dd>Indisponível</dd></dl>";

        var latitude = lat.ToString("G", CultureInfo.InvariantCulture);
        var longitude = lon.ToString("G", CultureInfo.InvariantCulture);
        return $"<dl><dt>Horário do evento</dt><dd>{horario}</dd><dt>Localização</dt></dl>" +
            $"<a href=\"https://www.google.com/maps/search/?api=1&amp;query={latitude}%2C{longitude}\" target=\"_blank\" rel=\"noopener noreferrer\">Abrir localização no Google Maps</a>";
    }
}
