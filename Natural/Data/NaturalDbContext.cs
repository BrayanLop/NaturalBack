using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain.Configuracion;
using Natural.Data.Domain.Estructura;
using Natural.Data.Domain.Gestion;
using Natural.Data.Domain.Sesion;

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
    public virtual DbSet<Usuario> Usuario { get; set; }
    public virtual DbSet<ConfiguracionServicio> ConfiguracionServicio { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
#warning Para proteger tu cadena de conexión, muévela a appsettings.json o variables de entorno.
        if (!optionsBuilder.IsConfigured)
        {
             optionsBuilder.UseNpgsql("User Id=postgres.rsnemoylcmbfpsirflly;Password=Losdelsur99*;Server=aws-0-us-east-1.pooler.supabase.com;Port=5432;Database=postgres;SSL Mode=Require;Trust Server Certificate=true"); 
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Empresa");

            entity.ToTable("Empresa");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Direccion).HasMaxLength(200);
            entity.Property(e => e.Nit).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Telefono).HasMaxLength(20);
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
            entity.ToTable("PersonaRol");

            entity.HasKey(e => new { e.PersonaId, e.RolCodigo });

            entity.Property(e => e.RolCodigo).HasMaxLength(10);

            entity.HasOne(d => d.Persona)
                .WithMany()
                .HasForeignKey(d => d.PersonaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PersonaRol_Persona");

            entity.HasOne(d => d.RolCodigoNavigation)
                .WithMany()
                .HasForeignKey(d => d.RolCodigo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PersonaRol_Rol");
        });

        modelBuilder.Entity<RegistroServicio>(entity =>
        {
            entity.ToTable("RegistroServicio");

            entity.HasIndex(e => e.PersonaId, "IX_RegistroServicio_PersonaId");
            entity.HasIndex(e => e.ServicioId, "IX_RegistroServicio_ServicioId");

            entity.HasOne(d => d.Empresa).WithMany(p => p.RegistroServicios)
                .HasForeignKey(d => d.EmpresaId)
                .HasConstraintName("FK_RegistroServicio_Empresa");

            entity.HasOne(d => d.Persona).WithMany(p => p.RegistroServicios)
                .HasForeignKey(d => d.PersonaId);

            entity.HasOne(d => d.Servicio).WithMany(p => p.RegistroServicios)
                .HasForeignKey(d => d.ServicioId);
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.Codigo);
            entity.ToTable("Rol");

            entity.Property(e => e.Codigo).HasMaxLength(10);
            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.ToTable("Servicio");

            entity.Property(e => e.Precio).HasColumnType("numeric(18, 2)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Servicios)
                .HasForeignKey(d => d.EmpresaId)
                .HasConstraintName("FK_Servicio_Empresa");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuario");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.NombreUsuario)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.ClaveHash)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Estado).IsRequired();

            entity.HasOne(e => e.Persona)
                .WithMany(p => p.Usuario)
                .HasForeignKey(e => e.PersonaId)
                .HasConstraintName("FK_Usuario_Persona");
        });

        modelBuilder.Entity<ConfiguracionServicio>(entity =>
        {
            entity.ToTable("ConfiguracionServicio");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.PorcentajeTrabajador)
                .HasColumnType("numeric(5, 2)")
                .IsRequired();

            entity.Property(e => e.PorcentajeEmpresa)
                .HasColumnType("numeric(5, 2)")
                .IsRequired();

            //entity.Property(e => e.FechaCreacion)
            //    .HasColumnType("timestamp")
            //    .HasDefaultValueSql("NOW()");

            entity.Property(e => e.Estado)
                .HasDefaultValue(true);

            entity.HasOne(e => e.Empresa)
                .WithMany(e => e.ConfiguracionServicios)
                .HasForeignKey(e => e.EmpresaId)
                .HasConstraintName("FK_ConfiguracionServicio_Empresa");

            entity.HasOne(e => e.Servicio)
                .WithMany(s => s.ConfiguracionServicios)
                .HasForeignKey(e => e.ServicioId)
                .HasConstraintName("FK_ConfiguracionServicio_Servicio");

            entity.HasIndex(e => new { e.EmpresaId, e.ServicioId })
                .IsUnique()
                .HasDatabaseName("UX_ConfiguracionServicio_Empresa_Servicio");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
