using Garupa.Domain.Entities;

namespace Garupa.Application.Interfaces;

public interface ILimiteMunicipal
{
    bool Contem(double lat, double lon);
}

public interface IRelatoService
{
    Task<Relato?> CriarAsync(string eventoId, int usuarioId, string tipo, double lat, double lon, string descricao, CancellationToken ct);
}
