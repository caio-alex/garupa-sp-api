using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Garupa.Api.Security;

public record TokenEmitido(string Token, DateTime ExpiraEm);

public class EmissorJwt(IConfiguration configuracao)
{
    public byte[] ObterChave()
    {
        var valor = configuracao["Jwt:Chave"];
        if (string.IsNullOrWhiteSpace(valor)) throw new InvalidOperationException("Configure Jwt:Chave para emitir tokens.");
        byte[] chave;
        try { chave = Convert.FromBase64String(valor); }
        catch (FormatException) { throw new InvalidOperationException("Jwt:Chave deve estar em Base64."); }
        if (chave.Length < 32) throw new InvalidOperationException("Jwt:Chave deve ter ao menos 32 bytes.");
        return chave;
    }

    public TokenEmitido Emitir(int usuarioId)
    {
        var chave = ObterChave();
        var expiraEm = DateTime.UtcNow.AddHours(1);
        var token = new JwtSecurityToken("garupa-api", "garupa-app",
            [new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString())], expires: expiraEm,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(chave), SecurityAlgorithms.HmacSha256));
        return new TokenEmitido(new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}
