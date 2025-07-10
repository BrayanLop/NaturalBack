using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Configuracion;
using Natural.Models.Areas.Configuracion;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracionServicioController : ControllerBase
{
    private readonly ServicioConfiguracion _servicioConfiguracion;

    public ConfiguracionServicioController(ServicioConfiguracion servicioConfiguracion)
    {
        _servicioConfiguracion = servicioConfiguracion;
    }

    [HttpPost("Crear")]
    public async Task<IActionResult> Crear([FromBody] ConfiguracionServicioViewModel configuracion)
    {
        var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();
        if (!int.TryParse(empresaIdHeader, out int empresaId))
            return BadRequest("Header 'empresaId' inválido.");

        configuracion.EmpresaId = empresaId;

        try
        {
            var nuevaConfiguracion = await _servicioConfiguracion.CrearConfiguracionServicioAsync(configuracion);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevaConfiguracion.Id }, nuevaConfiguracion);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("Obtener")]
    public async Task<IActionResult> ObtenerTodos()
    {
        var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();
        if (!int.TryParse(empresaIdHeader, out int empresaId))
            return BadRequest("Header 'empresaId' inválido.");

        var configuraciones = await _servicioConfiguracion.ObtenerConfiguracionesPorEmpresaAsync(empresaId);
        return Ok(configuraciones);
    }

    [HttpGet("Obtener/{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var configuracion = await _servicioConfiguracion.ObtenerConfiguracionPorIdAsync(id);
        if (configuracion == null)
            return NotFound();

        return Ok(configuracion);
    }

    [HttpPut("Actualizar/{id}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ConfiguracionServicioViewModel configuracion)
    {
        if (id != configuracion.Id)
            return BadRequest("Id de configuración no coincide.");

        var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();
        if (!int.TryParse(empresaIdHeader, out int empresaId))
            return BadRequest("Header 'empresaId' inválido.");

        configuracion.EmpresaId = empresaId;

        try
        {
            var actualizado = await _servicioConfiguracion.ActualizarConfiguracionServicioAsync(configuracion);
            if (!actualizado)
                return NotFound();

            return Ok(configuracion);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("Eliminar/{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var eliminado = await _servicioConfiguracion.EliminarConfiguracionServicioAsync(id);
        if (!eliminado)
            return NotFound();

        return NoContent();
    }
}
