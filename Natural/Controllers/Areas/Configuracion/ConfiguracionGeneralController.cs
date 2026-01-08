using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Configuracion;
using Natural.Models.Areas.Configuracion;

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
        var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();
        if (!int.TryParse(empresaIdHeader, out int empresaId))
            return BadRequest("Header 'empresaId' inválido.");

        configuracion.EmpresaId = empresaId;

        try
        {
            var nuevaConfiguracion = await _servicioConfiguracion.CambiarPeriodicidadAsync(configuracion, configuracion.Periodicidad);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevaConfiguracion.EmpresaId }, nuevaConfiguracion);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("Obtener/{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();
        if (!int.TryParse(empresaIdHeader, out int empresaId))
            return BadRequest("Header 'empresaId' inválido.");

        var configuracion = await _servicioConfiguracion.ObtenerConfiguracionGeneralPorEmpresaAsync(id);
        if (configuracion == null)
            return NotFound();

        return Ok(configuracion);
    }

    [HttpPut("Actualizar/{id}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ConfiguracionGeneralViewModel configuracion, [FromQuery] string periodicidadOriginal)
    {
        if (id != configuracion.EmpresaId)
            return BadRequest("Id de configuración no coincide.");

        var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();
        if (!int.TryParse(empresaIdHeader, out int empresaId))
            return BadRequest("Header 'empresaId' inválido.");

        configuracion.EmpresaId = empresaId;

        try
        {
            var nuevaPeriodicidad = configuracion.Periodicidad;
            configuracion.Periodicidad = periodicidadOriginal;
            var actualizado = await _servicioConfiguracion.CambiarPeriodicidadAsync(configuracion, nuevaPeriodicidad);
            if (actualizado == null)
                return NotFound();

            return Ok(configuracion);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
 