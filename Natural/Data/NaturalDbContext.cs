using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain;

namespace Natural.Data
{
    public class NaturalDbContext : DbContext
    {
        public NaturalDbContext(DbContextOptions<NaturalDbContext> options) : base(options) { }

        public DbSet<Servicio> Servicio { get; set; }
    }
}
