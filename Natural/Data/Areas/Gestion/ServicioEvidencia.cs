using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain;
using Natural.Data.Domain.Gestion;
using Natural.Data.Services;
using Natural.Models.Areas.Gestion;

namespace Natural.Data.Areas.Gestion;

public class ServicioEvidencia
{
    private readonly NaturalDbContext _context;
    private readonly SupabaseStorageService _storageService;

    public ServicioEvidencia(NaturalDbContext context, SupabaseStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    public async Task<EvidenciaRegistroServicioViewModel> SubirEvidenciaAsync(int empresaId, int registroServicioId, IFormFile archivo)
    {
        // Verificar que el registro existe y pertenece a la empresa
        var registroExiste = await _context.RegistroServicio
            .AnyAsync(r => r.Id == registroServicioId && r.EmpresaId == empresaId);

        if (!registroExiste)
        {
            throw new Exception("El registro de servicio no existe o no pertenece a esta empresa");
        }

        // Subir archivo a Supabase
        var urlEvidencia = await _storageService.SubirArchivoAsync(archivo, registroServicioId, empresaId);

        // Guardar referencia en la base de datos
        var evidencia = new EvidenciaRegistroServicio
        {
            RegistroServicioId = registroServicioId,
            UrlEvidencia = urlEvidencia,
            NombreArchivo = archivo.FileName,
            FechaSubida = DateTime.UtcNow,
            EmpresaId = empresaId
        };

        _context.EvidenciaRegistroServicios.Add(evidencia);
        await _context.SaveChangesAsync();

        return new EvidenciaRegistroServicioViewModel
        {
            Id = evidencia.Id,
            RegistroServicioId = evidencia.RegistroServicioId,
            UrlEvidencia = evidencia.UrlEvidencia,
            NombreArchivo = evidencia.NombreArchivo,
            FechaSubida = evidencia.FechaSubida
        };
    }

    public async Task<List<EvidenciaRegistroServicioViewModel>> ObtenerEvidenciasPorRegistroAsync(int empresaId, int registroServicioId)
    {
        return await _context.EvidenciaRegistroServicios
            .Where(e => e.RegistroServicioId == registroServicioId && e.EmpresaId == empresaId)
            .OrderByDescending(e => e.FechaSubida)
            .Select(e => new EvidenciaRegistroServicioViewModel
            {
                Id = e.Id,
                RegistroServicioId = e.RegistroServicioId,
                UrlEvidencia = e.UrlEvidencia,
                NombreArchivo = e.NombreArchivo,
                FechaSubida = e.FechaSubida
            })
            .ToListAsync();
    }

    public async Task<bool> EliminarEvidenciaAsync(int empresaId, int evidenciaId)
    {
        var evidencia = await _context.EvidenciaRegistroServicios
            .FirstOrDefaultAsync(e => e.Id == evidenciaId && e.EmpresaId == empresaId);

        if (evidencia == null)
        {
            return false;
        }

        // Eliminar de Supabase
        await _storageService.EliminarArchivoAsync(evidencia.UrlEvidencia);

        // Eliminar de la base de datos
        _context.EvidenciaRegistroServicios.Remove(evidencia);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<Dictionary<int, List<EvidenciaRegistroServicioViewModel>>> ObtenerEvidenciasPorRegistrosAsync(int empresaId, List<int> registroServicioIds)
    {
        var evidencias = await _context.EvidenciaRegistroServicios
            .Where(e => e.EmpresaId == empresaId && registroServicioIds.Contains(e.RegistroServicioId))
            .OrderByDescending(e => e.FechaSubida)
            .ToListAsync();

        return evidencias
            .GroupBy(e => e.RegistroServicioId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => new EvidenciaRegistroServicioViewModel
                {
                    Id = e.Id,
                    RegistroServicioId = e.RegistroServicioId,
                    UrlEvidencia = e.UrlEvidencia,
                    NombreArchivo = e.NombreArchivo,
                    FechaSubida = e.FechaSubida
                }).ToList()
            );
    }
}
