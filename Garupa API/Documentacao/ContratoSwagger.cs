using System.Text.Json;
using System.Text.Json.Nodes;
using Garupa.Application.Interfaces;
using Garupa.Domain.Entities;
using Garupa.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Garupa.Api.Documentacao;

public record UsuarioCriado(int UsuarioId);
public record ContatoCriado(int ContatoId);
public record ContatoConsulta(int ContatoId, string Nome, string Telefone);
public record UsuarioConsulta(int UsuarioId, string Nome, string Telefone, ContatoConsulta[] Contatos);
public record SosCriado(int SosId, string TokenAcompanhamento);
public record PosicaoConsulta(double Lat, double Lon);
public record AcompanhamentoConsulta(PosicaoConsulta Posicao, DateTime Horario, string Estado);
public record RelatoCriado(int RelatoId);
public record DistritoConsulta(int DistritoId, string Nome);
public record SaudeConsulta(string Versao, string Banco);

/// <summary>Documenta as respostas HTTP reais e exemplos fictícios de cada operação.</summary>
public class ContratoSwagger : IOperationFilter
{
    private record Contrato(string Resumo, int Status, Type? Resposta, int[] Erros);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Dictionary<string, Contrato> Contratos = new()
    {
        ["POST api/v1/usuarios"] = new("Cadastrar usuário e contato em uma transação", 201, typeof(UsuarioCriado), [400]),
        ["GET api/v1/usuarios/{id}"] = new("Consultar cadastro e contatos", 200, typeof(UsuarioConsulta), [404]),
        ["PUT api/v1/usuarios/{id}"] = new("Editar nome e telefone", 204, null, [400, 404, 409]),
        ["DELETE api/v1/usuarios/{id}"] = new("Excluir usuário e seus dados dependentes", 204, null, [404]),
        ["POST api/v1/usuarios/{id}/contatos"] = new("Adicionar contato de emergência", 201, typeof(ContatoCriado), [400, 404, 409]),
        ["PUT api/v1/usuarios/{id}/contatos/{contatoId}"] = new("Editar contato de emergência", 204, null, [400, 404, 409]),
        ["DELETE api/v1/usuarios/{id}/contatos/{contatoId}"] = new("Remover contato preservando pelo menos um", 204, null, [404, 409]),
        ["PUT api/v1/usuarios/{id}/consentimentos"] = new("Conceder ou revogar os cinco consentimentos", 204, null, [400, 404, 409]),
        ["PUT api/v1/usuarios/{id}/ficha-medica"] = new("Salvar ficha opcional com consentimento", 204, null, [400, 404, 409]),
        ["PUT api/v1/usuarios/{id}/perfil"] = new("Salvar usos, horários habituais e até cinco distritos", 204, null, [400, 404, 409]),
        ["POST api/v1/sos"] = new("Criar SOS ou recuperar o mesmo evento reenviado", 201, typeof(SosCriado), [400, 404, 409]),
        ["PATCH api/v1/sos/{id}/cancelar"] = new("Cancelar SOS de forma idempotente", 204, null, [404, 409]),
        ["PATCH api/v1/sos/{id}/encerrar"] = new("Encerrar SOS de forma idempotente", 204, null, [404, 409]),
        ["GET api/v1/acompanhar/{token}"] = new("Acompanhar posição e estado pelo token válido", 200, typeof(AcompanhamentoConsulta), [404]),
        ["GET api/v1/distritos"] = new("Listar os 96 distritos com identificadores IBGE", 200, typeof(DistritoConsulta[]), []),
        ["GET api/v1/mensagens"] = new("Consultar catálogo por contexto e tipo de uso", 200, typeof(MensagemResposta[]), [400]),
        ["GET api/v1/risco"] = new("Consultar pontos críticos por raio e contexto", 200, typeof(PontoCritico[]), [400]),
        ["GET api/v1/risco/celulas/{id}"] = new("Consultar todas as faixas de uma célula", 200, typeof(CelulaRisco[]), [404]),
        ["GET api/v1/plano-do-turno"] = new("Montar plano com faixa explícita ou habitual", 200, typeof(PlanoTurno), [400, 404]),
        ["POST api/v1/usuarios/{id}/vestivel"] = new("Parear dispositivo opcional", 204, null, [400, 404, 409]),
        ["DELETE api/v1/usuarios/{id}/vestivel"] = new("Desparear e descartar pendências", 204, null, [404]),
        ["POST api/v1/usuarios/{id}/alertas-vestivel"] = new("Enfileirar alerta com validade de 30 segundos", 202, null, [400, 404, 409]),
        ["POST api/v1/eventos-vestivel"] = new("Receber evento de dispositivo pareado", 202, null, [400, 404, 409]),
        ["GET api/v1/eventos-vestivel/pendentes"] = new("Consumir até 100 eventos uma única vez", 200, typeof(EventoPendente[]), [400, 404]),
        ["GET api/v1/vestivel/{dispositivoId}/alertas/pendentes"] = new("Consumir alertas válidos uma única vez", 200, typeof(AlertaPendente[]), [404]),
        ["POST api/v1/relatos"] = new("Registrar relato idempotente dentro de São Paulo", 201, typeof(RelatoCriado), [400, 404, 409]),
        ["GET api/v1/saude"] = new("Verificar versão e conexão com o banco", 200, typeof(SaudeConsulta), [503]),
        ["GET saude"] = new("Verificar saúde pelo endereço do monitor", 200, typeof(SaudeConsulta), [503])
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var caminho = context.ApiDescription.RelativePath?.Split('?')[0];
        if (!Contratos.TryGetValue($"{context.ApiDescription.HttpMethod} {caminho}", out var contrato))
            throw new InvalidOperationException("Operação sem contrato Swagger explícito.");

