using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain;
using Natural.Models.Areas.Estructura;

namespace Natural.Data
{
    public class NaturalDbContext : DbContext
    {
        public NaturalDbContext(DbContextOptions<NaturalDbContext> options) : base(options) { }

        public DbSet<Servicio> Servicio { get; set; }
        public DbSet<Persona> Persona { get; set; }
        public DbSet<Natural.Models.Areas.Estructura.PersonaViewModel> PersonaViewModel { get; set; } = default!;
    }
}
