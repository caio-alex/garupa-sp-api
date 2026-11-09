
using Garupa.Api.DTOs.Usuarios;
using Garupa.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Garupa.Api.Controllers;

[ApiController]
[Route("api/v1/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) ||
            string.IsNullOrWhiteSpace(request.Telefone))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Dados inválidos",
                Detail = "Nome e telefone são obrigatórios."
            });
        }

        var usuario = await _usuarioService.CriarAsync(
            request.Nome,
            request.Telefone,
            cancellationToken);

        return Created(
            $"/api/v1/usuarios/{usuario.Id}",
            new
            {
                usuarioId = usuario.Id
            });
    }

    // Endpoint auxiliar para consulta e teste do cadastro.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObterPorId(
        int id,
        CancellationToken cancellationToken)
    {
        var usuario = await _usuarioService.ObterPorIdAsync(
            id,
            cancellationToken);

        if (usuario is null)
            return NotFound();

        return Ok(new
        {
            usuarioId = usuario.Id,
            nome = usuario.Nome,
            telefone = usuario.Telefone,
            contatos = usuario.Contatos.Select(c => new
            {
                contatoId = c.Id,
                nome = c.Nome,
                telefone = c.Telefone
            })
        });
    }

    [HttpPost("{id:int}/contatos")]
    public async Task<IActionResult> AdicionarContato(
        int id,
        [FromBody] CriarContatoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) ||
            string.IsNullOrWhiteSpace(request.Telefone))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Dados inválidos",
                Detail = "Nome e telefone do contato são obrigatórios."
            });
        }

        var contato = await _usuarioService.AdicionarContatoAsync(
            id,
            request.Nome,
            request.Telefone,
            cancellationToken);

        if (contato is null)
            return NotFound();

        return StatusCode(201, new
        {
            contatoId = contato.Id
        });
    }

    // Endpoints existentes do vestível.
    // Ainda são mocks; a persistência será implementada depois.

    [HttpPost("{id:int}/vestivel")]
    public IActionResult ParearVestivel(
        int id,
        [FromBody] PareamentoRequest request)
    {
        return NoContent();
    }

    [HttpDelete("{id:int}/vestivel")]
    public IActionResult DesfazerPareamento(int id)
    {
        return NoContent();
    }

    [HttpPost("{id:int}/alertas-vestivel")]
    public IActionResult CriarAlertaVestivel(
        int id,
        [FromBody] AlertaVestivelRequest request)
    {
        return Accepted();
    }
}

public record PareamentoRequest(string dispositivoId);

public record AlertaVestivelRequest(
    string tipo,
    string ocorridoEm);
