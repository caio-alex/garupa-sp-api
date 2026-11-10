using System.Text.RegularExpressions;

namespace Garupa.Domain.Entities;

public static class TelefoneBrasileiro
{
    public static string Validar(string telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone) ||
            !Regex.IsMatch(telefone, @"^\+?[0-9 ()-]+$"))
            throw new ArgumentException("Telefone inválido.");
        var digitos = Regex.Replace(telefone, @"\D", "");
        if (digitos.Length is 12 or 13 && digitos.StartsWith("55"))
            digitos = digitos[2..];
        if (!Regex.IsMatch(digitos, @"^[1-9][0-9](?:[2-5][0-9]{7}|9[0-9]{8})$"))
            throw new ArgumentException("Telefone inválido.");
        return digitos;
    }
}
