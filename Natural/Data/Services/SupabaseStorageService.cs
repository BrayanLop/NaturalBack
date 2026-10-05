using Microsoft.AspNetCore.Http;
using Supabase;

namespace Natural.Data.Services;

public class SupabaseStorageService
{
    private readonly Supabase.Client _supabaseClient;
    private const string BucketName = "EvidenciaRegistroServicios";

    public SupabaseStorageService(Supabase.Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
    }

    public async Task<string> SubirArchivoAsync(IFormFile archivo, int registroServicioId, int empresaId)
    {
        if (archivo == null || archivo.Length == 0)
        {
            throw new ArgumentException("El archivo no puede estar vacío");
        }

        // Validar tipo de archivo permitido (im�genes + PDF para evidencias)
        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf" };
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

        if (!extensionesPermitidas.Contains(extension))
        {
            throw new ArgumentException($"Solo se permiten estos tipos de archivo: {string.Join(", ", extensionesPermitidas)}");
        }

        // Validar tama�o (max 5MB)
        if (archivo.Length > 5 * 1024 * 1024)
        {
            throw new ArgumentException("El archivo no puede superar los 5MB");
        }

        // Generar nombre �nico
        var nombreArchivo = $"{empresaId}/{registroServicioId}/{Guid.NewGuid()}{extension}";

        try
        {
            using var stream = archivo.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            // Subir a Supabase Storage
            await _supabaseClient.Storage
                .From(BucketName)
                .Upload(bytes, nombreArchivo, new Supabase.Storage.FileOptions
                {
                    ContentType = archivo.ContentType,
                    Upsert = false
                });

            // Obtener URL p�blica
            var url = _supabaseClient.Storage
                .From(BucketName)
                .GetPublicUrl(nombreArchivo);

            return url;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al subir archivo a Supabase: {ex.Message}", ex);
        }
    }

    public async Task<bool> EliminarArchivoAsync(string url)
    {
        try
        {
            // Extraer el path del archivo de la URL
            var uri = new Uri(url);
            var path = uri.AbsolutePath.Split($"{BucketName}/")[1];

            await _supabaseClient.Storage
                .From(BucketName)
                .Remove(new List<string> { path });

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> ObtenerEvidenciasPorRegistroAsync(int registroServicioId, int empresaId)
    {
        try
        {
            var prefix = $"{empresaId}/{registroServicioId}/";
            
            var archivos = await _supabaseClient.Storage
                .From(BucketName)
                .List(prefix);

            return archivos.Select(f => _supabaseClient.Storage
                .From(BucketName)
                .GetPublicUrl($"{prefix}{f.Name}"))
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }
}
