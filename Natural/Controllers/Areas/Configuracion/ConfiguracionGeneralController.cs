using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Configuracion;
using Natural.Models.Areas.Configuracion;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ConfiguracionGeneralController : ControllerBase
{
    private readonly ServicioConfiguracion _servicioConfiguracion;

    public ConfiguracionGeneralController(ServicioConfiguracion servicioConfiguracion)
    {
        _servicioConfiguracion = servicioConfiguracion;
    }

    [HttpPost("Crear")]
    public async Task<IActionResult> Crear([FromBody] ConfiguracionGeneralViewModel configuracion)
    {
        if (configuracion is null)
            return BadRequest("Debe proporcionar la configuración general.");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryGetEmpresaId(out var empresaId, out var errorResult))
            return errorResult;

        configuracion.EmpresaId = empresaId;

        try
        {
            var nuevaConfiguracion = await _servicioConfiguracion.CambiarPeriodicidadAsync(configuracion, configuracion.Periodicidad);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevaConfiguracion.EmpresaId }, nuevaConfiguracion);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Se produjo un error inesperado al crear la configuración.");
        }
    }

    [HttpGet("Obtener/{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        if (!TryGetEmpresaId(out _, out var errorResult))
            return errorResult;

        var configuracion = await _servicioConfiguracion.ObtenerConfiguracionGeneralPorEmpresaAsync(id);
        if (configuracion == null)
            return NotFound("No se encontró configuración general para la empresa solicitada.");

        return Ok(configuracion);
    }

    [HttpPut("Actualizar/{id}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ConfiguracionGeneralViewModel configuracion, [FromQuery] string periodicidadOriginal)
    {
        if (configuracion is null)
            return BadRequest("Debe proporcionar la configuración general.");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryGetEmpresaId(out var empresaId, out var errorResult))
            return errorResult;

        if (id != empresaId)
            return BadRequest("El id de la ruta no coincide con el header 'empresaId'.");

        if (string.IsNullOrWhiteSpace(periodicidadOriginal))
            return BadRequest("Debe proporcionar la periodicidad original.");

        configuracion.EmpresaId = empresaId;

        try
        {
            var nuevaPeriodicidad = configuracion.Periodicidad;
            configuracion.Periodicidad = periodicidadOriginal;

            var actualizado = await _servicioConfiguracion.CambiarPeriodicidadAsync(configuracion, nuevaPeriodicidad);
            if (actualizado == null)
                return NotFound("No se encontró la configuración general a actualizar.");

            return Ok(actualizado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Se produjo un error inesperado al actualizar la configuración.");
        }
    }

    private bool TryGetEmpresaId(out int empresaId, out IActionResult? errorResult)
    {
        empresaId = default;
        errorResult = null;

        var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();
        if (!int.TryParse(empresaIdHeader, out empresaId))
        {
            errorResult = BadRequest("Header 'empresaId' inválido.");
            return false;
        }

        return true;
    }
}