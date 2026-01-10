using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Estructura;
using Natural.Models.Areas.Estructura;

[Authorize]
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
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
        {
            return errorResult!;
        }

        persona.EmpresaId = empresaId;

        var nuevaPersona = await _servicioEstructura.CrearPersonaAsync(persona);
        return CreatedAtAction(nameof(ObtenerPersonaPorId), new { id = nuevaPersona.Id }, nuevaPersona);
    }

    [HttpGet("Obtener")]
    public async Task<IActionResult> ObtenerTodasLasPersonas()
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
        {
            return errorResult!;
        }

        var personas = await _servicioEstructura.ObtenerTodasLasPersonasAsync(empresaId);
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

        var rol = await _servicioEstructura.ObtenerRol(id);
        if (rol == null)
        {
            return NotFound();
        }

        persona.Rol = rol;

        return Ok(persona);
    }

    [HttpPut("Actualizar/{id}")]
    public async Task<IActionResult> ActualizarPersona(int id, [FromBody] PersonaViewModel persona)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
        {
            return errorResult!;
        }

        persona.EmpresaId = empresaId;
        persona.Id = id;

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

    private bool TryObtenerEmpresaId(out int empresaId, out IActionResult? errorResult)
    {
        empresaId = 0;
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
