using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Contabilidad;
using Natural.Data.Areas.Gestion;
using Natural.Data.Domain.Estructura;

namespace Natural.Controllers.Areas.Contabilidad
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContabilidadController : ControllerBase
    {
        private readonly ServicioContabilidad _servicioContabilidad;

        public ContabilidadController(ServicioContabilidad servicioContabilidad)
        {
            _servicioContabilidad = servicioContabilidad;
        }

        [HttpGet("PagosUltimosDias")]
        public async Task<IActionResult> PagosUltimosDias()
        {
            var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();
            if (!int.TryParse(empresaIdHeader, out int empresaId))
                return BadRequest("Header 'empresaId' inválido.");

            var pagos = await _servicioContabilidad.ObtenerPagosAgrupadosUltimosDiasAsync(empresaId);
            return Ok(pagos);
        }

        [HttpGet("DetalleServiciosPorPersona/{personaId}")]
        public async Task<IActionResult> ObtenerDetallePorPersona(int personaId)
        {
            var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();
            if (!int.TryParse(empresaIdHeader, out int empresaId))
                return BadRequest("Header empresaId inválido.");

            var detalle = await _servicioContabilidad.ObtenerDetalleServiciosPorPersonaAsync(empresaId, personaId);
            return Ok(detalle);
        }

    }
}
