using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Common;
using Natural.Data.Areas.Seguridad;
using Natural.Models.Areas.Seguridad;

namespace Natural.Controllers.Areas.Seguridad
{
    /// <summary>
    /// API interna para el microservicio de plataforma (empresas/tenants y sus accesos). NO es para el front:
    /// se autentica con la clave de servicio (header X-Service-Key), no con el JWT de un usuario.
    /// El microservicio es quien valida que quien llama sea un superadmin (rol 00) y quien audita.
    /// </summary>
    [AllowAnonymous]
    [ClaveServicio]
    [ApiController]
    [Route("api/[controller]")]
    public class PlataformaController : ControllerBase
    {
        private const string HeaderActor = "X-Actor";

        private readonly ServicioPlataforma _servicioPlataforma;
        private readonly ServicioAcceso _servicioAcceso;
        private readonly ILogger<PlataformaController> _logger;

        public PlataformaController(ServicioPlataforma servicioPlataforma, ServicioAcceso servicioAcceso, ILogger<PlataformaController> logger)
        {
            _servicioPlataforma = servicioPlataforma;
            _servicioAcceso = servicioAcceso;
            _logger = logger;
        }

        [HttpGet("Empresas")]
        public async Task<IActionResult> ListarEmpresas() =>
            Ok(await _servicioPlataforma.ListarEmpresasAsync());

        [HttpPost("Empresas")]
        public async Task<IActionResult> CrearEmpresa([FromBody] CrearEmpresaRequest? request)
        {
            var resultado = await _servicioPlataforma.CrearEmpresaAsync(request);
            Auditar("CrearEmpresa", resultado.Valor?.Id, resultado.Ok);
            return this.ToActionResult(resultado, e => StatusCode(StatusCodes.Status201Created, e));
        }

        [HttpPut("Empresas/{empresaId:int}")]
        public async Task<IActionResult> ActualizarEmpresa(int empresaId, [FromBody] EmpresaRequest? request)
        {
            var resultado = await _servicioPlataforma.ActualizarEmpresaAsync(empresaId, request);
            Auditar("ActualizarEmpresa", empresaId, resultado.Ok);
            return this.ToActionResult(resultado, _ => NoContent());
        }

        [HttpPatch("Empresas/{empresaId:int}/Estado")]
        public async Task<IActionResult> CambiarEstadoEmpresa(int empresaId, [FromQuery] bool activa)
        {
            var resultado = await _servicioPlataforma.CambiarEstadoEmpresaAsync(empresaId, activa);
            Auditar(activa ? "ActivarEmpresa" : "DesactivarEmpresa", empresaId, resultado.Ok);
            return this.ToActionResult(resultado, _ => Ok(new { activa }));
        }

        [HttpGet("Empresas/{empresaId:int}/Accesos")]
        public async Task<IActionResult> ListarAccesos(int empresaId) =>
            Ok(await _servicioAcceso.ListarAsync(empresaId));

        [HttpPost("Empresas/{empresaId:int}/Accesos")]
        public async Task<IActionResult> CrearAcceso(int empresaId, [FromBody] CrearAccesoRequest? request)
        {
            var resultado = await _servicioAcceso.CrearAsync(empresaId, request);
            Auditar("CrearAcceso", empresaId, resultado.Ok);
            return this.ToActionResult(resultado);
        }

        [HttpPatch("Empresas/{empresaId:int}/Accesos/{usuarioId:int}/Estado")]
        public async Task<IActionResult> CambiarEstadoAcceso(int empresaId, int usuarioId, [FromQuery] bool activo)
        {
            var resultado = await _servicioAcceso.CambiarEstadoAsync(empresaId, usuarioId, activo, personaSesionId: null);
            Auditar(activo ? "ActivarAcceso" : "DesactivarAcceso", empresaId, resultado.Ok, usuarioId);
            return this.ToActionResult(resultado, _ => Ok(new { activo }));
        }

        [HttpPatch("Empresas/{empresaId:int}/Accesos/{usuarioId:int}/Rol")]
        public async Task<IActionResult> CambiarRolAcceso(int empresaId, int usuarioId, [FromBody] CambiarRolRequest? request)
        {
            var resultado = await _servicioAcceso.CambiarRolAsync(empresaId, usuarioId, request?.Rol, personaSesionId: null);
            Auditar("CambiarRolAcceso", empresaId, resultado.Ok, usuarioId);
            return this.ToActionResult(resultado, _ => Ok(new { rol = request!.Rol }));
        }

        [HttpPut("Empresas/{empresaId:int}/Accesos/{usuarioId:int}/Contrasena")]
        public async Task<IActionResult> RestablecerContrasena(int empresaId, int usuarioId, [FromBody] RestablecerContrasenaRequest? request)
        {
            var resultado = await _servicioAcceso.RestablecerContrasenaAsync(empresaId, usuarioId, request?.NuevaContrasena);
            Auditar("RestablecerContrasena", empresaId, resultado.Ok, usuarioId);
            return this.ToActionResult(resultado, _ => Ok(new { mensaje = "Contraseña restablecida correctamente." }));
        }

        [HttpDelete("Empresas/{empresaId:int}/Accesos/{usuarioId:int}")]
        public async Task<IActionResult> EliminarAcceso(int empresaId, int usuarioId)
        {
            var resultado = await _servicioAcceso.EliminarAsync(empresaId, usuarioId, personaSesionId: null);
            Auditar("EliminarAcceso", empresaId, resultado.Ok, usuarioId);
            return this.ToActionResult(resultado, _ => NoContent());
        }

        [HttpPost("Empresas/{empresaId:int}/Impersonar")]
        public async Task<IActionResult> Impersonar(int empresaId, [FromBody] ImpersonarRequest? request)
        {
            var resultado = await _servicioPlataforma.ImpersonarAsync(empresaId, request);
            Auditar("Impersonar", empresaId, resultado.Ok);
            return this.ToActionResult(resultado);
        }

        private void Auditar(string accion, int? empresaId, bool ok, int? usuarioId = null)
        {
            var actor = Request.Headers[HeaderActor].ToString();
            _logger.LogInformation(
                "Plataforma {Accion} empresa={EmpresaId} usuario={UsuarioId} actor={Actor} ok={Ok}",
                accion, empresaId, usuarioId, string.IsNullOrWhiteSpace(actor) ? "desconocido" : actor, ok);
        }
    }
}
