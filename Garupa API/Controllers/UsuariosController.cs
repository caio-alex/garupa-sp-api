
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

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, CriarContatoRequest request, CancellationToken ct)
        => await _usuarioService.AtualizarAsync(id, request.Nome, request.Telefone, ct)
            ? NoContent() : Problem(statusCode: 404, title: "Usuário não encontrado.");

    [HttpPut("{id:int}/contatos/{contatoId:int}")]
    public async Task<IActionResult> AtualizarContato(int id, int contatoId, CriarContatoRequest request, CancellationToken ct)
        => await _usuarioService.AtualizarContatoAsync(id, contatoId, request.Nome, request.Telefone, ct)
            ? NoContent() : Problem(statusCode: 404, title: "Contato não encontrado.");

    [HttpDelete("{id:int}/contatos/{contatoId:int}")]
    public async Task<IActionResult> RemoverContato(int id, int contatoId, CancellationToken ct)
        => await _usuarioService.RemoverContatoAsync(id, contatoId, ct)
            ? NoContent() : Problem(statusCode: 404, title: "Contato não encontrado.");

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
            request.Contato.Nome,
            request.Contato.Telefone,
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

}
