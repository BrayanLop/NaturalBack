using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Common;
using Natural.Data.Areas.Configuracion;
using Natural.Models.Areas.Configuracion;

[Route("api/[controller]")]
public class ConfiguracionGeneralController : ApiControllerBase
{
    private readonly ServicioConfiguracion _servicioConfiguracion;

    public ConfiguracionGeneralController(ServicioConfiguracion servicioConfiguracion)
    {
        _servicioConfiguracion = servicioConfiguracion;
    }

    [HttpPost("Crear")]
    [Authorize(Policy = Roles.PoliticaGestion)]
    public async Task<IActionResult> Crear([FromBody] ConfiguracionGeneralViewModel configuracion)
    {
        if (configuracion is null)
            return BadRequest("Debe proporcionar la configuración general.");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

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
    }

    /// <param name="id">Id de la empresa; debe coincidir con la empresa de la sesión.</param>
    [HttpGet("Obtener/{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        if (id != empresaId)
            return Forbid();

        var configuracion = await _servicioConfiguracion.ObtenerConfiguracionGeneralPorEmpresaAsync(empresaId);
        if (configuracion == null)
            return NotFound("No se encontró configuración general para la empresa solicitada.");

        return Ok(configuracion);
    }

    [HttpPut("Actualizar/{id}")]
    [Authorize(Policy = Roles.PoliticaGestion)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ConfiguracionGeneralViewModel configuracion, [FromQuery] string periodicidadOriginal)
    {
        if (configuracion is null)
            return BadRequest("Debe proporcionar la configuración general.");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        if (id != empresaId)
            return BadRequest("El id de la ruta no coincide con la empresa de la sesión.");

        if (string.IsNullOrWhiteSpace(periodicidadOriginal))
            return BadRequest("Debe proporcionar la periodicidad original.");

        configuracion.EmpresaId = empresaId;

        try
        {
            var nuevaPeriodicidad = configuracion.Periodicidad;
            configuracion.Periodicidad = periodicidadOriginal;

            var actualizado = await _servicioConfiguracion.CambiarPeriodicidadAsync(configuracion, nuevaPeriodicidad);
            return Ok(actualizado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
