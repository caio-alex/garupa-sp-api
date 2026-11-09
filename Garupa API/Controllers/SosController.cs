using Microsoft.AspNetCore.Mvc;

namespace Garupa.Api.Controllers
{
    [ApiController]
    public class SosController : ControllerBase
    {
        // Corresponde a POST /sos
        [HttpPost("/sos")]
        public IActionResult CriarSos([FromBody] SosRequest request)
        {
            // Retorna sosId e tokenAcompanhamento como pede a imagem
            return Ok(new { sosId = 1, tokenAcompanhamento = "abc-123456" });
        }

        // Corresponde a PATCH /sos/{id}/cancelar
        [HttpPatch("/sos/{id}/cancelar")]
        public IActionResult CancelarSos(int id)
        {
            return NoContent(); // Status 204
        }

        // Corresponde a GET /acompanhar/{token}
        [HttpGet("/acompanhar/{token}")]
        public IActionResult Acompanhar(string token)
        {
            return Ok(new { posicao = "lat/lon", horario = "2026-11-07T16:00:00Z", estado = "ativo" });
        }
    }

    // DTO para montar o corpo no Swagger baseado na imagem
    public record SosRequest(string eventoId, int usuarioId, string tipo, string origem, string ocorridoEm, double lat, double lon);
}