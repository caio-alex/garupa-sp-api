using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garupa.Domain.Entities
{
    public class Usuario
    {
        public int Id { get; private set; }
        public string Nome { get; private set; }
        public string Telefone { get; private set; }
        public FichaMedica? FichaMedica { get; private set; }

        // Encapsulamento de coleções
        private readonly List<ContatoEmergencia> _contatos = new();
        public IReadOnlyCollection<ContatoEmergencia> Contatos => _contatos.AsReadOnly();

        // Construtor bloqueia a criação de utilizadores inválidos
        public Usuario(string nome, string telefone)
        {
            if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório");
            if (string.IsNullOrWhiteSpace(telefone)) throw new ArgumentException("Telefone é obrigatório");

            Nome = nome;
            Telefone = telefone;
        }

        public void AdicionarContato(ContatoEmergencia contato)
        {
            _contatos.Add(contato);
        }

        public void AtualizarFichaMedica(FichaMedica ficha)
        {
            FichaMedica = ficha; // A ficha é sensível e será encriptada na infraestrutura
        }
    }
}
