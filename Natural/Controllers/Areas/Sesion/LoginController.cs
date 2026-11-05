using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Estructura;
using Natural.Data.Areas.Seguridad;
using Natural.Data.Areas.Sesion;

namespace Natural.Controllers.Areas.Sesion
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ServicioLogin _servicioLogin;
        private readonly ServicioEstructura _servicioEstructura;
        private readonly ServicioSeguridad _servicioSeguridad;
        private readonly ServicioAcceso _servicioAcceso;

        public LoginController(
            ServicioLogin servicioLogin,
            ServicioEstructura servicioEstructura,
            ServicioSeguridad servicioSeguridad,
            ServicioAcceso servicioAcceso)
        {
            _servicioLogin = servicioLogin;
            _servicioEstructura = servicioEstructura;
            _servicioSeguridad = servicioSeguridad;
            _servicioAcceso = servicioAcceso;
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
            var passwordHash = request.Password;

            var usuario = await _servicioLogin.ObtenerUsuarioAsync(identificador, passwordHash);

            if (usuario is null)
                return Unauthorized("Usuario no encontrado");

            // Un usuario desactivado por el administrador (o de una empresa desactivada) no puede ingresar.
            if (!usuario.Estado)
                return StatusCode(StatusCodes.Status403Forbidden, "Tu usuario está inactivo. Comunícate con el administrador.");

            var rol = await _servicioEstructura.ObtenerRolUsuario(usuario.Id);
            var persona = await _servicioEstructura.ObtenerPersonaPorIdAsync(usuario.PersonaId);

            if (persona is null)
                return NotFound("No se encontró la persona asociada");

            if (persona.EmpresaId is int empresaId && !await _servicioAcceso.EmpresaActivaAsync(empresaId))
                return StatusCode(StatusCodes.Status403Forbidden, "La empresa está inactiva. Comunícate con el administrador de la plataforma.");

            var (token, expiraEn) = _servicioSeguridad.GenerarTokenJwt(persona, rol, usuario.Id);

            var response = new
            {
                token,
                expiraEn,
                persona = new
                {
                    id = persona.Id,
                    idUsuario = usuario.Id,
                    nombre = persona.Nombre,
                    empresaId = persona.EmpresaId,
                    nombreEmpresa = persona.Empresa?.Nombre,
                    rol
                }
            };

            return Ok(response);
        }
    }
}
