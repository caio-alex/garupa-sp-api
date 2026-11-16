using System.ComponentModel.DataAnnotations;
using Garupa.Api.Security;
using Garupa.Domain.Entities;
using Garupa.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Garupa.Api.Controllers;

[ApiController]
[Route("api/v1/autenticacao")]
public class AutenticacaoController(GarupaDbContext banco, EmissorJwt emissor) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        string telefone;
        try { telefone = TelefoneBrasileiro.Validar(request.Telefone); }
        catch (ArgumentException) { return Unauthorized(); }

        var usuario = await banco.Usuarios.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Telefone == telefone && u.SenhaHash != null, ct);
        var hasher = new PasswordHasher<Usuario>();
        if (usuario is null || hasher.VerifyHashedPassword(usuario, usuario.SenhaHash!, request.Senha)
            == PasswordVerificationResult.Failed)
            return Unauthorized();

        return Ok(emissor.Emitir(usuario.Id));
    }
}

public record LoginRequest([Required, MaxLength(20)] string Telefone, [Required, MaxLength(128)] string Senha);
