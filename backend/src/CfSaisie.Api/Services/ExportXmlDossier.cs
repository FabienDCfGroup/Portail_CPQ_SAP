using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using CfSaisie.Data;
using CfSaisie.Sap;
using Microsoft.Extensions.Options;

namespace CfSaisie.Api.Services;

/// <summary>
/// Dossier d'export des fichiers XML du portail (section « Export »). Le chemin diffère selon l'environnement :
/// appsettings.Development.json sur le poste, variable d'environnement Export__DossierXml en test et en production.
/// </summary>
public sealed class OptionsExport
{
    public const string Section = "Export";

    public string? DossierXml { get; set; }

    /// <summary>Le dossier doit exister et être accessible en écriture (vérifié au démarrage de l'API).</summary>
    public static bool EstUtilisable(string? dossier)
    {
        if (string.IsNullOrWhiteSpace(dossier) || !Directory.Exists(dossier)) return false;
        var essai = Path.Combine(dossier, $".essai-ecriture-{Guid.NewGuid():N}");
        try
        {
            File.WriteAllText(essai, string.Empty);
            File.Delete(essai);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}

public sealed class ExportXmlException(string message, Exception? innerException = null) : Exception(message, innerException);

/// <summary>Tout ce qui est écrit dans le XML : le dossier, et les données ERP retrouvées au moment de la validation.</summary>
public sealed record DonneesExport(
    Dossier Dossier,
    ClientSap? Client,
    AdresseLivraisonSap? AdresseErp,
    AgenceRetraitSap? Agence,
    string? DevisSourceReference,
    string? CodeCcx,
    string ValidePar,
    DateTime GenereLe);

/// <summary>Fichiers écrits pour SAP à la validation : répertoire du ConfigId et ses deux XML.</summary>
public sealed record FichiersExport(string Repertoire, string FichierPortail, string FichierCpq);

/// <summary>
/// Export pour SAP généré à la validation d'une commande ou d'un devis, dans un répertoire au nom du ConfigId :
/// « {ConfigId}_portailSAP.xml » (toutes les données saisies dans le portail, avec les données ERP associées ; tous les champs
/// présents, vides s'ils ne sont pas renseignés) et « {ConfigId}.xml » (XML de fin de configuration lu dans le Middleware).
/// </summary>
public sealed class ExportXmlDossier(IOptions<OptionsExport> options, ILogger<ExportXmlDossier> journal)
{
    public const string Suffixe = "_portailSAP.xml";
    public const string SuffixeCpq = ".xml";

    /// <summary>ConfigId utilisable comme nom de fichier ou de répertoire (caractères interdits remplacés par « _ »).</summary>
    public static string NomConfig(string configId)
    {
        var interdits = Path.GetInvalidFileNameChars();
        return new string(configId.Trim().Select(c => interdits.Contains(c) ? '_' : c).ToArray());
    }

    /// <summary>Nom du fichier du portail : ConfigId du CPQ + « _portailSAP.xml ».</summary>
    public static string NomFichier(string configId) => NomConfig(configId) + Suffixe;

    /// <summary>Nom du fichier du CPQ : ConfigId + « .xml » (nom donné par le configurateur à son propre fichier).</summary>
    public static string NomFichierCpq(string configId) => NomConfig(configId) + SuffixeCpq;

    /// <summary>
    /// Écrit les deux XML dans {DossierXml}/{ConfigId}/ (remplace la version précédente : devis puis commande issue du devis).
    /// Les deux fichiers sont écrits en temporaire puis renommés : SAP ne lit jamais un fichier à moitié écrit.
    /// </summary>
    /// <param name="xmlCpq">XML de fin de configuration renvoyé par le Middleware (GetXmlForSite).</param>
    public async Task<FichiersExport> EcrireAsync(DonneesExport donnees, string xmlCpq, CancellationToken annulation)
    {
        var configId = donnees.Dossier.ConfigId;
        if (string.IsNullOrWhiteSpace(configId))
        {
            throw new RequeteInvalideException($"Le dossier {donnees.Dossier.Reference} n'a pas de configuration (ConfigId) : le fichier XML ne peut pas être généré.");
        }
        if (string.IsNullOrWhiteSpace(xmlCpq))
        {
            throw new RequeteInvalideException($"Le XML du configurateur pour {configId} est vide : enregistrez la configuration, puis réessayez.");
        }

        var repertoire = Path.Combine(options.Value.DossierXml!, NomConfig(configId));
        var portail = Path.Combine(repertoire, NomFichier(configId));
        var cpq = Path.Combine(repertoire, NomFichierCpq(configId));
        // XML du CPQ vérifié avant toute écriture : un XML illisible ne laisse aucun fichier.
        var documentCpq = DocumentCpq(xmlCpq);
        string[] temporaires = [portail + ".tmp", cpq + ".tmp"];
        try
        {
            Directory.CreateDirectory(repertoire);
            await EcrireXmlAsync(temporaires[0], Construire(donnees), annulation);
            await EcrireXmlAsync(temporaires[1], documentCpq, annulation);
            File.Move(temporaires[0], portail, overwrite: true);
            File.Move(temporaires[1], cpq, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            foreach (var t in temporaires)
            {
                try { File.Delete(t); } catch (IOException) { }
            }
            throw new ExportXmlException($"Les fichiers XML de la commande n'ont pas pu être écrits dans {repertoire} : {ex.Message}", ex);
        }

        journal.LogInformation("Fichiers XML générés pour {Dossier} dans {Repertoire}", donnees.Dossier.Reference, repertoire);
        return new FichiersExport(repertoire, portail, cpq);
    }

    private static async Task EcrireXmlAsync(string chemin, XDocument document, CancellationToken annulation)
    {
        var reglages = new XmlWriterSettings { Async = true, Indent = true, Encoding = new UTF8Encoding(false) };
        await using var flux = File.Create(chemin);
        await using var ecrivain = XmlWriter.Create(flux, reglages);
        await document.SaveAsync(ecrivain, annulation);
    }

    /// <summary>XML du CPQ relu tel quel ; seule la déclaration est réécrite en UTF-8, l'encodage réel du fichier.</summary>
    private static XDocument DocumentCpq(string xml)
    {
        try
        {
            var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            document.Declaration = new XDeclaration("1.0", "utf-8", null);
            return document;
        }
        catch (XmlException ex)
        {
            throw new ExportXmlException($"Le XML du configurateur est illisible : {ex.Message}", ex);
        }
    }

    public static XDocument Construire(DonneesExport e)
    {
        var d = e.Dossier;
        var c = e.Client;
        var retrait = d.ModeLivraison == "Retrait en agence";

        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("CommandePortailSAP",
                new XAttribute("version", "1"),
                new XElement("Generation",
                    Champ("Date", e.GenereLe),
                    Champ("ValidePar", e.ValidePar),
                    Champ("Application", "CF Saisie commandes")),
                new XElement("Dossier",
                    Champ("Reference", d.Reference),
                    Champ("Type", d.Type.ToString()),
                    Champ("Origine", d.Origine.ToString()),
                    Champ("Statut", d.Statut.ToString()),
                    Champ("CreePar", d.CreePar),
                    Champ("DateCreation", d.DateCreation),
                    Champ("DateModification", d.DateModification),
                    Champ("NumeroSap", d.NumeroSap),
                    Champ("DevisSource", e.DevisSourceReference),
                    Champ("CommandeIssue", d.CommandeIssueReference)),
                new XElement("Configuration",
                    Champ("ConfigId", d.ConfigId),
                    Champ("Configurateur", d.Produit?.Configurateur?.Nom),
                    Champ("SiteProduction", d.Produit?.Configurateur?.ProductionSite),
                    Champ("Environnement", d.Environnement),
                    Champ("ConfigurationEnregistree", d.ConfigurationEnregistree)),
                new XElement("Produit",
                    Champ("Id", d.ProduitId),
                    Champ("Nom", d.NomProduit),
                    Champ("Famille", d.NomFamille)),
                new XElement("Client",
                    Champ("Code", d.CodeClient),
                    Champ("RaisonSociale", c?.RaisonSociale ?? d.NomClient),
                    Champ("Pays", c?.Pays),
                    Champ("Langue", c?.Langue),
                    Champ("ModePaiement", c?.ModePaiement),
                    Champ("EstVic", c?.EstVic),
                    Champ("Actif", c?.Actif)),
                new XElement("References",
                    Champ("ReferenceChantier", d.ReferenceChantier),
                    Champ("TicketSalesforce", d.TicketSalesforce),
                    Champ("ReferenceCommandeClient", d.ReferenceCommandeClient)),
                new XElement("Production",
                    // Ligne de fabrication calculée par le configurateur (attribut ccx de son XML), vide s'il n'en écrit pas.
                    Champ("CodeCcx", e.CodeCcx),
                    Champ("DateFinProduction", d.DateFinProduction)),
                new XElement("Livraison",
                    Champ("Mode", d.ModeLivraison),
                    // Date de commande = date du jour de la génération, c'est-à-dire de la validation.
                    Champ("DateCommande", DateOnly.FromDateTime(e.GenereLe)),
                    Champ("DateLivraisonSouhaitee", d.DateLivraisonSouhaitee),
                    new XElement("Adresse",
                        Champ("CodeClient", retrait ? null : d.LivraisonCodeClient),
                        Champ("NomClient", retrait ? null : d.LivraisonNomClient),
                        Champ("IdAdresseErp", retrait ? null : e.AdresseErp?.Id),
                        Champ("Libelle", retrait ? null : d.LivraisonLibelleAdresse),
                        Champ("NomDestinataire", d.LivraisonNom),
                        Champ("Telephone", d.LivraisonTelephone),
                        Champ("Adresse", d.LivraisonAdresse),
                        Champ("CodePostal", d.LivraisonCodePostal),
                        Champ("Ville", d.LivraisonVille),
                        Champ("Pays", retrait ? null : e.AdresseErp?.Pays)),
                    new XElement("Acces",
                        (d.Acces ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(a => new XElement("Type", a)),
                        Champ("Autre", d.AccesAutre)),
                    new XElement("AgenceRetrait",
                        Champ("Code", retrait ? e.Agence?.Code : null),
                        Champ("Libelle", retrait ? d.AgenceRetrait : null),
                        Champ("Region", retrait ? e.Agence?.Region : null))),
                new XElement("Commentaires",
                    Champ("Externe", d.CommentaireExterne),
                    Champ("Interne", d.CommentaireInterne))));
    }

    // Formats stables pour SAP : dates ISO 8601, booléens true/false, élément vide si la valeur est absente.
    private static XElement Champ(string nom, object? valeur) => new(nom, valeur switch
    {
        null => string.Empty,
        DateTime dt => dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
        DateOnly jour => jour.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => valeur.ToString() ?? string.Empty,
    });
}
