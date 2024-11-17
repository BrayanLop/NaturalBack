using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Estructura;
using Natural.Models.Areas.Estructura;

[ApiController]
[Route("api/[controller]")]
public class PersonaController : ControllerBase
{
    private readonly ServicioEstructura _servicioEstructura;

    public PersonaController(ServicioEstructura servicioEstructura)
    {
        _servicioEstructura = servicioEstructura;
    }

    [HttpPost("Crear")]
    public async Task<IActionResult> CrearPersona([FromBody] PersonaViewModel persona)
    {
        var nuevaPersona = await _servicioEstructura.CrearPersonaAsync(persona);
        return CreatedAtAction(nameof(ObtenerPersonaPorId), new { id = nuevaPersona.Id }, nuevaPersona);
    }

    [HttpGet("Obtener")]
    public async Task<IActionResult> ObtenerTodasLasPersonas()
    {
        var personas = await _servicioEstructura.ObtenerTodasLasPersonasAsync();
        return Ok(personas);
    }

    [HttpGet("Obtener/{id}")]
    public async Task<IActionResult> ObtenerPersonaPorId(int id)
    {
        var persona = await _servicioEstructura.ObtenerPersonaPorIdAsync(id);
        if (persona == null)
        {
            return NotFound();
        }

        return Ok(persona);
    }

    [HttpPut("Actualizar/{id}")]
    public async Task<IActionResult> ActualizarPersona(int id, [FromBody] PersonaViewModel persona)
    {
        if (id != persona.Id)
        {
            return BadRequest();
        }

        var actualizado = await _servicioEstructura.ActualizarPersonaAsync(persona);
        if (!actualizado)
        {
            return NotFound();
        }

        return Ok(persona);
    }

    [HttpDelete("Eliminar/{id}")]
    public async Task<IActionResult> EliminarPersona(int id)
    {
        var eliminado = await _servicioEstructura.EliminarPersonaAsync(id);
        if (!eliminado)
        {
            return NotFound();
        }

        return NoContent();
    }
}
