
using Garupa.Application.Interfaces;
using Garupa.Domain.Entities;
using Garupa.Domain.Repositories;

namespace Garupa.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;

    public UsuarioService(
        IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<Usuario> CriarAsync(
        string nome,
        string telefone,
        CancellationToken cancellationToken = default)
    {
        var usuario = new Usuario(nome, telefone);

        await _usuarioRepository.AdicionarAsync(
            usuario,
            cancellationToken);

        await _usuarioRepository.SalvarAlteracoesAsync(
            cancellationToken);

        return usuario;
    }

    public async Task<Usuario?> ObterPorIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _usuarioRepository.ObterPorIdAsync(
            id,
            cancellationToken);
    }

    public async Task<ContatoEmergencia?> AdicionarContatoAsync(
        int usuarioId,
        string nome,
        string telefone,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(
            usuarioId,
            cancellationToken);

        if (usuario is null)
            return null;

        var contato = new ContatoEmergencia(
            nome,
            telefone);

        usuario.AdicionarContato(contato);

        await _usuarioRepository.SalvarAlteracoesAsync(
            cancellationToken);

        return contato;
    }
}
