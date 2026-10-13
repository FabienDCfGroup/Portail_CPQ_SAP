using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CfSaisie.Data.Migrations
{
    /// <inheritdoc />
    public partial class TablesErp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "erp");

            migrationBuilder.CreateTable(
                name: "AgencesRetrait",
                schema: "erp",
                columns: table => new
                {
                    CodeAgence = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Localisation = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Actif = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DateMaj = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencesRetrait", x => x.CodeAgence);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                schema: "erp",
                columns: table => new
                {
                    CodeClient = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RaisonSociale = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Pays = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Langue = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    ModePaiement = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstVic = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DateMaj = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.CodeClient);
                });

            migrationBuilder.CreateTable(
                name: "ModesLivraison",
                schema: "erp",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DateMaj = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModesLivraison", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "AdressesClients",
                schema: "erp",
                columns: table => new
                {
                    IdAdresse = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodeClient = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EstPrincipale = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    NomDestinataire = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Telephone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Adresse = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ComplementAdresse = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CodePostal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ville = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Pays = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateMaj = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdressesClients", x => x.IdAdresse);
                    table.ForeignKey(
                        name: "FK_AdressesClients_Clients_CodeClient",
                        column: x => x.CodeClient,
                        principalSchema: "erp",
                        principalTable: "Clients",
                        principalColumn: "CodeClient",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Commandes",
                schema: "erp",
                columns: table => new
                {
                    NumeroCommande = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CodeClient = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReferenceCommandeClient = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReferencePortail = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ConfigId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatutErp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateDebutProduction = table.Column<DateOnly>(type: "date", nullable: true),
                    DateFinProduction = table.Column<DateOnly>(type: "date", nullable: true),
                    DateLivraisonPrevue = table.Column<DateOnly>(type: "date", nullable: true),
                    DateLivraisonReelle = table.Column<DateOnly>(type: "date", nullable: true),
                    DateMaj = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commandes", x => x.NumeroCommande);
                    table.ForeignKey(
                        name: "FK_Commandes_Clients_CodeClient",
                        column: x => x.CodeClient,
                        principalSchema: "erp",
                        principalTable: "Clients",
                        principalColumn: "CodeClient",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdressesClients_CodeClient",
                schema: "erp",
                table: "AdressesClients",
                column: "CodeClient");

            migrationBuilder.CreateIndex(
                name: "UX_AdressesClients_AdressePrincipale",
                schema: "erp",
                table: "AdressesClients",
                column: "CodeClient",
                unique: true,
                filter: "[EstPrincipale] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_RaisonSociale",
                schema: "erp",
                table: "Clients",
                column: "RaisonSociale");

            migrationBuilder.CreateIndex(
                name: "IX_Commandes_CodeClient",
                schema: "erp",
                table: "Commandes",
                column: "CodeClient");

            migrationBuilder.CreateIndex(
                name: "IX_Commandes_ConfigId",
                schema: "erp",
                table: "Commandes",
                column: "ConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Commandes_DateCreation",
                schema: "erp",
                table: "Commandes",
                column: "DateCreation");

            migrationBuilder.CreateIndex(
                name: "IX_Commandes_ReferencePortail",
                schema: "erp",
                table: "Commandes",
                column: "ReferencePortail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdressesClients",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "AgencesRetrait",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "Commandes",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "ModesLivraison",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "Clients",
                schema: "erp");
        }
    }
}
