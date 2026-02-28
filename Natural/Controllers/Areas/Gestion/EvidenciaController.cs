using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Natural.Data.Areas.Gestion;

namespace Natural.Controllers.Areas.Gestion;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EvidenciaController : ControllerBase
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
        try
        {
            if (!TryGetHeaderInt("empresaId", out var empresaId, out var errorResult))
            {
                return errorResult!;
            }

            if (archivo == null || archivo.Length == 0)
            {
                return BadRequest("Debe proporcionar un archivo");
            }

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
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al subir la evidencia", detalle = ex.Message });
        }
    }

    [HttpGet("ObtenerEvidencias/{registroServicioId:int}")]
    public async Task<IActionResult> ObtenerEvidencias(int registroServicioId)
    {
        try
        {
            if (!TryGetHeaderInt("empresaId", out var empresaId, out var errorResult))
            {
                return errorResult!;
            }

            var evidencias = await _servicioEvidencia.ObtenerEvidenciasPorRegistroAsync(empresaId, registroServicioId);

            return Ok(evidencias);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener las evidencias", detalle = ex.Message });
        }
    }

    [HttpPost("ObtenerEvidenciasPorRegistros")]
    public async Task<IActionResult> ObtenerEvidenciasPorRegistros([FromBody] List<int> registroServicioIds)
    {
        try
        {
            if (!TryGetHeaderInt("empresaId", out var empresaId, out var errorResult))
            {
                return errorResult!;
            }

            if (registroServicioIds == null || !registroServicioIds.Any())
            {
                return BadRequest("Debe proporcionar al menos un ID de registro");
            }

            var evidencias = await _servicioEvidencia.ObtenerEvidenciasPorRegistrosAsync(empresaId, registroServicioIds);

            return Ok(evidencias);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener las evidencias", detalle = ex.Message });
        }
    }

    [HttpDelete("EliminarEvidencia/{evidenciaId:int}")]
    public async Task<IActionResult> EliminarEvidencia(int evidenciaId)
    {
        try
        {
            if (!TryGetHeaderInt("empresaId", out var empresaId, out var errorResult))
            {
                return errorResult!;
            }

            var eliminado = await _servicioEvidencia.EliminarEvidenciaAsync(empresaId, evidenciaId);

            if (!eliminado)
            {
                return NotFound("Evidencia no encontrada");
            }

            return Ok(new { mensaje = "Evidencia eliminada correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al eliminar la evidencia", detalle = ex.Message });
        }
    }

    private bool TryGetHeaderInt(string headerName, out int value, out IActionResult? errorResult)
    {
        var headerValue = Request.Headers[headerName].FirstOrDefault();
        if (!int.TryParse(headerValue, out value))
        {
            errorResult = BadRequest($"Header '{headerName}' inválido.");
            return false;
        }

        errorResult = null;
        return true;
    }
}
