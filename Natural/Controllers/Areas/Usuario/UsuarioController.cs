using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Estructura;
using Natural.Data.Areas.Sesion;

namespace Natural.Controllers.Areas.Sesion
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly ServicioLogin _servicioLogin;
        private readonly ServicioEstructura _servicioEstructura;

        public UsuarioController(ServicioLogin servicioLogin, ServicioEstructura servicioEstructura)
        {
            _servicioLogin = servicioLogin;
            _servicioEstructura = servicioEstructura;
        }

        [HttpPost("Autenticar")]
        public async Task<IActionResult> Autenticar([FromBody] LoginRequest request)
        {
            if (request is null)
                return BadRequest("Debe enviar las credenciales");

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Debe enviar la cédula");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Debe enviar la contraseña");

            var usuario = await _servicioLogin.ObtenerUsuarioAsync(request.Email, request.Password);

            if (usuario is null)
                return Unauthorized("Usuario no encontrado");

            var rol = await _servicioEstructura.ObtenerRolUsuario(usuario.Id);

            var persona = await _servicioEstructura.ObtenerPersonaPorIdAsync(usuario.PersonaId);

            if (persona is null)
                return NotFound("No se encontró la persona asociada");

            var response = new
            {
                id = persona.Id,
                nombre = persona.Nombre,
                empresaId = persona.EmpresaId,
                rol = rol
            };

            return Ok(response);
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
