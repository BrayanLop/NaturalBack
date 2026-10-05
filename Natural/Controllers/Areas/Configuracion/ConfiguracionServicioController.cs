using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Common;
using Natural.Data.Areas.Configuracion;
using Natural.Models.Areas.Configuracion;

[Route("api/[controller]")]
public class ConfiguracionServicioController : ApiControllerBase
{
    private readonly ServicioConfiguracion _servicioConfiguracion;

    public ConfiguracionServicioController(ServicioConfiguracion servicioConfiguracion)
    {
        _servicioConfiguracion = servicioConfiguracion;
    }

    [HttpPost("Crear")]
    [Authorize(Policy = Roles.PoliticaGestion)]
    public async Task<IActionResult> Crear([FromBody] ConfiguracionServicioViewModel configuracion)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        configuracion.EmpresaId = empresaId;

        try
        {
            var nuevaConfiguracion = await _servicioConfiguracion.CrearConfiguracionServicioAsync(configuracion);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevaConfiguracion.Id }, nuevaConfiguracion);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("Obtener")]
    public async Task<IActionResult> ObtenerTodos()
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        var configuraciones = await _servicioConfiguracion.ObtenerConfiguracionesPorEmpresaAsync(empresaId);
        return Ok(configuraciones);
    }

    [HttpGet("Obtener/{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        var configuracion = await _servicioConfiguracion.ObtenerConfiguracionPorIdAsync(id, empresaId);
        if (configuracion == null)
            return NotFound();

        return Ok(configuracion);
    }

    [HttpPut("Actualizar/{id}")]
    [Authorize(Policy = Roles.PoliticaGestion)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ConfiguracionServicioViewModel configuracion)
    {
        if (id != configuracion.Id)
            return BadRequest("Id de configuración no coincide.");

        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        configuracion.EmpresaId = empresaId;

        try
        {
            var actualizado = await _servicioConfiguracion.ActualizarConfiguracionServicioAsync(configuracion);
            if (!actualizado)
                return NotFound();

            return Ok(configuracion);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("Eliminar/{id}")]
    [Authorize(Policy = Roles.PoliticaGestion)]
    public async Task<IActionResult> Eliminar(int id)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        var eliminado = await _servicioConfiguracion.EliminarConfiguracionServicioAsync(id, empresaId);
        if (!eliminado)
            return NotFound();

        return NoContent();
    }
}
