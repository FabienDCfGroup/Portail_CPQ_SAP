namespace CfSaisie.Cpq;

/// <summary>Lecture d'un ConfigId simplifié du Middleware : « &lt;Configurateur&gt;_&lt;numéro&gt; » (ex. Volet_00000561).</summary>
public static class ConfigIdCpq
{
    /// <summary>
    /// Configurateur d'un ConfigId saisi pour une modification ou une consultation.
    /// L'ancien outil prenait le texte avant le premier « _ » (Configurateur.CreeRequete), ce qui donne « CHIV »
    /// pour CHIV_Initial_00000012 : on retient ici le plus long nom de configurateur connu suivi de « _ ».
    /// </summary>
    /// <returns>Le nom du configurateur tel qu'enregistré, ou null si aucun ne correspond.</returns>
    public static string? Configurateur(string? configId, IEnumerable<string> configurateursConnus)
    {
        ArgumentNullException.ThrowIfNull(configurateursConnus);
        var id = configId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return configurateursConnus
            .Where(nom => !string.IsNullOrWhiteSpace(nom)
                && id.Length > nom.Length + 1
                && id.StartsWith(nom + "_", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(nom => nom.Length)
            .FirstOrDefault();
    }
}
