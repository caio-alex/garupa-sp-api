using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garupa.Domain.Entities
{
    public class ContatoEmergencia
    {
        public int Id { get; private set; }
        public string Nome { get; private set; }
        public string Telefone { get; private set; }

        public ContatoEmergencia(string nome, string telefone)
        {
            Nome = nome;
            Telefone = telefone;
        }
    }

    public class FichaMedica
    {
        public int Id { get; private set; }
        public string TipoSanguineo { get; private set; }
        public string Alergias { get; private set; }
        public string Condicoes { get; private set; }
        public string Medicamentos { get; private set; }

        public FichaMedica(string tipoSanguineo, string alergias, string condicoes, string medicamentos)
        {
            TipoSanguineo = tipoSanguineo;
            Alergias = alergias;
            Condicoes = condicoes;
            Medicamentos = medicamentos;
        }
    }
}
