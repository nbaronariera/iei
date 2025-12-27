using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UI.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Provincia",
                columns: table => new
                {
                    codigo = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provincia", x => x.codigo);
                });

            migrationBuilder.CreateTable(
                name: "Localidad",
                columns: table => new
                {
                    codigo = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", nullable: false),
                    codigoProvincia = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Localidad", x => x.codigo);
                    table.ForeignKey(
                        name: "FK_Localidad_Provincia_codigoProvincia",
                        column: x => x.codigoProvincia,
                        principalTable: "Provincia",
                        principalColumn: "codigo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Estacion",
                columns: table => new
                {
                    cod_estacion = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", nullable: false),
                    tipo = table.Column<string>(type: "TEXT", nullable: false),
                    direccion = table.Column<string>(type: "TEXT", nullable: false),
                    codigoPostal = table.Column<int>(type: "INTEGER", nullable: false),
                    longitud = table.Column<double>(type: "REAL", nullable: false),
                    latitud = table.Column<double>(type: "REAL", nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", nullable: false),
                    horario = table.Column<string>(type: "TEXT", nullable: false),
                    contacto = table.Column<string>(type: "TEXT", nullable: false),
                    URL = table.Column<string>(type: "TEXT", nullable: false),
                    codigoLocalidad = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estacion", x => x.cod_estacion);
                    table.ForeignKey(
                        name: "FK_Estacion_Localidad_codigoLocalidad",
                        column: x => x.codigoLocalidad,
                        principalTable: "Localidad",
                        principalColumn: "codigo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Estacion_codigoLocalidad",
                table: "Estacion",
                column: "codigoLocalidad");

            migrationBuilder.CreateIndex(
                name: "IX_Localidad_codigoProvincia",
                table: "Localidad",
                column: "codigoProvincia");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Estacion");

            migrationBuilder.DropTable(
                name: "Localidad");

            migrationBuilder.DropTable(
                name: "Provincia");
        }
    }
}
