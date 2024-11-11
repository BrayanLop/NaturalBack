using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Estructura;
using Natural.Models.Areas.Estructura;

[ApiController]
[Route("api/[controller]")]
public class ServicioController : ControllerBase
{
    private readonly ServicioEstructura _servicioEstructura;

    public ServicioController(ServicioEstructura servicioEstructura)
    {
        _servicioEstructura = servicioEstructura;
    }

    [HttpPost("Crear")]
    public async Task<IActionResult> CrearServicio([FromBody] ServicioViewModel servicio)
    {
        var nuevoServicio = await _servicioEstructura.CrearServicioAsync(servicio);
        return CreatedAtAction(nameof(ObtenerServicioPorId), new { id = nuevoServicio.Id }, nuevoServicio);
    }

    [HttpGet("Obtener")]
    public async Task<IActionResult> ObtenerTodosLosServicios()
    {
        var servicios = await _servicioEstructura.ObtenerTodosLosServicioAsync();
        return Ok(servicios);
    }

    [HttpGet("Obtener/{id}")]
    public async Task<IActionResult> ObtenerServicioPorId(int id)
    {
        var servicio = await _servicioEstructura.ObtenerServicioPorIdAsync(id);
        if (servicio == null)
        {
            return NotFound();
        }

        return Ok(servicio);
    }

    [HttpPut("Actualizar/{id}")]
    public async Task<IActionResult> ActualizarServicio(int id, [FromBody] ServicioViewModel servicio)
    {
        if (id != servicio.Id)
        {
            return BadRequest();
        }

        var actualizado = await _servicioEstructura.ActualizarServicioAsync(servicio);
        if (!actualizado)
        {
            return NotFound();
        }

        return Ok(servicio);
    }

    [HttpDelete("Eliminar/{id}")]
    public async Task<IActionResult> EliminarServicio(int id)
    {
        var eliminado = await _servicioEstructura.EliminarServicioAsync(id);
        if (!eliminado)
        {
            return NotFound();
        }

        return NoContent();
    }
}
