using Microsoft.AspNetCore.Mvc;

namespace Garupa.Api.Controllers
{
    [ApiController]
    [Route("/usuarios")]
    public class UsuariosController : ControllerBase
    {
        // Corresponde a POST /usuarios/{id}/vestivel
        [HttpPost("{id}/vestivel")]
        public IActionResult ParearVestivel(int id, [FromBody] PareamentoRequest request)
        {
            return NoContent(); // Status 204
        }

        // Corresponde a DELETE /usuarios/{id}/vestivel
        [HttpDelete("{id}/vestivel")]
        public IActionResult DesfazerPareamento(int id)
        {
            return NoContent(); // Status 204
        }

        // Corresponde a POST /usuarios/{id}/alertas-vestivel
        [HttpPost("{id}/alertas-vestivel")]
        public IActionResult CriarAlertaVestivel(int id, [FromBody] AlertaVestivelRequest request)
        {
            return Accepted(); // Status 202
        }
    }

    public record PareamentoRequest(string dispositivoId);
    public record AlertaVestivelRequest(string tipo, string ocorridoEm);
}