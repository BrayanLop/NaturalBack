using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Estructura;
using Natural.Data.Areas.Sesion;

namespace Natural.Controllers.Areas.Sesion
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ServicioLogin _servicioLogin;
        private readonly ServicioEstructura _servicioEstructura;

        public LoginController(ServicioLogin servicioLogin, ServicioEstructura servicioEstructura)
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

            var personaTask = _servicioEstructura.ObtenerPersonaPorIdAsync(usuario.PersonaId);
            var rolTask = _servicioEstructura.ObtenerRol(usuario.PersonaId);

            await Task.WhenAll(personaTask, rolTask);

            var persona = personaTask.Result;

            if (persona is null)
                return NotFound("No se encontró la persona asociada");

            var response = new
            {
                id = persona.Id,
                nombre = persona.Nombre,
                empresaId = persona.EmpresaId,
                rol = rolTask.Result
            };

            return Ok(response);
        }
    }
}
