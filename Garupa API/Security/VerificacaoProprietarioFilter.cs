using Garupa.Api.Controllers;
using Garupa.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Garupa.Api.Security;

public sealed class VerificacaoProprietarioFilter(GarupaDbContext banco) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        if (!int.TryParse(context.HttpContext.User.FindFirst("sub")?.Value, out var usuarioId) || usuarioId <= 0 ||
            !await banco.Usuarios.AnyAsync(u => u.Id == usuarioId && u.SenhaHash != null, context.HttpContext.RequestAborted))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var args = context.ActionArguments;
        var permitido = context.Controller switch
        {
            UsuariosController when args.TryGetValue("id", out var id) => (int)id! == usuarioId,
            PrivacidadeController when args.TryGetValue("id", out var id) => (int)id! == usuarioId,
            IntegracaoVestivelController when args.TryGetValue("id", out var id) => (int)id! == usuarioId,
            IntegracaoVestivelController when args.TryGetValue("usuarioId", out var id) => (int?)id == usuarioId,
            IntegracaoVestivelController when args.TryGetValue("dispositivoId", out var dispositivo) =>
                await banco.Vestiveis.AnyAsync(v => v.UsuarioId == usuarioId && v.DispositivoId == (string)dispositivo!, context.HttpContext.RequestAborted),
            IntegracaoVestivelController when args.TryGetValue("request", out var request) && request is EventoVestivelRequest evento =>
                await banco.Vestiveis.AnyAsync(v => v.UsuarioId == usuarioId && v.DispositivoId == evento.DispositivoId, context.HttpContext.RequestAborted),
            SosController when args.TryGetValue("request", out var request) && request is SosRequest sos => sos.UsuarioId == usuarioId,
            SosController when args.TryGetValue("id", out var id) =>
                await banco.Sos.AnyAsync(s => s.Id == (int)id! && s.UsuarioId == usuarioId, context.HttpContext.RequestAborted),
            RelatoController when args.TryGetValue("request", out var request) && request is RelatoRequest relato => relato.UsuarioId == usuarioId,
            ConteudoController when args.TryGetValue("usuarioId", out var id) => (int?)id == usuarioId,
            _ => true
        };

        if (!permitido)
        {
            context.Result = new ForbidResult();
            return;
        }
        await next();
    }
}
