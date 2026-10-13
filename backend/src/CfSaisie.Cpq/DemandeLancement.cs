namespace CfSaisie.Cpq;

public enum ActionConfigurateur
{
    Creation,
    Modification,
    Consultation,
}

public enum TypeDocument
{
    Devis,
    Commande,
}

public enum LangueConfigurateur
{
    Francais,
    Anglais,
    Allemand,
    Neerlandais,
}

/// <summary>Configurateur tel que paramétré dans la base du portail.</summary>
/// <param name="Nom">Nom exact attendu par le Middleware (ex. "Volet", "CHIV_Initial").</param>
/// <param name="ProductionSite">Site de production ; <see cref="OpenRequestBuilder.ProductionSiteParDefaut"/> s'il n'est pas renseigné.</param>
public sealed record ConfigurateurCpq(string Nom, string? ProductionSite = null);

/// <summary>Demande de lancement d'un configurateur, telle que saisie dans le portail.</summary>
public sealed record DemandeLancement
{
    public required ActionConfigurateur Action { get; init; }

    public required TypeDocument TypeDocument { get; init; }

    public required ConfigurateurCpq Configurateur { get; init; }

    public string? CodeClient { get; init; }

    /// <summary>Identifiant de la configuration existante (modification et consultation).</summary>
    public string? ConfigId { get; init; }

    public LangueConfigurateur? Langue { get; init; }

    /// <summary>Envoyée dans le champ BusinessCode.</summary>
    public string? ReferenceChantier { get; init; }

    public string? Email { get; init; }

    /// <summary>Envoyée dans le champ ClientLangueNotice, uniquement si elle est renseignée.</summary>
    public string? LangueNotice { get; init; }
}
