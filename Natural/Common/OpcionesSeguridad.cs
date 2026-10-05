namespace Natural.Common
{
    /// <summary>Sección "Seguridad" de la configuración.</summary>
    public class OpcionesSeguridad
    {
        /// <summary>
        /// Si es true, las contraseñas se guardan con PBKDF2 y las antiguas (SHA-256) se convierten al iniciar sesión.
        /// Mientras alguna versión anterior del API use la misma base de datos debe quedar en false:
        /// esas versiones solo entienden el formato SHA-256.
        /// </summary>
        public bool MigrarHashContrasenas { get; set; }
    }
}
