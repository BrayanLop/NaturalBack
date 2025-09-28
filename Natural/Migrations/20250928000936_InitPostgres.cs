using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Natural.Migrations
{
    /// <inheritdoc />
    public partial class InitPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Empresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Nit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Estado = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Persona",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Apellido = table.Column<string>(type: "text", nullable: false),
                    Cedula = table.Column<string>(type: "text", nullable: false),
                    Residencia = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Edad = table.Column<int>(type: "integer", nullable: false),
                    Celular = table.Column<string>(type: "text", nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persona", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Persona_Empresa",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Servicio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Disponible = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servicio_Empresa",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Rol",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    PersonaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rol", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK_Rol_Persona_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Persona",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreUsuario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClaveHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    PersonaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuario_Persona",
                        column: x => x.PersonaId,
                        principalTable: "Persona",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionServicio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    ServicioId = table.Column<int>(type: "integer", nullable: false),
                    PorcentajeTrabajador = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    PorcentajeEmpresa = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "NOW()"),
                    Estado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionServicio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionServicio_Empresa",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfiguracionServicio_Servicio",
                        column: x => x.ServicioId,
                        principalTable: "Servicio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistroServicio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonaId = table.Column<int>(type: "integer", nullable: false),
                    ServicioId = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<int>(type: "integer", nullable: true),
                    FechaServicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Liquidado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistroServicio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistroServicio_Empresa",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RegistroServicio_Persona_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Persona",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistroServicio_Servicio_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonaRol",
                columns: table => new
                {
                    PersonaId = table.Column<int>(type: "integer", nullable: false),
                    RolCodigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_PersonaRol_Persona",
                        column: x => x.PersonaId,
                        principalTable: "Persona",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonaRol_Rol",
                        column: x => x.RolCodigo,
                        principalTable: "Rol",
                        principalColumn: "Codigo");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionServicio_ServicioId",
                table: "ConfiguracionServicio",
                column: "ServicioId");

            migrationBuilder.CreateIndex(
                name: "UX_ConfiguracionServicio_Empresa_Servicio",
                table: "ConfiguracionServicio",
                columns: new[] { "EmpresaId", "ServicioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Persona_EmpresaId",
                table: "Persona",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonaRol_PersonaId",
                table: "PersonaRol",
                column: "PersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonaRol_RolCodigo",
                table: "PersonaRol",
                column: "RolCodigo");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroServicio_EmpresaId",
                table: "RegistroServicio",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroServicio_PersonaId",
                table: "RegistroServicio",
                column: "PersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroServicio_ServicioId",
                table: "RegistroServicio",
                column: "ServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_Rol_PersonaId",
                table: "Rol",
                column: "PersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicio_EmpresaId",
                table: "Servicio",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_PersonaId",
                table: "Usuario",
                column: "PersonaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionServicio");

            migrationBuilder.DropTable(
                name: "PersonaRol");

            migrationBuilder.DropTable(
                name: "RegistroServicio");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "Rol");

            migrationBuilder.DropTable(
                name: "Servicio");

            migrationBuilder.DropTable(
                name: "Persona");

            migrationBuilder.DropTable(
                name: "Empresa");
        }
    }
}
