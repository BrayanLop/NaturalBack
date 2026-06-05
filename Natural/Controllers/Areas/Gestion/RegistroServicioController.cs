using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Gestion;
using Natural.Models.Areas.Gestion;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RegistroServicioController : ControllerBase
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
        {
            return BadRequest("Debe enviar al menos un registro.");
        }

        if (!TryGetHeaderInt("empresaId", out var empresaId, out var errorResult))
        {
            return errorResult!;
        }

        registros.ForEach(item => item.EmpresaId = empresaId);

        var registrosIds = await _servicioGestion.GuardarRegistrosServicioAsync(registros);

        if (registrosIds == null || registrosIds.Count == 0)
        {
            return StatusCode(500, "Hubo un problema al guardar los registros.");
        }

        return Ok(new
        {
            mensaje = "Registros guardados correctamente",
            registrosIds = registrosIds
        });
    }

    [HttpGet("ObtenerRegistros")]
    public async Task<IActionResult> ObtenerRegistros([FromQuery] int? personaId, [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin)
    {
        if (!TryGetHeaderInt("empresaId", out var empresaId, out var errorResult))
        {
            return errorResult!;
        }

        if (fechaInicio.HasValue != fechaFin.HasValue)
        {
            return BadRequest("Debe proporcionar ambos valores de fecha (inicio y fin).");
        }

        if (fechaInicio.HasValue && fechaFin.HasValue && fechaInicio > fechaFin)
        {
            return BadRequest("La fecha de inicio no puede ser mayor a la fecha de fin.");
        }

        var inicio = fechaInicio ?? DateTime.UtcNow.Date;
        var fin = fechaFin ?? DateTime.UtcNow.Date;

        var registros = await _servicioGestion.ObtenerRegistrosFechaActualAsync(empresaId, personaId ?? 0, inicio, fin);

        return Ok(registros);
    }

    [HttpGet("ObtenerRegistrosPersona")]
    public async Task<IActionResult> ObtenerRegistrosPersona()
    {
        if (!TryGetHeaderInt("empresaId", out var empresaId, out var errorResult))
        {
            return errorResult!;
        }

        if (!TryGetHeaderInt("personaId", out var personaId, out var personaError))
        {
            return personaError!;
        }

        var registros = await _servicioGestion.ObtenerRegistrosFechaActualPersonaAsync(empresaId, personaId);

        if (registros == null || registros.Count == 0)
        {
            return NotFound("No se encontraron registros para esta persona.");
        }

        return Ok(registros);
    }

    [HttpGet("ObtenerConsolidadoDia")]
    public async Task<IActionResult> ObtenerConsolidadoDia([FromQuery] int? personaId)
    {
        if (!TryGetHeaderInt("empresaId", out var empresaId, out var errorResult))
        {
            return errorResult!;
        }

        var consolidado = await _servicioGestion.ObtenerConsolidadoDiaAsync(empresaId, personaId ?? 0);

        return Ok(consolidado);
    }

    [HttpPatch("ActualizarConfirmado/{registroServicioId:int}")]
    public async Task<IActionResult> ActualizarConfirmado(int registroServicioId, [FromQuery] bool? confirmado)
    {
        try
        {
            if (!confirmado.HasValue)
                return BadRequest("Debe proporcionar el estado de confirmación.");

            if (!TryGetHeaderInt("empresaId", out var empresaId, out var errorResult))
            {
                return errorResult!;
            }

            var actualizado = await _servicioGestion.ActualizarConfirmadoAsync(registroServicioId, empresaId, confirmado.Value);
            if (!actualizado)
                return NotFound("No se pudo actualizar el registro especificado.");

            return Ok("Estado de confirmación actualizado correctamente.");
        }
        catch (Exception)
        {
            return Problem(
                detail: "Ocurrió un error inesperado al actualizar el registro.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPatch("Rechazar/{registroServicioId:int}")]
    public async Task<IActionResult> Rechazar(int registroServicioId, [FromQuery] string? motivo)
    {
        try
        {
            if (!TryGetHeaderInt("empresaId", out var empresaId, out var errorResult))
            {
                return errorResult!;
            }

            var rechazado = await _servicioGestion.RechazarAsync(registroServicioId, empresaId, motivo);
            if (!rechazado)
                return NotFound("No se pudo rechazar el registro especificado (no existe o ya está liquidado).");

            return Ok("Registro rechazado correctamente.");
        }
        catch (Exception)
        {
            return Problem(
                detail: "Ocurrió un error inesperado al rechazar el registro.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPatch("MarcarPropinaPagada/{registroServicioId:int}")]
    public async Task<IActionResult> MarcarPropinaPagada(int registroServicioId, [FromQuery] bool? pagada)
    {
        try
        {
            if (!TryGetHeaderInt("empresaId", out var empresaId, out var errorResult))
            {
                return errorResult!;
            }

            var actualizado = await _servicioGestion.MarcarPropinaPagadaAsync(registroServicioId, empresaId, pagada ?? true);
            if (!actualizado)
                return NotFound("No se pudo actualizar la propina del registro especificado.");

            return Ok("Estado de propina actualizado correctamente.");
        }
        catch (Exception)
        {
            return Problem(
                detail: "Ocurrió un error inesperado al actualizar la propina.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPatch("ActualizarFormaPago/{registroServicioId:int}")]
    public async Task<IActionResult> ActualizarFormaPago(int registroServicioId, [FromQuery] char? formaPago)
    {
        try
        {
            if (!formaPago.HasValue)
                return BadRequest("Debe proporcionar la forma de pago.");

            if (!TryGetHeaderInt("empresaId", out var empresaId, out var errorResult))
            {
                return errorResult!;
            }

            var actualizado = await _servicioGestion.ActualizarFormaPagoAsync(registroServicioId, empresaId, formaPago.Value);
            if (!actualizado)
                return NotFound("No se pudo actualizar el registro especificado.");

            return Ok("Forma de pago actualizada correctamente.");
        }
        catch (Exception)
        {
            return Problem(
                detail: "Ocurrió un error inesperado al actualizar el registro.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private bool TryGetHeaderInt(string headerName, out int value, out IActionResult? errorResult)
    {
        var headerValue = Request.Headers[headerName].FirstOrDefault();
        if (!int.TryParse(headerValue, out value))
        {
            errorResult = BadRequest($"Header '{headerName}' inválido.");
            return false;
        }

        errorResult = null;
        return true;
    }
}
