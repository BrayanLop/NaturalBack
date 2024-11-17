using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Natural.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Servicios",
                table: "Servicio");

            migrationBuilder.RenameTable(
                name: "Servicio",
                newName: "Servicio");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Servicio",
                table: "Servicio",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Persona",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cedula = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Residencia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Edad = table.Column<int>(type: "int", nullable: false),
                    Celular = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persona", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Persona");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Servicio",
                table: "Servicio");

            migrationBuilder.RenameTable(
                name: "Servicio",
                newName: "Servicio");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Servicios",
                table: "Servicios",
                column: "Id");
        }
    }
}
