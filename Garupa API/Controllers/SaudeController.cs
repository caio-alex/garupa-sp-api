using Microsoft.AspNetCore.Mvc;

namespace Garupa.Api.Controllers
{
    [ApiController]
    public class SaudeController : ControllerBase
    {
        // Corresponde a GET /saude
        [HttpGet("/saude")]
        public IActionResult VerificarSaude()
        {
            return Ok(new { versao = "1.0", banco = "conectado" }); // Status 200
        }
    }
}