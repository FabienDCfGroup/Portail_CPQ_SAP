using System.Text.RegularExpressions;

namespace CfSaisie.Cpq;

/// <summary>Règle de saisie du nom d'un configurateur (ConfiguratorName attendu par le Middleware).</summary>
public static partial class NomConfigurateur
{
    public const int LongueurMax = 100;

    /// <summary>
    /// Lettres, chiffres et « _ » (ex. Volet, MA10PE, CHIV_Initial), sans « _ » en début ni en fin :
    /// le nom sert de préfixe au ConfigId (« &lt;nom&gt;_&lt;numéro&gt; », voir <see cref="ConfigIdCpq"/>).
    /// </summary>
    public static bool EstValide(string? nom) =>
        nom is { Length: > 0 and <= LongueurMax } && Format().IsMatch(nom);

    [GeneratedRegex("^[A-Za-z0-9]+(_[A-Za-z0-9]+)*$")]
    private static partial Regex Format();
}
