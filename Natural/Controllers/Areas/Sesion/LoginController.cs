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
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest("Debe enviar la cédula");

            var usuario = await _servicioLogin.ObtenerUsuarioAsync(request.Email, request.Password);

            if (usuario == null)
                return Unauthorized("Usuario no encontrado");

            var persona = await _servicioEstructura.ObtenerPersonaPorIdAsync(usuario.PersonaId);

            // Solo devolver lo necesario
            var response = new
            {
                id = persona.Id,
                nombre = persona.Nombre,
                empresaId = persona.EmpresaId,
                rol = persona.Rol
            };

            return Ok(response);
        }
    }
}
