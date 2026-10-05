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
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
            if (usuario is null)
                return null;

            if (EsHashLegado(usuario.ClaveHash))
            {
                if (!SonIguales(usuario.ClaveHash, contrasena))
                    return null;

                if (_opciones.MigrarHashContrasenas)
                {
                    usuario.ClaveHash = _passwordHasher.HashPassword(usuario, contrasena);
                    await _context.SaveChangesAsync();
                }

                return usuario;
            }

            var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.ClaveHash, contrasena);
            if (resultado == PasswordVerificationResult.Failed)
                return null;

            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.ClaveHash = _passwordHasher.HashPassword(usuario, contrasena);
                await _context.SaveChangesAsync();
            }

            return usuario;
        }

        private static bool EsHashLegado(string claveHash) => claveHash.Length == LongitudHashLegado;

        private static bool SonIguales(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }
}
