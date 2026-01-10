using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Contabilidad;

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
        public async Task<IActionResult> PagosUltimosDias([FromQuery]int? personaId)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
                return errorResult!;

            var pagos = await _servicioContabilidad.ObtenerPagosAgrupadosUltimosDiasAsync(empresaId, personaId);
            return Ok(pagos);
        }

        [HttpGet("DetalleServiciosPorPersona/{personaId}")]
        public async Task<IActionResult> ObtenerDetallePorPersona(int personaId)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
                return errorResult!;

            var detalle = await _servicioContabilidad.ObtenerDetalleServiciosPorPersonaAsync(empresaId, personaId);
            return Ok(detalle);
        }

        [HttpPost("LiquidarPersona")]
        public async Task<IActionResult> LiquidarPersona([FromQuery] int personaId)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
                return errorResult!;

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
        public async Task<IActionResult> ObtenerHistorialLiquidaciones(
            [FromQuery] DateTime fechaDesde,
            [FromQuery] DateTime fechaHasta,
            [FromQuery] int? personaId)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
                return errorResult!;

            if (!TryValidarFechas(fechaDesde, fechaHasta, validarOrdenCronologico: false, out var errorFechas))
                return errorFechas!;

            try
            {
                var historial = await _servicioContabilidad.ObtenerHistorialLiquidacionesAsync(
                    empresaId,
                    fechaDesde,
                    fechaHasta,
                    personaId);

                return Ok(historial);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("HistorialIngresosEmpresa")]
        public async Task<IActionResult> ObtenerHistorialIngresosEmpresa(
            [FromQuery] DateTime fechaDesde,
            [FromQuery] DateTime fechaHasta)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
                return errorResult!;

            if (!TryValidarFechas(fechaDesde, fechaHasta, validarOrdenCronologico: true, out var errorFechas))
                return errorFechas!;

            try
            {
                var historial = await _servicioContabilidad.ObtenerHistorialIngresosEmpresaAsync(
                    empresaId,
                    fechaDesde,
                    fechaHasta);

                return Ok(historial);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private bool TryObtenerEmpresaId(out int empresaId, out IActionResult? errorResult)
        {
            errorResult = null;
            empresaId = default;

            var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();
            if (!int.TryParse(empresaIdHeader, out empresaId))
            {
                errorResult = BadRequest("Header 'empresaId' inválido.");
                return false;
            }

            return true;
        }

        private bool TryValidarFechas(DateTime fechaDesde, DateTime fechaHasta, bool validarOrdenCronologico, out IActionResult? errorResult)
        {
            errorResult = null;

            if (fechaDesde == default || fechaHasta == default)
            {
                errorResult = BadRequest("Debe especificar fechaDesde y fechaHasta.");
                return false;
            }

            if (validarOrdenCronologico && fechaDesde > fechaHasta)
            {
                errorResult = BadRequest("fechaDesde no puede ser mayor que fechaHasta.");
                return false;
            }

            return true;
        }
    }
}
