namespace CfSaisie.Api;

public sealed class OptionsMiddleware
{
    public const string Section = "Middleware";

    /// <summary>Si vrai, aucun appel réseau : le Middleware est simulé.</summary>
    public bool Simulation { get; set; }

    public OptionsProfil Profil { get; set; } = new();

    /// <summary>Environnements disponibles, par code (« test », « production »).</summary>
    public Dictionary<string, OptionsEnvironnement> Environnements { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string EnvironnementParDefaut { get; set; } = "test";

    /// <summary>Profil utilisé pour un environnement : le sien s'il est déclaré, sinon le profil commun.</summary>
    public OptionsProfil ProfilDe(string environnement) => Environnements[environnement].Profil ?? Profil;
}

public sealed class OptionsEnvironnement
{
    /// <summary>Libellé du badge (MODE TEST, PRODUCTION).</summary>
    public string Libelle { get; set; } = string.Empty;

    /// <summary>Hôte du Middleware, ex. test.cfgroup.smartconfigurator.fr.</summary>
    public string Serveur { get; set; } = string.Empty;

    /// <summary>Profil propre à cet environnement (ex. Middleware local) ; à défaut, Middleware:Profil.</summary>
    public OptionsProfil? Profil { get; set; }
}

public sealed class OptionsProfil
{
    public string Identifiant { get; set; } = string.Empty;

    /// <summary>Jamais dans appsettings : user-secrets en local, coffre de secrets ensuite.</summary>
    public string? MotDePasse { get; set; }

    public bool ModeErp { get; set; }
}

public sealed class OptionsPortail
{
    public const string Section = "Portail";

    public string Societe { get; set; } = "CF Group";
}
