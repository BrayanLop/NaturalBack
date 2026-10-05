using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Natural.Migrations
{
    /// <summary>
    /// Liquidación con pagos por forma de pago.
    /// Cambios SOLO aditivos (la versión anterior del API sigue usando la misma BD):
    ///  - Columna nullable EgresosEmpresa.FormaPago.
    ///  - Tablas nuevas Liquidacion y LiquidacionPago con sus índices.
    /// El snapshot del modelo estaba desfasado respecto a la BD real (faltaban tablas/columnas que ya existen);
    /// la migración generada incluía esos cambios y se eliminaron a mano: aquí solo queda lo nuevo.
    /// </summary>
    public partial class LiquidacionPagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<char>(
                name: "FormaPago",
                table: "EgresosEmpresa",
                type: "character(1)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Liquidacion",
                columns: table => new
                {
                    IdLiquidacion = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    PersonaId = table.Column<int>(type: "integer", nullable: false),
                    FechaLiquidacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CantidadServicios = table.Column<int>(type: "integer", nullable: false),
                    TotalFacturado = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalComision = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalDeducciones = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetoPagado = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Liquidacion", x => x.IdLiquidacion);
                    table.ForeignKey(
                        name: "FK_Liquidacion_Empresa",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Liquidacion_Persona",
                        column: x => x.PersonaId,
                        principalTable: "Persona",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LiquidacionPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdLiquidacion = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FormaPago = table.Column<char>(type: "character(1)", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiquidacionPago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiquidacionPago_Liquidacion",
                        column: x => x.IdLiquidacion,
                        principalTable: "Liquidacion",
                        principalColumn: "IdLiquidacion",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Liquidacion_Empresa_Fecha",
                table: "Liquidacion",
                columns: new[] { "EmpresaId", "FechaLiquidacion" });

            migrationBuilder.CreateIndex(
                name: "IX_Liquidacion_Empresa_Persona",
                table: "Liquidacion",
                columns: new[] { "EmpresaId", "PersonaId" });

            migrationBuilder.CreateIndex(
                name: "IX_Liquidacion_PersonaId",
                table: "Liquidacion",
                column: "PersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_LiquidacionPago_IdLiquidacion",
                table: "LiquidacionPago",
                column: "IdLiquidacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiquidacionPago");

            migrationBuilder.DropTable(
                name: "Liquidacion");

            migrationBuilder.DropColumn(
                name: "FormaPago",
                table: "EgresosEmpresa");
        }
    }
}
