using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Estructura;

namespace Natural.Controllers.Areas.Sesion
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly ServicioEstructura _servicioEstructura;

        public UsuarioController(ServicioEstructura servicioEstructura)
        {
            _servicioEstructura = servicioEstructura;
        }

        [HttpPut("ActualizarContrasena")]
        public async Task<IActionResult> ActualizarContrasena([FromBody] ActualizarContrasenaRequest? request)
        {
            if (request is null)
                return BadRequest("Debe enviar los datos para actualizar la contraseña.");

            if (request.UsuarioId <= 0)
                return BadRequest("El identificador de usuario no es válido.");

            if (string.IsNullOrWhiteSpace(request.NuevaContrasena))
                return BadRequest("Debe enviar la nueva contraseña.");

            if (request.NuevaContrasena.Length != 44)
                return BadRequest("La contraseña debe enviarse en formato correcto");

            var actualizado = await _servicioEstructura.ActualizarContrasenaUsuarioAsync(request.UsuarioId, request.NuevaContrasena);
            if (!actualizado)
                return NotFound("No se pudo actualizar la contraseña del usuario.");

            return Ok(new { mensaje = "Contraseña actualizada correctamente." });
        }
    }
}
