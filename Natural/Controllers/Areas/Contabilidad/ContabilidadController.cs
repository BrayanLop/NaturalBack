using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Configuracion;
using Natural.Data.Areas.Contabilidad;
using Natural.Models.Areas.Configuracion;

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

        [HttpPost("LiquidarPersona")]
        public async Task<IActionResult> LiquidarPersona([FromQuery]int personaId)
        {
            var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();
            if (!int.TryParse(empresaIdHeader, out int empresaId))
                return BadRequest("Header empresaId inválido.");

            try
            {
                await _servicioContabilidad.LiquidarServiciosAsync(empresaId, personaId);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("HistorialLiquidaciones")]
        public async Task<IActionResult> ObtenerHistorialLiquidaciones([FromQuery] DateTime fechaDesde, [FromQuery] DateTime fechaHasta, [FromQuery] int? personaId)
        {
            var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();
            if (!int.TryParse(empresaIdHeader, out int empresaId))
                return BadRequest("Header empresaId inválido.");

            if (fechaDesde == default || fechaHasta == default)
                return BadRequest("Debe especificar fechaDesde y fechaHasta.");

            try
            {
                var historial = await _servicioContabilidad.ObtenerHistorialLiquidacionesAsync(
                    empresaId,
                    fechaDesde,
                    fechaHasta,
                    personaId
                );

                return Ok(historial);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
