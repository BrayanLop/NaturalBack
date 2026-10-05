using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Common;
using Natural.Data.Areas.Estructura;
using Natural.Models.Areas.Estructura;

[Route("api/[controller]")]
public class PersonaController : ApiControllerBase
{
    private readonly ServicioEstructura _servicioEstructura;

    public PersonaController(ServicioEstructura servicioEstructura)
    {
        _servicioEstructura = servicioEstructura;
    }

    [HttpPost("Crear")]
    [Authorize(Policy = Roles.PoliticaGestion)]
    public async Task<IActionResult> CrearPersona([FromBody] PersonaViewModel persona)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        persona.EmpresaId = empresaId;

        var nuevaPersona = await _servicioEstructura.CrearPersonaAsync(persona);
        return CreatedAtAction(nameof(ObtenerPersonaPorId), new { id = nuevaPersona.Id }, nuevaPersona);
    }

    [HttpGet("Obtener")]
    public async Task<IActionResult> ObtenerTodasLasPersonas()
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        var personas = await _servicioEstructura.ObtenerTodasLasPersonasAsync(empresaId);
        return Ok(personas);
    }

    [HttpGet("Obtener/{id}")]
    public async Task<IActionResult> ObtenerPersonaPorId(int id)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        var persona = await _servicioEstructura.ObtenerPersonaPorIdAsync(id, empresaId);
        if (persona == null)
            return NotFound();

        var rol = await _servicioEstructura.ObtenerRol(id);
        if (rol == null)
            return NotFound();

        persona.Rol = rol;

        return Ok(persona);
    }

    [HttpPut("Actualizar/{id}")]
    [Authorize(Policy = Roles.PoliticaGestion)]
    public async Task<IActionResult> ActualizarPersona(int id, [FromBody] PersonaViewModel persona)
    {
        if (id <= 0)
            return BadRequest();

        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        persona.EmpresaId = empresaId;
        persona.Id = id;

        var actualizado = await _servicioEstructura.ActualizarPersonaAsync(persona);
        if (!actualizado)
            return NotFound();

        return Ok(persona);
    }

    [HttpDelete("Eliminar/{id}")]
    [Authorize(Policy = Roles.PoliticaGestion)]
    public async Task<IActionResult> EliminarPersona(int id)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        var eliminado = await _servicioEstructura.EliminarPersonaAsync(id, empresaId);
        if (!eliminado)
            return NotFound();

        return NoContent();
    }
}
