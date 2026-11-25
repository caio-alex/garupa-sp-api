using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Garupa.Api.Security;

public enum TipoPii { Nenhum, Nome, Telefone, Credencial, Cpf, Email, Saude, Localizacao, Documento }
public enum OrigemPii { Cadastro, AtualizacaoCadastro, Contato, FichaMedica }

// A marca acompanha cada valor. A API publica so oferece a representacao protegida.
public sealed class DadoRastreado
{
    private readonly JsonElement valor;
    public string Caminho { get; }
    public TipoPii Tipo { get; }
    public bool Sensivel => Tipo != TipoPii.Nenhum;

    internal DadoRastreado(string caminho, TipoPii tipo, JsonElement valor)
    {
        Caminho = caminho;
        Tipo = tipo;
        this.valor = valor.Clone();
    }

    public string ParaLog() => Sensivel ? "[PROTEGIDO]" : valor.GetRawText();
    public override string ToString() => ParaLog();
}

public sealed class DadosRastreados
{
    public IReadOnlyList<DadoRastreado> Campos { get; }
    internal DadosRastreados(List<DadoRastreado> campos) => Campos = campos.AsReadOnly();
    public override string ToString() => "[Dados rastreados: usar LoggerProtegido]";
}

public sealed class DetectorPii
{
    private static Regex Padrao(string texto) =>
        new(texto, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
            TimeSpan.FromMilliseconds(100));

    private static readonly Regex Email = Padrao(@"[a-z0-9._%+\-]+@[a-z0-9.\-]+\.[a-z]{2,}");
    // Candidatos a PII. Estes padroes nao validam titularidade ou digitos verificadores.
    private static readonly Regex Cpf = Padrao(@"\b[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2}\b|\b[0-9]{11}\b");
    private static readonly Regex Telefone = Padrao(@"\+?55[ .-]*\(?[0-9]{2}\)?[ .-]*[0-9]{4,5}[ .-]*[0-9]{4}\b|\([0-9]{2}\)[ .-]*[0-9]{4,5}[ .-]*[0-9]{4}\b|\b[0-9]{2}[ .-]*[0-9]{4,5}-[0-9]{4}\b");

    public DadosRastreados Inspecionar(object entrada) => Inspecionar(JsonSerializer.SerializeToElement(entrada));
    public DadosRastreados Inspecionar(JsonElement entrada)
    {
        var campos = new List<DadoRastreado>();
        Percorrer(entrada, "", TipoPii.Nenhum, campos);
        return new DadosRastreados(campos);
    }

    private static void Percorrer(JsonElement valor, string caminho, TipoPii herdado, List<DadoRastreado> campos)
    {
        if (valor.ValueKind == JsonValueKind.Object)
        {
            var indice = 0;
            foreach (var propriedade in valor.EnumerateObject())
            {
                var (rotulo, tipo) = Campo(propriedade.Name, ++indice);
                var proximo = caminho.Length == 0 ? rotulo : caminho + "." + rotulo;
                Percorrer(propriedade.Value, proximo, herdado != TipoPii.Nenhum ? herdado : tipo, campos);
            }
            return;
        }
        if (valor.ValueKind == JsonValueKind.Array)
        {
            var indice = 0;
            foreach (var item in valor.EnumerateArray())
                Percorrer(item, caminho + "[" + indice++ + "]", herdado, campos);
            return;
        }

        var tipoFinal = herdado;
        if (valor.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            tipoFinal = TipoPii.Nenhum;
        else if (tipoFinal == TipoPii.Nenhum)
        {
            var texto = valor.ToString();
            try
            {
                if (Email.IsMatch(texto)) tipoFinal = TipoPii.Email;
                else if (Cpf.IsMatch(texto)) tipoFinal = TipoPii.Cpf;
                else if (Telefone.IsMatch(texto)) tipoFinal = TipoPii.Telefone;
            }
            catch (RegexMatchTimeoutException)
            {
                tipoFinal = TipoPii.Documento;
            }
        }
        campos.Add(new DadoRastreado(caminho.Length == 0 ? "valor" : caminho, tipoFinal, valor));
    }

    private static (string Rotulo, TipoPii Tipo) Campo(string nome, int indice)
    {
        var normalizado = new string(nome.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark &&
                char.IsLetterOrDigit(c)).ToArray()).ToLowerInvariant();
        // Rotulos fixos evitam levar PII ou instrucoes contidas nas chaves para logs e IA.
        return normalizado switch
        {
            "nome" or "name" or "nomecompleto" => ("Nome", TipoPii.Nome),
            "telefone" or "phone" or "celular" => ("Telefone", TipoPii.Telefone),
            "senha" or "password" or "senhahash" or "token" or "apikey" => ("Senha", TipoPii.Credencial),
            "cpf" => ("Cpf", TipoPii.Cpf),
            "email" => ("Email", TipoPii.Email),
            "rg" or "documento" => ("Documento", TipoPii.Documento),
            "tiposanguineo" => ("TipoSanguineo", TipoPii.Saude),
            "alergias" => ("Alergias", TipoPii.Saude),
            "condicoes" => ("Condicoes", TipoPii.Saude),
            "medicamentos" => ("Medicamentos", TipoPii.Saude),
            "fichamedica" or "saude" or "laudo" => ("Saude", TipoPii.Saude),
            "lat" or "latitude" => ("Latitude", TipoPii.Localizacao),
            "lon" or "longitude" => ("Longitude", TipoPii.Localizacao),
            "contato" => ("Contato", TipoPii.Nenhum),
            "contatos" => ("Contatos", TipoPii.Nenhum),
            _ => ("campo" + indice, TipoPii.Nenhum)
        };
    }
}
