using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Gestion;
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

    [HttpGet("ObtenerRegistros")]
    public async Task<IActionResult> ObtenerRegistros()
    {
        var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();

        if (!int.TryParse(empresaIdHeader, out int empresaId))
            return BadRequest("Header 'empresaId' inválido.");

        var registros = await _servicioGestion.ObtenerRegistrosFechaActualAsync(empresaId);

        if (registros == null || registros.Count == 0)
        {
            return NotFound("No se encontraron registros para esta persona.");
        }

        return Ok(registros);
    }

    [HttpGet("ObtenerRegistrosPersona")]
    public async Task<IActionResult> ObtenerRegistrosPersona()
    {
        var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();
        var personaIdHeader = Request.Headers["personaId"].FirstOrDefault();

        if (!int.TryParse(empresaIdHeader, out int empresaId))
            return BadRequest("Header 'empresaId' inválido.");

        if (!int.TryParse(personaIdHeader, out int personaId))
            return BadRequest("Header 'personaId' inválido.");

        var registros = await _servicioGestion.ObtenerRegistrosFechaActualPersonaAsync(empresaId, personaId);

        if (registros == null || registros.Count == 0)
        {
            return NotFound("No se encontraron registros para esta persona.");
        }

        return Ok(registros);
    }

    [HttpGet("ObtenerConsolidadoDia")]
    public async Task<IActionResult> ObtenerConsolidadoDia()
    {
        var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();

        if (!int.TryParse(empresaIdHeader, out int empresaId))
            return BadRequest("Header 'empresaId' inválido.");

        var consolidado = await _servicioGestion.ObtenerConsolidadoDiaAsync(empresaId);

        if (consolidado == null || consolidado.Count == 0)
            return NotFound("No hay registros para hoy.");

        return Ok(consolidado);
    }

}
