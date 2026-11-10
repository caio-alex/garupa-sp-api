
using System.ComponentModel.DataAnnotations;

namespace Garupa.Api.DTOs.Usuarios;

public record CriarUsuarioRequest(
    [Required]
    [MaxLength(120)]
    string Nome,

    [Required]
    [MaxLength(20)]
    string Telefone,

    [Required]
    CriarContatoRequest Contato
);
