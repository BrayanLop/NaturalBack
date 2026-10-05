using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain;
using Natural.Data.Domain.Sesion;

namespace Natural.Data.Areas.Sesion
{
    /// <summary>
    /// El cliente envía la contraseña ya transformada a SHA-256 en base64 (44 caracteres).
    /// En BD se guarda un hash PBKDF2 (ASP.NET Identity) de ese valor. Los registros antiguos,
    /// que guardaban directamente el SHA-256, se migran al nuevo formato en el primer login exitoso.
    /// </summary>
    public class ServicioLogin
    {
        private const int LongitudHashLegado = 44;

        private readonly NaturalDbContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public ServicioLogin(NaturalDbContext context, IPasswordHasher<Usuario> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
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

                usuario.ClaveHash = _passwordHasher.HashPassword(usuario, contrasena);
                await _context.SaveChangesAsync();
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

        public string GenerarHash(Usuario usuario, string contrasena) => _passwordHasher.HashPassword(usuario, contrasena);

        private static bool EsHashLegado(string claveHash) => claveHash.Length == LongitudHashLegado;

        private static bool SonIguales(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }
}
