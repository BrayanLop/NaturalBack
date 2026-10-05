using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Common;
using Natural.Data.Areas.Estructura;
using Natural.Models.Areas.Estructura;

[Route("api/[controller]")]
public class ServicioController : ApiControllerBase
{
    private readonly ServicioEstructura _servicioEstructura;

    public ServicioController(ServicioEstructura servicioEstructura)
    {
        _servicioEstructura = servicioEstructura;
    }

    [HttpPost("Crear")]
    [Authorize(Policy = Roles.PoliticaGestion)]
    public async Task<IActionResult> CrearServicio([FromBody] ServicioViewModel servicio)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        servicio.EmpresaId = empresaId;

        var nuevoServicio = await _servicioEstructura.CrearServicioAsync(servicio);
        return CreatedAtAction(nameof(ObtenerServicioPorId), new { id = nuevoServicio.Id }, nuevoServicio);
    }

    [HttpGet("Obtener")]
    public async Task<IActionResult> ObtenerTodosLosServicios()
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        var servicios = await _servicioEstructura.ObtenerTodosLosServicioAsync(empresaId);
        return Ok(servicios);
    }

    [HttpGet("Obtener/{id}")]
    public async Task<IActionResult> ObtenerServicioPorId(int id)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        var servicio = await _servicioEstructura.ObtenerServicioPorIdAsync(id, empresaId);
        if (servicio == null)
            return NotFound();

        return Ok(servicio);
    }

    [HttpPut("Actualizar/{id}")]
    [Authorize(Policy = Roles.PoliticaGestion)]
    public async Task<IActionResult> ActualizarServicio(int id, [FromBody] ServicioViewModel servicio)
    {
        if (id != servicio.Id)
            return BadRequest();

        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        servicio.EmpresaId = empresaId;

        var actualizado = await _servicioEstructura.ActualizarServicioAsync(servicio);
        if (!actualizado)
            return NotFound();

        return Ok(servicio);
    }

    [HttpDelete("Eliminar/{id}")]
    [Authorize(Policy = Roles.PoliticaGestion)]
    public async Task<IActionResult> EliminarServicio(int id)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        var eliminado = await _servicioEstructura.EliminarServicioAsync(id, empresaId);
        if (!eliminado)
            return NotFound();

        return NoContent();
    }
}
