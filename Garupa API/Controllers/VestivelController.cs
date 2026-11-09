using Microsoft.AspNetCore.Mvc;

namespace Garupa.Api.Controllers
{
    [ApiController]
    public class VestivelController : ControllerBase
    {
        // Corresponde a POST /eventos-vestivel
        [HttpPost("/eventos-vestivel")]
        public IActionResult RegistrarEvento([FromBody] EventoVestivelRequest request)
        {
            return Accepted(); // Status 202
        }

        // Corresponde a GET /eventos-vestivel/pendentes
        [HttpGet("/eventos-vestivel/pendentes")]
        public IActionResult GetEventosPendentes([FromQuery] int usuarioId)
        {
            return Ok(new[] { new { tipo = "impacto", ocorridoEm = "2026-11-07T16:00:00Z" } });
        }

        // Corresponde a GET /vestivel/{dispositivoId}/alertas/pendentes
        [HttpGet("/vestivel/{dispositivoId}/alertas/pendentes")]
        public IActionResult GetAlertasPendentes(string dispositivoId)
        {
            return Ok(new[] { new { alertaId = 1, tipo = "contagem_queda", ocorridoEm = "2026-11-07T16:00:00Z" } });
        }
    }

    public record EventoVestivelRequest(string dispositivoId, string tipo, string ocorridoEm);
}