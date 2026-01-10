public sealed class ActualizarContrasenaRequest
{
    public int UsuarioId { get; init; }
    public string NuevaContrasena { get; init; } = string.Empty;
}