using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain;
using Natural.Data.Domain.Sesion;

namespace Natural.Data.Areas.Sesion
{
    public class ServicioLogin
    {
        private readonly NaturalDbContext _context;

        public ServicioLogin(NaturalDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario> ObtenerUsuarioAsync(string usuario, string contrasenia)
        {
            return await _context.Usuario
                .FirstOrDefaultAsync(r => r.NombreUsuario == usuario && r.ClaveHash == contrasenia);                
        }
    }
}
