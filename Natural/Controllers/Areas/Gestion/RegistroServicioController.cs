using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Gestion;
using Natural.Models.Areas.Gestion;

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

        var resultado = await _servicioGestion.GuardarRegistrosServicioAsync(registros);

        if (!resultado)
        {
            return StatusCode(500, "Hubo un problema al guardar los registros.");
        }

        return Ok("Registros guardados correctamente.");
    }

    [HttpGet("ObtenerRegistros")]
    public async Task<IActionResult> ObtenerRegistros([FromQuery] int? personaId)
    {
        if (!TryGetHeaderInt("empresaId", out var empresaId, out var errorResult))
        {
            return errorResult!;
        }

        var registros = await _servicioGestion.ObtenerRegistrosFechaActualAsync(empresaId, personaId ?? 0);

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
