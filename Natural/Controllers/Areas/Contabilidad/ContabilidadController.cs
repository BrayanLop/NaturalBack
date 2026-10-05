using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Common;
using Natural.Data.Areas.Contabilidad;

namespace Natural.Controllers.Areas.Contabilidad
{
    [Route("api/[controller]")]
    public class ContabilidadController : ApiControllerBase
    {
        private readonly ServicioContabilidad _servicioContabilidad;

        public ContabilidadController(ServicioContabilidad servicioContabilidad)
        {
            _servicioContabilidad = servicioContabilidad;
        }

        [HttpGet("PagosUltimosDias")]
        public async Task<IActionResult> PagosUltimosDias([FromQuery] int? personaId)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
                return errorResult!;

            if (!TryRestringirAPersonaSesion(ref personaId, out errorResult))
                return errorResult!;

            var pagos = await _servicioContabilidad.ObtenerPagosAgrupadosUltimosDiasAsync(empresaId, personaId);
            return Ok(pagos);
        }

        [HttpGet("DetalleServiciosPorPersona/{personaId}")]
        public async Task<IActionResult> ObtenerDetallePorPersona(int personaId)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
                return errorResult!;

            if (!PuedeConsultarPersona(personaId))
                return Forbid();

            var detalle = await _servicioContabilidad.ObtenerDetalleServiciosPorPersonaAsync(empresaId, personaId);
            return Ok(detalle);
        }

        [HttpPost("LiquidarPersona")]
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> LiquidarPersona([FromQuery] int personaId, [FromQuery] string? egresosId)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
                return errorResult!;

            if (!TryParsearIds(egresosId, out var idsEgresos))
                return BadRequest("El parámetro egresosId debe ser una lista de números separados por coma.");

            try
            {
                await _servicioContabilidad.LiquidarServiciosAsync(empresaId, personaId, idsEgresos);
                return Ok();
            }
            catch (InvalidOperationException ex)
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

            if (!TryRestringirAPersonaSesion(ref personaId, out errorResult))
                return errorResult!;

            if (!TryValidarFechas(fechaDesde, fechaHasta, validarOrdenCronologico: false, out var errorFechas))
                return errorFechas!;

            var historial = await _servicioContabilidad.ObtenerHistorialLiquidacionesAsync(empresaId, fechaDesde, fechaHasta, personaId);
            return Ok(historial);
        }

        [HttpGet("HistorialIngresosEmpresa")]
        [Authorize(Policy = Roles.PoliticaGestion)]
        public async Task<IActionResult> ObtenerHistorialIngresosEmpresa(
            [FromQuery] DateTime fechaDesde,
            [FromQuery] DateTime fechaHasta)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
                return errorResult!;

            if (!TryValidarFechas(fechaDesde, fechaHasta, validarOrdenCronologico: true, out var errorFechas))
                return errorFechas!;

            var historial = await _servicioContabilidad.ObtenerHistorialIngresosEmpresaAsync(empresaId, fechaDesde, fechaHasta);
            return Ok(historial);
        }

        [HttpGet("ConsolidadoIngresosEgresos")]
        [Authorize(Policy = Roles.PoliticaGestion)]
        public async Task<IActionResult> ConsolidadoIngresosEgresos(
            [FromQuery] DateTime fechaDesde,
            [FromQuery] DateTime fechaHasta)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
                return errorResult!;

            if (!TryValidarFechas(fechaDesde, fechaHasta, validarOrdenCronologico: true, out var errorFechas))
                return errorFechas!;

            var consolidado = await _servicioContabilidad.ObtenerConsolidadoIngresosEgresosAsync(empresaId, fechaDesde, fechaHasta);
            return Ok(consolidado);
        }

        [HttpGet("ConsolidadoFormaPago")]
        [Authorize(Policy = Roles.PoliticaGestion)]
        public async Task<IActionResult> ConsolidadoFormaPago([FromQuery] DateTime fechaDesde, [FromQuery] DateTime fechaHasta, [FromQuery] string? formaPago)
        {
            if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
                return errorResult!;

            if (!TryValidarFechas(fechaDesde, fechaHasta, validarOrdenCronologico: true, out var errorFechas))
                return errorFechas!;

            var consolidado = await _servicioContabilidad.ObtenerConsolidadoPorFormaPagoAsync(empresaId, fechaDesde, fechaHasta, formaPago);
            return Ok(consolidado);
        }

        /// <summary>Si quien consulta no es admin ni super empleado, solo puede ver su propia información.</summary>
        private bool TryRestringirAPersonaSesion(ref int? personaId, out IActionResult? errorResult)
        {
            errorResult = null;
            if (PuedeGestionar)
                return true;

            if (!TryObtenerPersonaId(out var personaSesion, out errorResult))
                return false;

            personaId = personaSesion;
            return true;
        }

        private static bool TryParsearIds(string? valor, out List<int> ids)
        {
            ids = new List<int>();
            if (string.IsNullOrWhiteSpace(valor))
                return true;

            foreach (var parte in valor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!int.TryParse(parte, out var id))
                    return false;

                ids.Add(id);
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
