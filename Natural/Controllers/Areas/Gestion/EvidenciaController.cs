using Microsoft.AspNetCore.Mvc;
using Natural.Common;
using Natural.Data.Areas.Gestion;

namespace Natural.Controllers.Areas.Gestion;

[Route("api/[controller]")]
public class EvidenciaController : ApiControllerBase
{
    private readonly ServicioEvidencia _servicioEvidencia;

    public EvidenciaController(ServicioEvidencia servicioEvidencia)
    {
        _servicioEvidencia = servicioEvidencia;
    }

    [HttpPost("SubirEvidencia")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5MB máximo
    public async Task<IActionResult> SubirEvidencia([FromForm] int registroServicioId, [FromForm] IFormFile archivo)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        if (archivo == null || archivo.Length == 0)
            return BadRequest("Debe proporcionar un archivo");

        try
        {
            var evidencia = await _servicioEvidencia.SubirEvidenciaAsync(empresaId, registroServicioId, archivo);

            return Ok(new
            {
                mensaje = "Evidencia subida correctamente",
                evidencia
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("ObtenerEvidencias/{registroServicioId:int}")]
    public async Task<IActionResult> ObtenerEvidencias(int registroServicioId)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        var evidencias = await _servicioEvidencia.ObtenerEvidenciasPorRegistroAsync(empresaId, registroServicioId);
        return Ok(evidencias);
    }

    [HttpPost("ObtenerEvidenciasPorRegistros")]
    public async Task<IActionResult> ObtenerEvidenciasPorRegistros([FromBody] List<int> registroServicioIds)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        if (registroServicioIds == null || registroServicioIds.Count == 0)
            return BadRequest("Debe proporcionar al menos un ID de registro");

        var evidencias = await _servicioEvidencia.ObtenerEvidenciasPorRegistrosAsync(empresaId, registroServicioIds);
        return Ok(evidencias);
    }

    [HttpDelete("EliminarEvidencia/{evidenciaId:int}")]
    public async Task<IActionResult> EliminarEvidencia(int evidenciaId)
    {
        if (!TryObtenerEmpresaId(out var empresaId, out var errorResult))
            return errorResult!;

        var eliminado = await _servicioEvidencia.EliminarEvidenciaAsync(empresaId, evidenciaId);
        if (!eliminado)
            return NotFound("Evidencia no encontrada");

        return Ok(new { mensaje = "Evidencia eliminada correctamente" });
    }
}
