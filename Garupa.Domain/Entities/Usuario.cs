
using System;
using System.Collections.Generic;

namespace Garupa.Domain.Entities;

public class Usuario
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;
    public Guid Versao { get; private set; } = Guid.NewGuid();

    public FichaMedica? FichaMedica { get; private set; }

    private readonly List<ContatoEmergencia> _contatos = new();

    public IReadOnlyCollection<ContatoEmergencia> Contatos
        => _contatos.AsReadOnly();

    private Usuario() { } // Utilizado pelo EF Core

    public Usuario(string nome, string telefone)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório");

        if (string.IsNullOrWhiteSpace(telefone))
            throw new ArgumentException("Telefone é obrigatório");

        Nome = nome.Trim();
        Telefone = TelefoneBrasileiro.Validar(telefone);
    }

    public void AdicionarContato(ContatoEmergencia contato)
    {
        ArgumentNullException.ThrowIfNull(contato);
        _contatos.Add(contato);
        Versao = Guid.NewGuid();
    }

    public void Atualizar(string nome, string telefone)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 120)
            throw new ArgumentException("Nome inválido.");
        Nome = nome.Trim();
        Telefone = TelefoneBrasileiro.Validar(telefone);
        Versao = Guid.NewGuid();
    }

    public bool AtualizarContato(int id, string nome, string telefone)
    {
        var contato = _contatos.SingleOrDefault(c => c.Id == id);
        if (contato is null) return false;
        contato.Atualizar(nome, telefone);
        Versao = Guid.NewGuid();
        return true;
    }

    public bool RemoverContato(int id)
    {
        var contato = _contatos.SingleOrDefault(c => c.Id == id);
        if (contato is null) return false;
        if (_contatos.Count == 1)
            throw new Garupa.Domain.Exceptions.ConflitoRegraException("Mantenha ao menos um contato de emergência.");
        _contatos.Remove(contato);
        Versao = Guid.NewGuid();
        return true;
    }

    public void AtualizarFichaMedica(FichaMedica ficha)
    {
        ArgumentNullException.ThrowIfNull(ficha);
        FichaMedica = ficha;
    }
}
