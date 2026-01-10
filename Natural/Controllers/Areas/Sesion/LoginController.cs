using System.Security.Cryptography;
using System.Text;
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
                return BadRequest("Debe enviar el email");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Debe enviar la contraseña");

            var identificador = request.Email.Trim();
            // ✅ NO hashear aquí, la contraseña ya viene hasheada del frontend
            var passwordHash = request.Password;

            var usuario = await _servicioLogin.ObtenerUsuarioAsync(identificador, passwordHash);

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
                rol
            };

            return Ok(response);
        }
    }   
}
