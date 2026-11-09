
using System;

namespace Garupa.Domain.Entities
{
    public class ContatoEmergencia
    {
        public int Id { get; private set; }

        // Chave estrangeira para a tabela Usuario
        public int UsuarioId { get; private set; }

        public string Nome { get; private set; } = string.Empty;

        public string Telefone { get; private set; } = string.Empty;

        // Construtor para o Entity Framework
        private ContatoEmergencia() { }

        public ContatoEmergencia(string nome, string telefone)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException(
                    "Nome do contato é obrigatório");

            if (string.IsNullOrWhiteSpace(telefone))
                throw new ArgumentException(
                    "Telefone do contato é obrigatório");

            Nome = nome.Trim();
            Telefone = telefone.Trim();
        }
    }
}
