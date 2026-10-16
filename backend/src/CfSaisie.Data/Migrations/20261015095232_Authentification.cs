using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CfSaisie.Data.Migrations
{
    /// <inheritdoc />
    public partial class Authentification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Utilisateurs");

            migrationBuilder.CreateTable(
                name: "JournalConnexions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Resultat = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ObjetId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Compte = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Nom = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdresseIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Navigateur = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalConnexions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalConnexions_Compte",
                table: "JournalConnexions",
                column: "Compte");

            migrationBuilder.CreateIndex(
                name: "IX_JournalConnexions_Date",
                table: "JournalConnexions",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JournalConnexions");

            migrationBuilder.CreateTable(
                name: "Utilisateurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.Id);
                });
        }
    }
}
