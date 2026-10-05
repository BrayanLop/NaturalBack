using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Common;
using Natural.Data.Areas.Gestion;
using Natural.Models.Areas.Gestion;

[Route("api/[controller]")]
public class RegistroServicioController : ApiControllerBase
{
    private readonly ServicioGestion _servicioGestion;

    public RegistroServicioController(ServicioGestion servicioGestion)
    {
        _servicioGestion = servicioGestion;
    }

    [HttpPost("Guardar")]
    public async Task<IActionResult> GuardarRegistroServicios([FromBody] List<RegistroServicioViewModel> registros)
    {
        if (registros == null || registros.Count == 0)
            return BadRequest("Debe enviar al menos un registro.");

        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        // Un trabajador solo registra servicios propios y sin confirmar; la confirmación es del administrador.
        if (!PuedeGestionar)
        {
            if (!TryObtenerPersonaId(out var personaId, out errorResult))
                return errorResult!;

            registros.ForEach(r =>
            {
                r.PersonaId = personaId;
                r.Confirmado = false;
            });
        }

        try
        {
            var registrosIds = await _servicioGestion.GuardarRegistrosServicioAsync(empresaId, registros);
            return Ok(new
            {
                mensaje = "Registros guardados correctamente",
                registrosIds
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <remarks>
    /// El administrador ve los registros de todas las personas; los demás roles solo los propios.
    /// El parámetro personaId se conserva por compatibilidad con el front, pero ya no define permisos.
    /// </remarks>
    [HttpGet("ObtenerRegistros")]
    public async Task<IActionResult> ObtenerRegistros([FromQuery] int? personaId, [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        if (fechaInicio.HasValue != fechaFin.HasValue)
            return BadRequest("Debe proporcionar ambos valores de fecha (inicio y fin).");

        if (fechaInicio.HasValue && fechaFin.HasValue && fechaInicio > fechaFin)
            return BadRequest("La fecha de inicio no puede ser mayor a la fecha de fin.");

        if (!TryObtenerFiltroPersona(out var personaIdFiltro, out errorResult))
            return errorResult!;

        var (inicioUtc, finUtc) = fechaInicio.HasValue && fechaFin.HasValue
            ? ServicioGestion.NormalizarRangoUtc(fechaInicio.Value, fechaFin.Value)
            : TimeZoneHelper.RangoDiaActualUtc();

        var registros = await _servicioGestion.ObtenerRegistrosFechaActualAsync(empresaId, personaIdFiltro, inicioUtc, finUtc);

        return Ok(registros);
    }

    [HttpGet("ObtenerRegistrosPersona")]
    public async Task<IActionResult> ObtenerRegistrosPersona()
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        if (!TryObtenerPersonaId(out var personaId, out errorResult))
            return errorResult!;

        var registros = await _servicioGestion.ObtenerRegistrosFechaActualPersonaAsync(empresaId, personaId);

        if (registros.Count == 0)
            return NotFound("No se encontraron registros para esta persona.");

        return Ok(registros);
    }

    [HttpGet("ObtenerConsolidadoDia")]
    public async Task<IActionResult> ObtenerConsolidadoDia([FromQuery] int? personaId)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        if (!TryObtenerFiltroPersona(out var personaIdFiltro, out errorResult))
            return errorResult!;

        var consolidado = await _servicioGestion.ObtenerConsolidadoDiaAsync(empresaId, personaIdFiltro);

        return Ok(consolidado);
    }

    [HttpPatch("ActualizarConfirmado/{registroServicioId:int}")]
    [Authorize(Policy = Roles.PoliticaGestion)]
    public async Task<IActionResult> ActualizarConfirmado(int registroServicioId, [FromQuery] bool? confirmado)
    {
        if (!confirmado.HasValue)
            return BadRequest("Debe proporcionar el estado de confirmación.");

        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        var actualizado = await _servicioGestion.ActualizarConfirmadoAsync(registroServicioId, empresaId, confirmado.Value);
        if (!actualizado)
            return NotFound("No se pudo actualizar el registro especificado.");

        return Ok("Estado de confirmación actualizado correctamente.");
    }

    [HttpPatch("ActualizarFormaPago/{registroServicioId:int}")]
    public async Task<IActionResult> ActualizarFormaPago(int registroServicioId, [FromQuery] char? formaPago)
    {
        if (!formaPago.HasValue)
            return BadRequest("Debe proporcionar la forma de pago.");

        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        int? personaIdFiltro = null;
        if (!PuedeGestionar)
        {
            if (!TryObtenerPersonaId(out var personaId, out errorResult))
                return errorResult!;

            personaIdFiltro = personaId;
        }

        var actualizado = await _servicioGestion.ActualizarFormaPagoAsync(registroServicioId, empresaId, formaPago.Value, personaIdFiltro);
        if (!actualizado)
            return NotFound("No se pudo actualizar el registro especificado.");

        return Ok("Forma de pago actualizada correctamente.");
    }

    /// <summary>Administrador: sin filtro (null). Otros roles: solo su propia persona.</summary>
    private bool TryObtenerFiltroPersona(out int? personaIdFiltro, out IActionResult? errorResult)
    {
        personaIdFiltro = null;
        errorResult = null;

        if (EsAdministrador)
            return true;

        if (!TryObtenerPersonaId(out var personaId, out errorResult))
            return false;

        personaIdFiltro = personaId;
        return true;
    }
}
