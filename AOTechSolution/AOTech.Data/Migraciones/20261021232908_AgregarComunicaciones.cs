using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AOTech.Data.Migraciones
{
    public partial class AgregarComunicaciones : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "Comunicaciones");

            migrationBuilder.CreateTable(
                name: "Avisos",
                schema: "Comunicaciones",
                columns: table => new
                {
                    AvisoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioCreadorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Borrador"),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avisos", x => x.AvisoId);
                    table.CheckConstraint("CK_Avisos_Estado", "[Estado] IN (N'Borrador', N'Enviado')");
                    table.ForeignKey(
                        name: "FK_Avisos_Usuarios_Creador",
                        column: x => x.UsuarioCreadorId,
                        principalSchema: "Seguridad",
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId");
                });

            migrationBuilder.CreateTable(
                name: "AvisoDestinatarios",
                schema: "Comunicaciones",
                columns: table => new
                {
                    AvisoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaLectura = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvisoDestinatarios", x => new { x.AvisoId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_AvisoDestinatarios_Avisos",
                        column: x => x.AvisoId,
                        principalSchema: "Comunicaciones",
                        principalTable: "Avisos",
                        principalColumn: "AvisoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvisoDestinatarios_Usuarios",
                        column: x => x.UsuarioId,
                        principalSchema: "Seguridad",
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Avisos_UsuarioCreadorId",
                schema: "Comunicaciones",
                table: "Avisos",
                column: "UsuarioCreadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Avisos_Estado_FechaCreacion",
                schema: "Comunicaciones",
                table: "Avisos",
                columns: new[] { "Estado", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_AvisoDestinatarios_UsuarioId_FechaLectura",
                schema: "Comunicaciones",
                table: "AvisoDestinatarios",
                columns: new[] { "UsuarioId", "FechaLectura" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvisoDestinatarios",
                schema: "Comunicaciones");

            migrationBuilder.DropTable(
                name: "Avisos",
                schema: "Comunicaciones");

            migrationBuilder.Sql(
                "IF SCHEMA_ID(N'Comunicaciones') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.objects WHERE schema_id = SCHEMA_ID(N'Comunicaciones')) EXEC(N'DROP SCHEMA [Comunicaciones]');");
        }
    }
}
