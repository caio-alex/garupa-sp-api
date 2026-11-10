namespace Garupa.Application.Interfaces;

public interface ICriptografiaDados
{
    string Proteger(string valor);
    string Desproteger(string valor);
}
