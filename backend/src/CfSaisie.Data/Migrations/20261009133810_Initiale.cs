using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CfSaisie.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initiale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommentaireFamilles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentaireFamilles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Configurateurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductionSite = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurateurs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Familles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Image = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Familles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Audience = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Titre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Corps = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "CommentaireModeles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FamilleId = table.Column<int>(type: "int", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Texte = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentaireModeles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentaireModeles_CommentaireFamilles_FamilleId",
                        column: x => x.FamilleId,
                        principalTable: "CommentaireFamilles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Produits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Image = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LienConfigurateur = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfigurateurId = table.Column<int>(type: "int", nullable: false),
                    FamilleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Produits_Configurateurs_ConfigurateurId",
                        column: x => x.ConfigurateurId,
                        principalTable: "Configurateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Produits_Familles_FamilleId",
                        column: x => x.FamilleId,
                        principalTable: "Familles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Accreditations",
                columns: table => new
                {
                    CodeClient = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProduitId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accreditations", x => new { x.CodeClient, x.ProduitId });
                    table.ForeignKey(
                        name: "FK_Accreditations_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dossiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Origine = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CodeClient = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NomClient = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProduitId = table.Column<int>(type: "int", nullable: true),
                    NomProduit = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NomFamille = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreePar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceChantier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TicketSalesforce = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfigId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Environnement = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ConfigurationEnregistree = table.Column<bool>(type: "bit", nullable: false),
                    DateFinProduction = table.Column<DateOnly>(type: "date", nullable: true),
                    CommentaireExterne = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommentaireInterne = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModeLivraison = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LivraisonCodeClient = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LivraisonNomClient = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LivraisonLibelleAdresse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LivraisonNom = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LivraisonTelephone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LivraisonAdresse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LivraisonCodePostal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LivraisonVille = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Acces = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccesAutre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgenceRetrait = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumeroSap = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DevisSourceId = table.Column<int>(type: "int", nullable: true),
                    CommandeIssueReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceCommandeClient = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCommande = table.Column<DateOnly>(type: "date", nullable: true),
                    DateLivraisonSouhaitee = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dossiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dossiers_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accreditations_ProduitId",
                table: "Accreditations",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentaireModeles_FamilleId",
                table: "CommentaireModeles",
                column: "FamilleId");

            migrationBuilder.CreateIndex(
                name: "IX_Configurateurs_Nom",
                table: "Configurateurs",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dossiers_ProduitId",
                table: "Dossiers",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_Dossiers_Reference",
                table: "Dossiers",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Familles_Nom",
                table: "Familles",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produits_ConfigurateurId",
                table: "Produits",
                column: "ConfigurateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Produits_FamilleId",
                table: "Produits",
                column: "FamilleId");

            migrationBuilder.CreateIndex(
                name: "IX_Produits_Nom",
                table: "Produits",
                column: "Nom",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accreditations");

            migrationBuilder.DropTable(
                name: "CommentaireModeles");

            migrationBuilder.DropTable(
                name: "Dossiers");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Utilisateurs");

            migrationBuilder.DropTable(
                name: "CommentaireFamilles");

            migrationBuilder.DropTable(
                name: "Produits");

            migrationBuilder.DropTable(
                name: "Configurateurs");

            migrationBuilder.DropTable(
                name: "Familles");
        }
    }
}
