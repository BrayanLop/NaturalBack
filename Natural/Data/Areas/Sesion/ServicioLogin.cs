using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Natural.Common;
using Natural.Data.Domain;
using Natural.Data.Domain.Sesion;

namespace Natural.Data.Areas.Sesion
{
    /// <summary>
    /// El cliente envía la contraseña ya transformada a SHA-256 en base64 (44 caracteres).
    /// Formatos en BD: el antiguo guarda ese SHA-256 tal cual; el nuevo, un hash PBKDF2 (ASP.NET Identity) de él.
    /// Se aceptan ambos. Con Seguridad:MigrarHashContrasenas = true, el antiguo se convierte en el primer login exitoso.
    /// </summary>
    public class ServicioLogin
    {
        private const int LongitudHashLegado = 44;

        private readonly NaturalDbContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly OpcionesSeguridad _opciones;

        public ServicioLogin(NaturalDbContext context, IPasswordHasher<Usuario> passwordHasher, IOptions<OpcionesSeguridad> opciones)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _opciones = opciones.Value;
        }

        public async Task<Usuario?> ObtenerUsuarioAsync(string nombreUsuario, string contrasena)
        {
            // Puede haber varios registros con el mismo nombre de usuario (la versión anterior buscaba
            // por usuario Y clave a la vez): se prueba cada uno, como hacía ella.
            var candidatos = await _context.Usuario
                .Where(u => u.NombreUsuario == nombreUsuario)
                .OrderBy(u => u.Id)
                .ToListAsync();

            foreach (var usuario in candidatos)
            {
                var claveHash = usuario.ClaveHash ?? string.Empty;

                // Formato antiguo (o cualquier formato no PBKDF2): comparación exacta, igual que antes.
                if (SonIguales(claveHash, contrasena))
                {
                    if (_opciones.MigrarHashContrasenas && EsHashLegado(claveHash))
                    {
                        usuario.ClaveHash = _passwordHasher.HashPassword(usuario, contrasena);
                        await _context.SaveChangesAsync();
                    }

                    return usuario;
                }

                if (EsHashLegado(claveHash))
                    continue;

                var resultado = VerificarHashNuevo(usuario, claveHash, contrasena);
                if (resultado == PasswordVerificationResult.Failed)
                    continue;

                if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    usuario.ClaveHash = _passwordHasher.HashPassword(usuario, contrasena);
                    await _context.SaveChangesAsync();
                }

                return usuario;
            }

            return null;
        }

        /// <summary>Verifica un hash PBKDF2; si el valor guardado no tiene ese formato, se considera fallido.</summary>
        private PasswordVerificationResult VerificarHashNuevo(Usuario usuario, string claveHash, string contrasena)
        {
            try
            {
                return _passwordHasher.VerifyHashedPassword(usuario, claveHash, contrasena);
            }
            catch (FormatException)
            {
                return PasswordVerificationResult.Failed;
            }
        }

        private static bool EsHashLegado(string claveHash) => claveHash.Length == LongitudHashLegado;

        private static bool SonIguales(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }
}
