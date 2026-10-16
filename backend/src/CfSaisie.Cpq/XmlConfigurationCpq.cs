using System.Xml;
using System.Xml.Linq;

namespace CfSaisie.Cpq;

/// <summary>
/// Informations utiles du XML généré par le configurateur en fin de configuration (élément racine « Dossier »),
/// tel que renvoyé par le Middleware (GetXmlForSite).
/// </summary>
/// <param name="CodeCcx">Ligne de fabrication (ex. « L », « LC », « LGV ») : attribut ccx, calculé par le configurateur ; null s'il n'en écrit pas.</param>
/// <param name="ProductionSite">Site de production de la configuration (Entete/ProductionSite).</param>
/// <param name="Nature">Nature de la dernière validation (Entete/Nature) : « C » commande, « D » devis.</param>
public sealed record XmlConfigurationCpq(string? CodeCcx, string? ProductionSite, string? Nature = null)
{
    /// <summary>
    /// Lit <c>/Dossier/Attributs/Attribut[@Key="ccx"]/@Value</c>, <c>/Dossier/Entete/ProductionSite</c> et <c>/Dossier/Entete/Nature</c>.
    /// La règle de calcul du code CCX est propre à chaque configurateur : le portail ne fait que lire la valeur produite.
    /// </summary>
    /// <exception cref="FormatException">Le texte n'est pas un XML « Dossier » lisible.</exception>
    public static XmlConfigurationCpq Lire(string xml)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (XmlException ex)
        {
            throw new FormatException("Le XML de la configuration est illisible.", ex);
        }

        var dossier = document.Root;
        if (dossier is null || dossier.Name.LocalName != "Dossier")
        {
            throw new FormatException("Le XML de la configuration n'a pas l'élément racine « Dossier » attendu.");
        }

        var codeCcx = dossier.Elements().Where(e => e.Name.LocalName == "Attributs")
            .Elements().Where(e => e.Name.LocalName == "Attribut")
            .FirstOrDefault(e => string.Equals((string?)e.Attribute("Key"), "ccx", StringComparison.OrdinalIgnoreCase))
            ?.Attribute("Value")?.Value.Trim();

        var entete = dossier.Elements().Where(e => e.Name.LocalName == "Entete").Elements().ToList();
        var site = entete.FirstOrDefault(e => e.Name.LocalName == "ProductionSite")?.Value.Trim();
        var nature = entete.FirstOrDefault(e => e.Name.LocalName == "Nature")?.Value.Trim();

        return new XmlConfigurationCpq(
            string.IsNullOrEmpty(codeCcx) ? null : codeCcx,
            string.IsNullOrEmpty(site) ? null : site,
            string.IsNullOrEmpty(nature) ? null : nature.ToUpperInvariant());
    }
}
