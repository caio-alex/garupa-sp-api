using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Garupa.Domain.Enums;

namespace Garupa.Domain.Entities
{
    public class Sos
    {
        public int Id { get; private set; }
        public string EventoId { get; private set; } // GUID gerado no telemóvel para idempotência
        public int UsuarioId { get; private set; }
        public TipoEmergencia Tipo { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public EstadoSos Estado { get; private set; }
        public string TokenAcompanhamento { get; private set; }
        public DateTime OcorridoEm { get; private set; }

        public Sos(string eventoId, int usuarioId, TipoEmergencia tipo, double latitude, double longitude, DateTime ocorridoEm)
        {
            EventoId = eventoId;
            UsuarioId = usuarioId;
            Tipo = tipo;
            Latitude = latitude;
            Longitude = longitude;
            OcorridoEm = ocorridoEm;
            Estado = EstadoSos.Ativo;
            TokenAcompanhamento = Guid.NewGuid().ToString("N");
        }

        public void Cancelar()
        {
            if (Estado == EstadoSos.Ativo)
            {
                Estado = EstadoSos.Cancelado;
            }
        }
    }
}
