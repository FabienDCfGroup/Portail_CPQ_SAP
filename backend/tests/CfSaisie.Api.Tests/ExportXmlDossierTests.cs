using System.Xml.Linq;
using CfSaisie.Api.Services;
using CfSaisie.Data;
using CfSaisie.Sap;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CfSaisie.Api.Tests;

public class ExportXmlDossierTests : IDisposable
{
    private readonly string _dossierTemporaire = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"export-xml-{Guid.NewGuid():N}")).FullName;

    public void Dispose() => Directory.Delete(_dossierTemporaire, recursive: true);

    private static Dossier CommandeChezLeClient() => new()
    {
        Id = 1,
        Reference = "CF-2026-1001",
        Type = TypeDossier.Commande,
        Origine = OrigineDossier.CreationInterne,
        Statut = StatutDossier.Envoye,
        CodeClient = "105789",
        NomClient = "Test bascule sap",
        ProduitId = 4,
        Produit = new Produit { Id = 4, Nom = "Contura", Configurateur = new Configurateur { Nom = "Contura", ProductionSite = null } },
        NomProduit = "Contura",
        NomFamille = "Volet hors-sol",
        CreePar = "Fabien Delpierre",
        DateCreation = new DateTime(2026, 10, 15, 11, 24, 3),
        DateModification = new DateTime(2026, 10, 15, 17, 2, 0),
        ReferenceChantier = "CHANTIER-42",
        TicketSalesforce = "SF-1234",
        ConfigId = "Contura_00002978",
        Environnement = "local",
        ConfigurationEnregistree = true,
        DateFinProduction = new DateOnly(2026, 11, 3),
        ModeLivraison = "Chez le client",
        LivraisonCodeClient = "105789",
        LivraisonNomClient = "Test bascule sap",
        LivraisonLibelleAdresse = "Test bascule sap",
        LivraisonNom = "M. Dupont",
        LivraisonTelephone = "0102030405",
        LivraisonAdresse = "1 rue des Bassins",
        LivraisonCodePostal = "35000",
        LivraisonVille = "Rennes",
        Acces = "Semi-remorque;VL",
        AccesAutre = null,
        CommentaireExterne = "Livrer le matin",
    };

    private static DonneesExport Donnees(Dossier d, AgenceRetraitSap? agence = null, string? codeCcx = null) => new(
        d,
        new ClientSap("105789", "Test bascule sap", "France", null, "Encaissement par Virement", false, true),
        d.ModeLivraison == "Chez le client" ? new AdresseLivraisonSap("105789-S-1", "Test bascule sap", true, "M. Dupont", "0102030405", "1 rue des Bassins", null, "35000", "Rennes", "France") : null,
        agence,
        null,
        codeCcx,
        "Fabien Delpierre",
        new DateTime(2026, 10, 15, 17, 2, 30));

    // XML de fin de configuration tel que le Middleware le renvoie (déclaration utf-16 d'un XmlSerializer vers une chaîne).
    private const string XmlCpq = """
        <?xml version="1.0" encoding="utf-16"?>
        <Dossier xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
          <Entete>
            <Nature>C</Nature>
            <IDConfig>Contura_00002978</IDConfig>
          </Entete>
          <Attributs>
            <Attribut Key="ccx" Value="L" AttributGroup="InfoProd" />
          </Attributs>
        </Dossier>
        """;

    private ExportXmlDossier Export() =>
        new(Options.Create(new OptionsExport { DossierXml = _dossierTemporaire }), NullLogger<ExportXmlDossier>.Instance);

    private static string Valeur(XDocument x, string chemin) =>
        chemin.Split('/').Aggregate(x.Root!, (e, nom) => e.Element(nom) ?? throw new Xunit.Sdk.XunitException($"Élément {chemin} absent")).Value;

    [Theory]
    [InlineData("Liner_00012943", "Liner_00012943_portailSAP.xml")]
    [InlineData("CHIV_Initial_00000012", "CHIV_Initial_00000012_portailSAP.xml")]
    [InlineData(" Moon_00006869 ", "Moon_00006869_portailSAP.xml")]
    [InlineData("Volet/0001", "Volet_0001_portailSAP.xml")]
    public void NomFichier_ConfigIdSuiviDuSuffixe(string configId, string attendu)
    {
        Assert.Equal(attendu, ExportXmlDossier.NomFichier(configId));
    }

    [Fact]
    public void Construire_ContientLesDonneesSaisiesEtErp()
    {
        var x = ExportXmlDossier.Construire(Donnees(CommandeChezLeClient()));

        Assert.Equal("CommandePortailSAP", x.Root!.Name.LocalName);
        Assert.Equal("CF-2026-1001", Valeur(x, "Dossier/Reference"));
        Assert.Equal("Commande", Valeur(x, "Dossier/Type"));
        Assert.Equal("2026-10-15T11:24:03", Valeur(x, "Dossier/DateCreation"));
        Assert.Equal("Contura_00002978", Valeur(x, "Configuration/ConfigId"));
        Assert.Equal("Contura", Valeur(x, "Configuration/Configurateur"));
        Assert.Equal("Volet hors-sol", Valeur(x, "Produit/Famille"));
        Assert.Equal("France", Valeur(x, "Client/Pays"));
        Assert.Equal("false", Valeur(x, "Client/EstVic"));
        Assert.Equal("CHANTIER-42", Valeur(x, "References/ReferenceChantier"));
        Assert.Equal("2026-11-03", Valeur(x, "Production/DateFinProduction"));
        Assert.Equal("Chez le client", Valeur(x, "Livraison/Mode"));
        // Date de commande = jour de la validation (GenereLe), pas une date saisie.
        Assert.Equal("2026-10-15", Valeur(x, "Livraison/DateCommande"));
        Assert.Equal("105789-S-1", Valeur(x, "Livraison/Adresse/IdAdresseErp"));
        Assert.Equal("Rennes", Valeur(x, "Livraison/Adresse/Ville"));
        Assert.Equal(["Semi-remorque", "VL"], x.Root.Element("Livraison")!.Element("Acces")!.Elements("Type").Select(e => e.Value));
        Assert.Equal("Livrer le matin", Valeur(x, "Commentaires/Externe"));
        Assert.Equal("Fabien Delpierre", Valeur(x, "Generation/ValidePar"));
    }

    [Fact]
    public void Construire_TousLesChampsSontPresentsMemeVides()
    {
        var x = ExportXmlDossier.Construire(Donnees(CommandeChezLeClient()));

        // Champs non renseignés : présents et vides (le format reste le même d'une commande à l'autre).
        Assert.Equal(string.Empty, Valeur(x, "Dossier/NumeroSap"));
        Assert.Equal(string.Empty, Valeur(x, "Client/Langue"));
        Assert.Equal(string.Empty, Valeur(x, "Configuration/SiteProduction"));
        Assert.Equal(string.Empty, Valeur(x, "Production/CodeCcx"));
        Assert.Equal(string.Empty, Valeur(x, "Commentaires/Interne"));
        Assert.Equal(string.Empty, Valeur(x, "Livraison/AgenceRetrait/Code"));
        Assert.Equal(string.Empty, Valeur(x, "Livraison/Acces/Autre"));
    }

    [Fact]
    public void Construire_RetraitEnAgence_AgenceSansAdresseClient()
    {
        var d = CommandeChezLeClient();
        d.ModeLivraison = "Retrait en agence";
        d.AgenceRetrait = "Agence ACM";
        d.LivraisonCodeClient = d.LivraisonNomClient = d.LivraisonLibelleAdresse = null;
        d.LivraisonNom = d.LivraisonTelephone = d.LivraisonAdresse = d.LivraisonCodePostal = d.LivraisonVille = d.Acces = null;

        var x = ExportXmlDossier.Construire(Donnees(d, new AgenceRetraitSap("020", "Agence ACM", "Région Sud Est")));

        Assert.Equal("Retrait en agence", Valeur(x, "Livraison/Mode"));
        Assert.Equal("020", Valeur(x, "Livraison/AgenceRetrait/Code"));
        Assert.Equal("Agence ACM", Valeur(x, "Livraison/AgenceRetrait/Libelle"));
        Assert.Equal("Région Sud Est", Valeur(x, "Livraison/AgenceRetrait/Region"));
        Assert.Equal(string.Empty, Valeur(x, "Livraison/Adresse/IdAdresseErp"));
        Assert.Empty(x.Root!.Element("Livraison")!.Element("Acces")!.Elements("Type"));
    }

    [Fact]
    public void Construire_CodeCcxJusteAvantLaDateDeFinDeProduction()
    {
        var x = ExportXmlDossier.Construire(Donnees(CommandeChezLeClient(), codeCcx: "L"));

        var production = x.Root!.Element("Production")!.Elements().Select(e => e.Name.LocalName).ToList();
        Assert.Equal(["CodeCcx", "DateFinProduction"], production);
        Assert.Equal("L", Valeur(x, "Production/CodeCcx"));
    }

    [Fact]
    public async Task EcrireAsync_RepertoireDuConfigIdAvecLesDeuxXml()
    {
        var fichiers = await Export().EcrireAsync(Donnees(CommandeChezLeClient()), XmlCpq, CancellationToken.None);

        var repertoire = Path.Combine(_dossierTemporaire, "Contura_00002978");
        Assert.Equal(repertoire, fichiers.Repertoire);
        Assert.Equal(Path.Combine(repertoire, "Contura_00002978_portailSAP.xml"), fichiers.FichierPortail);
        Assert.Equal(Path.Combine(repertoire, "Contura_00002978.xml"), fichiers.FichierCpq);
        Assert.Equal(2, Directory.GetFiles(repertoire).Length); // pas de fichier temporaire restant
        Assert.Empty(Directory.GetFiles(_dossierTemporaire)); // rien à la racine du dossier d'export
    }

    [Fact]
    public async Task EcrireAsync_XmlCpqRecopieAvecUneDeclarationUtf8()
    {
        var fichiers = await Export().EcrireAsync(Donnees(CommandeChezLeClient()), XmlCpq, CancellationToken.None);

        var texte = File.ReadAllText(fichiers.FichierCpq);
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", texte);
        var cpq = XDocument.Load(fichiers.FichierCpq);
        Assert.Equal("Dossier", cpq.Root!.Name.LocalName);
        Assert.Equal("Contura_00002978", Valeur(cpq, "Entete/IDConfig"));
        Assert.Equal("L", cpq.Root.Element("Attributs")!.Element("Attribut")!.Attribute("Value")!.Value);
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", File.ReadAllText(fichiers.FichierPortail));
    }

    [Fact]
    public async Task EcrireAsync_RemplaceLesFichiersPrecedents()
    {
        var d = CommandeChezLeClient();
        await Export().EcrireAsync(Donnees(d), XmlCpq, CancellationToken.None);
        d.CommentaireExterne = "Modifié";
        var fichiers = await Export().EcrireAsync(Donnees(d), XmlCpq.Replace("Value=\"L\"", "Value=\"LC\""), CancellationToken.None);

        Assert.Equal("Modifié", Valeur(XDocument.Load(fichiers.FichierPortail), "Commentaires/Externe"));
        Assert.Contains("Value=\"LC\"", File.ReadAllText(fichiers.FichierCpq));
        Assert.Equal(2, Directory.GetFiles(fichiers.Repertoire).Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData("pas du xml")]
    public async Task EcrireAsync_XmlCpqVideOuIllisible_RienNEstEcrit(string xmlCpq)
    {
        await Assert.ThrowsAnyAsync<Exception>(() => Export().EcrireAsync(Donnees(CommandeChezLeClient()), xmlCpq, CancellationToken.None));

        Assert.Empty(Directory.GetFiles(_dossierTemporaire, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task EcrireAsync_CommandeIssueDuDevis_RemplaceLeXmlDuDevis()
    {
        var devis = CommandeChezLeClient();
        devis.Type = TypeDossier.Devis;
        devis.Reference = "DV-2026-1001";
        await Export().EcrireAsync(Donnees(devis), XmlCpq, CancellationToken.None);

        // Commande issue du devis : même ConfigId, donc même répertoire et mêmes fichiers.
        var commande = CommandeChezLeClient();
        commande.Reference = "CF-2026-1007";
        commande.DevisSourceId = 99;
        var fichiers = await Export().EcrireAsync(Donnees(commande) with { DevisSourceReference = "DV-2026-1001" }, XmlCpq, CancellationToken.None);

        var x = XDocument.Load(fichiers.FichierPortail);
        Assert.Single(Directory.GetDirectories(_dossierTemporaire));
        Assert.Equal(2, Directory.GetFiles(fichiers.Repertoire).Length);
        Assert.Equal("Commande", Valeur(x, "Dossier/Type"));
        Assert.Equal("CF-2026-1007", Valeur(x, "Dossier/Reference"));
        Assert.Equal("DV-2026-1001", Valeur(x, "Dossier/DevisSource"));
    }

    [Fact]
    public async Task EcrireAsync_SansConfigId_EstRefuse()
    {
        var d = CommandeChezLeClient();
        d.ConfigId = null;

        await Assert.ThrowsAsync<RequeteInvalideException>(() => Export().EcrireAsync(Donnees(d), XmlCpq, CancellationToken.None));
    }

    [Fact]
    public void EstUtilisable_DossierAbsentOuVide()
    {
        Assert.True(OptionsExport.EstUtilisable(_dossierTemporaire));
        Assert.False(OptionsExport.EstUtilisable(Path.Combine(_dossierTemporaire, "inexistant")));
        Assert.False(OptionsExport.EstUtilisable(null));
        Assert.False(OptionsExport.EstUtilisable(" "));
    }
}
