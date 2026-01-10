using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Contabilidad;
using Natural.Models.Areas.Contabilidad;

namespace Natural.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EgresosEmpresaController : ControllerBase
{
    private readonly ServicioContabilidad _servicioContabilidad;

    public EgresosEmpresaController(ServicioContabilidad servicioContabilidad)
    {
        _servicioContabilidad = servicioContabilidad;
    }

    [HttpPost("RegistrarEgreso")]
    public async Task<IActionResult> RegistrarEgreso([FromBody] EgresoEmpresaViewModel egresoEmpresa)
    {
        if (egresoEmpresa is null)
        {
            return BadRequest("El cuerpo de la solicitud no puede ser nulo.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        try
        {
            var egreso = await _servicioContabilidad.RegistrarEgresoAsync(empresaId, egresoEmpresa);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("ObtenerEgresosPorRango")]
    public async Task<IActionResult> ObtenerEgresosPorRango([FromQuery] DateOnly fechaInicio, [FromQuery] DateOnly fechaFin)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        var egresos = await _servicioContabilidad.ObtenerEgresosPorRangoAsync(empresaId, fechaInicio, fechaFin);
        return Ok(egresos);
    }

    private bool TryObtenerEmpresaId(out int empresaId, out IActionResult? errorResult)
    {
        errorResult = null;
        empresaId = default;

        var empresaIdHeader = Request.Headers["empresaId"].FirstOrDefault();
        if (!int.TryParse(empresaIdHeader, out empresaId))
        {
            errorResult = BadRequest("Header 'empresaId' inválido.");
            return false;
        }

        return true;
    }
}