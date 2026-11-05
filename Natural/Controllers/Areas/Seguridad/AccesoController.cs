using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Common;
using Natural.Data.Areas.Seguridad;
using Natural.Models.Areas.Seguridad;

namespace Natural.Controllers.Areas.Seguridad
{
    /// <summary>
    /// El administrador de la empresa (rol 01) gestiona los accesos de SU empresa: crear usuario, activar/desactivar,
    /// cambiar rol, restablecer contraseña y quitar el acceso. La empresa sale siempre del JWT.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize(Policy = Roles.PoliticaAdministrador)]
    public class AccesoController : ApiControllerBase
    {
        private readonly ServicioAcceso _servicioAcceso;

        public AccesoController(ServicioAcceso servicioAcceso)
        {
            _servicioAcceso = servicioAcceso;
        }

        [HttpGet("Obtener")]
        public async Task<IActionResult> Obtener()
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var error))
                return error!;

            return Ok(await _servicioAcceso.ListarAsync(empresaId));
        }

        [HttpPost("Crear")]
        public async Task<IActionResult> Crear([FromBody] CrearAccesoRequest? request)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var error))
                return error!;

            return this.ToActionResult(await _servicioAcceso.CrearAsync(empresaId, request));
        }

        [HttpPatch("CambiarEstado/{usuarioId:int}")]
        public async Task<IActionResult> CambiarEstado(int usuarioId, [FromQuery] bool activo)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var error) || !TryObtenerPersonaId(out var personaId, out error))
                return error!;

            return this.ToActionResult(
                await _servicioAcceso.CambiarEstadoAsync(empresaId, usuarioId, activo, personaId),
                _ => Ok(new { activo }));
        }

        [HttpPatch("CambiarRol/{usuarioId:int}")]
        public async Task<IActionResult> CambiarRol(int usuarioId, [FromBody] CambiarRolRequest? request)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var error) || !TryObtenerPersonaId(out var personaId, out error))
                return error!;

            return this.ToActionResult(
                await _servicioAcceso.CambiarRolAsync(empresaId, usuarioId, request?.Rol, personaId),
                _ => Ok(new { rol = request!.Rol }));
        }

        [HttpPut("RestablecerContrasena/{usuarioId:int}")]
        public async Task<IActionResult> RestablecerContrasena(int usuarioId, [FromBody] RestablecerContrasenaRequest? request)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var error))
                return error!;

            return this.ToActionResult(
                await _servicioAcceso.RestablecerContrasenaAsync(empresaId, usuarioId, request?.NuevaContrasena),
                _ => Ok(new { mensaje = "Contraseña restablecida correctamente." }));
        }

        [HttpDelete("Eliminar/{usuarioId:int}")]
        public async Task<IActionResult> Eliminar(int usuarioId)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var error) || !TryObtenerPersonaId(out var personaId, out error))
                return error!;

            return this.ToActionResult(
                await _servicioAcceso.EliminarAsync(empresaId, usuarioId, personaId),
                _ => NoContent());
        }
    }
}
