using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Common;
using Natural.Data.Areas.Contabilidad;
using Natural.Models.Areas.Contabilidad;

namespace Natural.Api.Controllers;

[Route("api/[controller]")]
public class EgresosEmpresaController : ApiControllerBase
{
    private readonly ServicioContabilidad _servicioContabilidad;

    public EgresosEmpresaController(ServicioContabilidad servicioContabilidad)
    {
        _servicioContabilidad = servicioContabilidad;
    }

    [HttpPost("RegistrarEgreso")]
    [Authorize(Policy = Roles.PoliticaGestion)]
    public async Task<IActionResult> RegistrarEgreso([FromBody] EgresoEmpresaViewModel egresoEmpresa)
    {
        if (egresoEmpresa is null)
            return BadRequest("El cuerpo de la solicitud no puede ser nulo.");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        try
        {
            await _servicioContabilidad.RegistrarEgresoAsync(empresaId, egresoEmpresa);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("ObtenerEgresosPorRango")]
    [Authorize(Policy = Roles.PoliticaGestion)]
    public async Task<IActionResult> ObtenerEgresosPorRango([FromQuery] DateOnly fechaInicio, [FromQuery] DateOnly fechaFin)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        var egresos = await _servicioContabilidad.ObtenerEgresosPorRangoAsync(empresaId, fechaInicio, fechaFin);
        return Ok(egresos);
    }

    [HttpGet("ObtenerEgresosPorPersona")]
    public async Task<IActionResult> ObtenerEgresosPorPersona([FromQuery] int personaId)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        if (!PuedeConsultarPersona(personaId))
            return Forbid();

        var egresos = await _servicioContabilidad.ObtenerEgresosPorPersonaAsync(empresaId, personaId);
        return Ok(egresos);
    }
}
