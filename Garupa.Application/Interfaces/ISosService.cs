using Garupa.Domain.Entities;
using Garupa.Domain.Enums;

namespace Garupa.Application.Interfaces;

public interface ISosService
{
    Task<Sos?> CriarAsync(string eventoId, int usuarioId, TipoEmergencia tipo, string origem,
        double lat, double lon, DateTime ocorridoEm, CancellationToken cancellationToken);
    Task<Sos?> AcompanharAsync(string token, CancellationToken cancellationToken);
    Task<bool> AlterarEstadoAsync(int id, bool cancelar, CancellationToken cancellationToken);
}