        operation.Summary = contrato.Resumo;
        operation.Responses = new OpenApiResponses
        {
            [contrato.Status.ToString()] = Resposta(context, contrato.Status, contrato.Resposta)
        };
        foreach (var status in contrato.Erros.Append(500))
            operation.Responses[status.ToString()] = Resposta(context, status, typeof(ProblemDetails));

        var corpo = context.ApiDescription.ParameterDescriptions.FirstOrDefault(p => p.Source?.Id == "Body");
        if (corpo is not null && ExemplosCorpo(corpo.Type.Name) is { } exemplo && operation.RequestBody?.Content is { } conteudos)
        {
            foreach (var media in conteudos.Values)
                media.Example = JsonSerializer.SerializeToNode(exemplo, JsonOptions);
        }

        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            parameter.Example = ExemploParametro(parameter.Name!);
            if (caminho == "api/v1/plano-do-turno" && parameter.Name == "faixaHoraria")
                parameter.Description = "Opcional. A faixa informada prevalece. Sem ela, usa o próximo horário habitual do tipo de dia; sem hábito, usa a hora de São Paulo.";
        }
        if (caminho is "api/v1/risco" or "api/v1/plano-do-turno")
            operation.Description = "Lista de até 500 pontos por score, limitada a 190.000 bytes para reservar espaço dentro dos 200 KB da resposta.";
    }

    private static OpenApiResponse Resposta(OperationFilterContext context, int status, Type? tipo)
    {
        var resposta = new OpenApiResponse
        {
            Description = status switch
            {
                200 => "Consulta concluída.", 201 => "Recurso criado ou recuperado por idempotência.",
                202 => "Evento aceito e persistido.", 204 => "Operação concluída sem corpo.",
                400 => "Entrada inválida.", 404 => "Recurso inexistente ou token expirado.",
                409 => "Conflito de estado, consentimento ou concorrência.", 503 => "Banco indisponível.",
                _ => "Erro interno com mensagem genérica."
            }
        };
        if (tipo is null) return resposta;
        resposta.Content = new Dictionary<string, OpenApiMediaType>
        {
            [status >= 400 ? "application/problem+json" : "application/json"] = new()
            {
                Schema = context.SchemaGenerator.GenerateSchema(tipo, context.SchemaRepository),
                Example = status >= 400 && status != 503
                    ? JsonSerializer.SerializeToNode(new { type = "about:blank", title = resposta.Description, status }, JsonOptions)
                    : status == 503 ? JsonSerializer.SerializeToNode(new SaudeConsulta("1.0", "indisponivel"), JsonOptions)
                    : null
            }
        };
        if (status == 503)
        {
            resposta.Content.Clear();
            resposta.Content["application/json"] = new OpenApiMediaType
            {
                Schema = context.SchemaGenerator.GenerateSchema(typeof(SaudeConsulta), context.SchemaRepository),
                Example = JsonSerializer.SerializeToNode(new SaudeConsulta("1.0", "indisponivel"), JsonOptions)
            };
        }
        return resposta;
    }

    private static object? ExemplosCorpo(string tipo) => tipo switch
    {
        "CriarUsuarioRequest" => new { nome = "Pessoa fictícia", telefone = "11987654321", contato = new { nome = "Contato fictício", telefone = "1134567890" } },
        "CriarContatoRequest" => new { nome = "Contato fictício", telefone = "1134567890" },
        "ConsentimentosRequest" => new { localizacaoEmUso = true, localizacaoEmSegundoPlano = true, fichaMedica = true, historicoDeLocais = false, perfilDeUso = true },
        "FichaMedicaRequest" => new { tipoSanguineo = "O+", alergias = "Exemplo fictício", condicoes = "", medicamentos = "" },
        "PerfilRequest" => new { tiposDeUso = new[] { "entregador_app" }, horariosHabituais = new[] { new { faixaHoraria = 2, tipoDia = "util" } }, distritosFrequentes = new[] { 355030801 } },
        "SosRequest" => new { eventoId = "a7cabdd5-8976-4454-8aa6-1fd22aa504c5", usuarioId = 1, tipo = "queda", origem = "celular", ocorridoEm = "2026-11-10T10:00:00Z", lat = -23.55, lon = -46.63 },
        "PareamentoRequest" => new { dispositivoId = "garupa-demo" },
        "AlertaVestivelRequest" => new { tipo = "ponto_critico", ocorridoEm = "2026-11-10T10:00:00Z" },
        "EventoVestivelRequest" => new { dispositivoId = "garupa-demo", tipo = "impacto", ocorridoEm = "2026-11-10T10:00:00Z" },
        "RelatoRequest" => new { eventoId = "f61061c7-a2e1-4f94-9425-d14c2f4c3455", usuarioId = 1, tipo = "buraco", lat = -23.55, lon = -46.63, descricao = "Exemplo fictício" },
        _ => null
    };

    private static JsonNode? ExemploParametro(string nome) => nome switch
    {
        "usuarioId" or "id" or "contatoId" => JsonValue.Create(1),
        "lat" => JsonValue.Create(-23.55), "lon" => JsonValue.Create(-46.63),
        "raioMetros" => JsonValue.Create(1000), "faixaHoraria" => JsonValue.Create(2),
        "tipoDia" => JsonValue.Create("util"), "chuva" => JsonValue.Create(false),
        "contexto" => JsonValue.Create("chuva"), "tipoDeUso" => JsonValue.Create("entregador_app"),
        "dispositivoId" => JsonValue.Create("garupa-demo"),
        _ => null
    };
}
