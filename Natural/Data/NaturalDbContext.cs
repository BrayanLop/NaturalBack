using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain.Configuracion;
using Natural.Data.Domain.Estructura;
using Natural.Data.Domain.Gestion;
using Natural.Data.Domain.Sesion;
using Natural.Models;

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

    public virtual DbSet<LiquidacionEgreso> LiquidacionEgresos { get; set; }
    public virtual DbSet<EgresosEmpresa> EgresosEmpresa { get; set; }
    public virtual DbSet<IngresosEmpresa> IngresosEmpresas { get; set; }
    public virtual DbSet<ConfiguracionGeneral> ConfiguracionGeneral { get; set; }
    public virtual DbSet<Empresa> Empresa { get; set; }
    public virtual DbSet<Persona> Persona { get; set; }
    public virtual DbSet<PersonaRol> PersonaRol { get; set; }
    public virtual DbSet<UsuarioRol> UsuarioRol { get; set; }
    public virtual DbSet<RegistroServicio> RegistroServicio { get; set; }
    public virtual DbSet<Rol> Rol { get; set; }
    public virtual DbSet<Servicio> Servicio { get; set; }
    public virtual DbSet<Usuario> Usuario { get; set; }
    public virtual DbSet<ConfiguracionServicio> ConfiguracionServicio { get; set; }
    public virtual DbSet<EvidenciaRegistroServicio> EvidenciaRegistroServicios { get; set; }

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
        modelBuilder.Entity<ConfiguracionGeneral>(entity =>
        {
            entity.ToTable("ConfiguracionGeneral");

            entity.HasKey(e => new { e.EmpresaId, e.Periodicidad });

            entity.Property(e => e.EmpresaId)
                  .IsRequired();

            entity.Property(e => e.Periodicidad)
                  .HasColumnType("character varying")
                  .IsRequired();

            entity.Property(e => e.ValorPeriodicidad)
                  .IsRequired();

            entity.HasOne(e => e.Empresa)
                  .WithMany(e => e.ConfiguracionGeneral)
                  .HasForeignKey(e => e.EmpresaId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("ConfiguracionGeneral_EmpresaId_fkey");
        });

        modelBuilder.Entity<LiquidacionEgreso>(entity =>
        {
            entity.ToTable("LiquidacionEgreso");

            // ✅ Clave primaria simple
            entity.HasKey(e => e.IdLiquidacionEgreso);

            entity.Property(e => e.IdLiquidacionEgreso)
                  .ValueGeneratedOnAdd()
               .UseIdentityColumn();

            entity.Property(e => e.IdLiquidacion)
            .HasMaxLength(36)
                  .IsRequired();

            entity.Property(e => e.IdEgreso)
                  .IsRequired();

            entity.Property(e => e.EmpresaId)
             .IsRequired();

            // ✅ Índice único compuesto para evitar duplicados
            entity.HasIndex(e => new { e.IdLiquidacion, e.IdEgreso })
              .IsUnique()
                .HasDatabaseName("UX_LiquidacionEgreso_Liquidacion_Egreso");

          entity.HasOne(e => e.Empresa)
           .WithMany()
                  .HasForeignKey(e => e.EmpresaId)
                  .OnDelete(DeleteBehavior.Restrict);

          entity.HasOne(e => e.EgresoEmpresa)
          .WithMany()
              .HasForeignKey(e => e.IdEgreso)
                  .OnDelete(DeleteBehavior.Restrict)
    .HasConstraintName("LiquidacionEgreso_IdEgreso_fkey");
        });
        
        modelBuilder.Entity<EgresosEmpresa>(entity =>
        {
            entity.HasKey(e => e.EgresoId).HasName("EgresosEmpresa_pkey");

            entity.ToTable("EgresosEmpresa");

            entity.HasOne(d => d.Empresa).WithMany(p => p.EgresosEmpresas)
                .HasForeignKey(d => d.EmpresaId)
                .HasConstraintName("EgresosEmpresa_EmpresaId_fkey");
        });


        modelBuilder.Entity<IngresosEmpresa>(entity =>
        {
            entity.HasKey(e => e.IngresoId).HasName("IngresosEmpresa_pkey");

            entity.ToTable("IngresosEmpresa");

            entity.HasOne(d => d.Empresa).WithMany(p => p.IngresosEmpresas)
                .HasForeignKey(d => d.EmpresaId)
                .HasConstraintName("IngresosEmpresa_EmpresaId_fkey");
        });

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

        modelBuilder.Entity<UsuarioRol>(entity =>
        {
            entity.ToTable("UsuarioRol");

            entity.HasKey(e => new { e.UsuarioId, e.RolCodigo });

            entity.Property(e => e.RolCodigo).HasMaxLength(10);

            entity.HasOne(d => d.Persona)
                .WithMany()
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsuarioRol_Usuario");

            entity.HasOne(d => d.RolCodigoNavigation)
                .WithMany()
                .HasForeignKey(d => d.RolCodigo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsuarioRol_Rol");
        });

        modelBuilder.Entity<RegistroServicio>(entity =>
        {
            entity.ToTable("RegistroServicio");

            entity.HasIndex(e => e.PersonaId, "IX_RegistroServicio_PersonaId");
            entity.HasIndex(e => e.ServicioId, "IX_RegistroServicio_ServicioId");

            entity.Property(e => e.Propina).HasColumnType("numeric(18, 2)").HasDefaultValue(0m);
            entity.Property(e => e.ValorServicio).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Comision).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.PorcentajeTrabajador).HasColumnType("numeric(5, 2)");
            entity.Property(e => e.MotivoRechazo).HasMaxLength(256);
            entity.Property(e => e.Observaciones).HasMaxLength(256);
            entity.Property(e => e.Rechazado).HasDefaultValue(false);
            entity.Property(e => e.PropinaPagada).HasDefaultValue(false);

            entity.HasOne(d => d.Empresa).WithMany(p => p.RegistroServicios)
                .HasForeignKey(d => d.EmpresaId)
                .HasConstraintName("FK_RegistroServicio_Empresa");

            entity.HasOne(d => d.Persona).WithMany(p => p.RegistroServicios)
                .HasForeignKey(d => d.PersonaId);

            entity.HasOne(d => d.Servicio).WithMany(p => p.RegistroServicios)
                .HasForeignKey(d => d.ServicioId);
            
            entity.HasMany(d => d.Evidencias)
                .WithOne(e => e.RegistroServicio)
                .HasForeignKey(e => e.RegistroServicioId)
                .OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<EvidenciaRegistroServicio>(entity =>
        {
            entity.ToTable("EvidenciaRegistroServicio");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.UrlEvidencia)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.NombreArchivo)
                .HasMaxLength(255);

            entity.Property(e => e.FechaSubida)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.EmpresaId);

            entity.Property(e => e.RegistroServicioId)
                .IsRequired();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
