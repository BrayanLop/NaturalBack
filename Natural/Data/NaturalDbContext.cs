using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain.Estructura;
using Natural.Data.Domain.Gestion;

namespace Natural.Data.Domain;

public partial class NaturalDbContext : DbContext
{
    public NaturalDbContext()
    {
    }

    public NaturalDbContext(DbContextOptions<NaturalDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Empresa> Empresa { get; set; }

    public virtual DbSet<Persona> Persona { get; set; }

    public virtual DbSet<PersonaRol> PersonaRol { get; set; }

    public virtual DbSet<RegistroServicio> RegistroServicio { get; set; }

    public virtual DbSet<Rol> Rol { get; set; }

    public virtual DbSet<Servicio> Servicio { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=NaturalDb;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Empresa__3214EC07EB48E8ED");

            entity.ToTable("Empresa");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Direccion)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Nit)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Persona>(entity =>
        {
            entity.ToTable("Persona");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Personas)
                .HasForeignKey(d => d.EmpresaId)
                .HasConstraintName("FK_Persona_Empresa");
        });

        modelBuilder.Entity<PersonaRol>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PersonaRol");

            entity.Property(e => e.RolCodigo)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Persona).WithMany()
                .HasForeignKey(d => d.PersonaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PersonaRol_Persona");

            entity.HasOne(d => d.RolCodigoNavigation).WithMany()
                .HasForeignKey(d => d.RolCodigo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PersonaRol_Rol");
        });

        modelBuilder.Entity<RegistroServicio>(entity =>
        {
            entity.ToTable("RegistroServicio");

            entity.HasIndex(e => e.PersonaId, "IX_RegistroServicio_PersonaId");

            entity.HasIndex(e => e.ServicioId, "IX_RegistroServicio_ServicioId");

            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.RegistroServicios)
                .HasForeignKey(d => d.EmpresaId)
                .HasConstraintName("FK_RegistroServicio_Empresa");

            entity.HasOne(d => d.Persona).WithMany(p => p.RegistroServicios).HasForeignKey(d => d.PersonaId);

            entity.HasOne(d => d.Servicio).WithMany(p => p.RegistroServicios).HasForeignKey(d => d.ServicioId);
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.Codigo);

            entity.ToTable("Rol");

            entity.Property(e => e.Codigo)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.ToTable("Servicio");

            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Servicios)
                .HasForeignKey(d => d.EmpresaId)
                .HasConstraintName("FK_Servicio_Empresa");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
