
using System;
using System.Collections.Generic;

namespace Garupa.Domain.Entities;

public class Usuario
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;

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
        Telefone = telefone.Trim();
    }

    public void AdicionarContato(ContatoEmergencia contato)
    {
        ArgumentNullException.ThrowIfNull(contato);
        _contatos.Add(contato);
    }

    public void AtualizarFichaMedica(FichaMedica ficha)
    {
        ArgumentNullException.ThrowIfNull(ficha);
        FichaMedica = ficha;
    }
}
