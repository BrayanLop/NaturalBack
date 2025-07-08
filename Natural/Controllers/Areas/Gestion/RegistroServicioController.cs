using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Gestion;
using Natural.Data.Domain.Estructura;
using Natural.Models.Areas.Gestion;

[ApiController]
[Route("api/[controller]")]
public class RegistroServicioController : ControllerBase
{
    private readonly ServicioGestion _servicioGestion;

    public RegistroServicioController(ServicioGestion servicioGestion)
    {
        _servicioGestion = servicioGestion;
    }

    [HttpPost("Guardar")]
    public async Task<IActionResult> GuardarRegistroServicios([FromBody] List<RegistroServicioViewModel> registros)
    {
        if (registros == null || registros.Count == 0)
        {
            return BadRequest("Debe enviar al menos un registro.");
        }

        var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();

        if (!int.TryParse(empresaIdHeader, out int empresaId))
            return BadRequest("Header 'empresaId' inválido.");

        registros.ForEach(item => item.EmpresaId = empresaId);

        var resultado = await _servicioGestion.GuardarRegistrosServicioAsync(registros);

        if (!resultado)
        {
            return StatusCode(500, "Hubo un problema al guardar los registros.");
        }

        return Ok("Registros guardados correctamente.");
    }

    [HttpGet("ObtenerPorPersona/{personaId}")]
    public async Task<IActionResult> ObtenerRegistrosPorPersona()
    {
        var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();

        if (!int.TryParse(empresaIdHeader, out int empresaId))
            return BadRequest("Header 'empresaId' inválido.");

        var registros = await _servicioGestion.ObtenerRegistrosPorPersonaAsync(empresaId);

        if (registros == null || registros.Count == 0)
        {
            return NotFound("No se encontraron registros para esta persona.");
        }

        return Ok(registros);
    }
}
