using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CfSaisie.Data.Migrations
{
    /// <inheritdoc />
    public partial class Langues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Langues",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Langues", x => x.Code);
                });

            migrationBuilder.InsertData(
                table: "Langues",
                columns: new[] { "Code", "Actif", "Libelle", "Ordre" },
                values: new object[,]
                {
                    { "de", true, "Deutsch", 3 },
                    { "en", true, "English", 2 },
                    { "fr", true, "Français", 1 },
                    { "nl", true, "Nederlands", 4 },
                    { "pt", false, "Português", 5 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Langues");
        }
    }
}
