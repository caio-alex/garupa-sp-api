using System.Security.Cryptography;
using System.Text;
using Garupa.Application.Interfaces;

namespace Garupa.Infrastructure.Security;

public sealed class CriptografiaDados : ICriptografiaDados
{
    private readonly byte[] chave;
    private static readonly byte[] Contexto = Encoding.UTF8.GetBytes("Garupa.FichaMedica.v1");

    public CriptografiaDados(byte[] chave)
    {
        if (chave.Length != 32) throw new ArgumentException("A chave deve ter 256 bits.");
        this.chave = chave.ToArray();
    }

    public string Proteger(string valor)
    {
        var origem = Encoding.UTF8.GetBytes(valor);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var resultado = new byte[12 + 16 + origem.Length];
        nonce.CopyTo(resultado, 0);
        using var aes = new AesGcm(chave, 16);
        aes.Encrypt(nonce, origem, resultado.AsSpan(28), resultado.AsSpan(12, 16), Contexto);
        return "g1:" + Convert.ToBase64String(resultado);
    }

    public string Desproteger(string valor)
    {
        if (!valor.StartsWith("g1:", StringComparison.Ordinal))
            throw new CryptographicException("Execute preparar-banco para converter fichas antigas.");
        var origem = Convert.FromBase64String(valor[3..]);
        if (origem.Length < 28) throw new CryptographicException("Ficha protegida inválida.");
        var resultado = new byte[origem.Length - 28];
        using var aes = new AesGcm(chave, 16);
        aes.Decrypt(origem.AsSpan(0, 12), origem.AsSpan(28), origem.AsSpan(12, 16), resultado, Contexto);
        return Encoding.UTF8.GetString(resultado);
    }

    public static CriptografiaDados Carregar(string? chaveBase64, string arquivo, bool permitirCriacao)
    {
        if (!string.IsNullOrWhiteSpace(chaveBase64))
            return new CriptografiaDados(Convert.FromBase64String(chaveBase64));
        if (!File.Exists(arquivo))
        {
            if (!permitirCriacao)
                throw new InvalidOperationException("Configure Criptografia__Chave ou um arquivo de chave persistente.");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(arquivo))!);
            try
            {
                using var stream = new FileStream(arquivo, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                stream.Write(RandomNumberGenerator.GetBytes(32));
            }
            catch (IOException) when (File.Exists(arquivo)) { }
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(arquivo, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        return new CriptografiaDados(File.ReadAllBytes(arquivo));
    }
}
