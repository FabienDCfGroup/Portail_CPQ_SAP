namespace CfSaisie.Sap;

// Contrats des échanges avec SAP. Aucune API SAP n'étant documentée à ce stade,
// seules des implémentations simulées existent (voir SapSimule).
// Les données attendues sont décrites dans Analyse/02_Donnees_ERP_portail.md.

public sealed record ClientSap(
    string Code,
    string RaisonSociale,
    string Pays,
    /// <summary>Code langue du client (FR, EN, DE, NL, PT…), utilisé pour ouvrir le configurateur.</summary>
    string Langue,
    /// <summary>Mode de paiement ; "VIC" = paiement à la commande.</summary>
    string? ModePaiement)
{
    public bool EstVic => string.Equals(ModePaiement, "VIC", StringComparison.OrdinalIgnoreCase);
}

public sealed record AdresseLivraisonSap(
    string Id,
    string Libelle,
    bool EstPrincipale,
    string Nom,
    string Telephone,
    string Adresse,
    string CodePostal,
    string Ville,
    string Pays);

public sealed record AgenceRetraitSap(string Code, string Libelle);

public enum EtatCapacite
{
    Disponible,
    Limitee,
    Complet,
}

public sealed record JourPlanningSap(DateOnly Date, EtatCapacite Etat, string Libelle);

public sealed record CommandeAEnvoyer(
    string ReferencePortail,
    bool EstDevis,
    string CodeClient,
    string Produit,
    string? ConfigId,
    DateOnly? DateFinProduction);

public sealed record ResultatEnvoiSap(bool Succes, string? NumeroSap, string? Message);

public interface ISapClientService
{
    Task<IReadOnlyList<ClientSap>> RechercherAsync(string? recherche, int maximum, CancellationToken annulation = default);

    Task<ClientSap?> LireAsync(string code, CancellationToken annulation = default);

    Task<IReadOnlyList<AdresseLivraisonSap>> AdressesAsync(string code, CancellationToken annulation = default);
}

public interface ISapLogistiqueService
{
    Task<IReadOnlyList<AgenceRetraitSap>> AgencesAsync(CancellationToken annulation = default);

    /// <summary>Jours ouvrés proposables pour la fin de production d'un produit.</summary>
    Task<(int DelaiJours, IReadOnlyList<JourPlanningSap> Jours)> PlanningAsync(string produit, DateOnly aPartirDu, int nombreJours, CancellationToken annulation = default);
}

public interface ISapCommandeService
{
    Task<ResultatEnvoiSap> EnvoyerAsync(CommandeAEnvoyer commande, CancellationToken annulation = default);
}
