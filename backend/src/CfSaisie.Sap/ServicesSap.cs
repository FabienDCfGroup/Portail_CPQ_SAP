namespace CfSaisie.Sap;

// Contrats des données ERP (SAP/AX) utilisées par le portail : clients, adresses de livraison, agences de retrait.
// Implémentation : lecture des tables du schéma « erp » de la base du portail, alimentées par le flux ERP
// (contrat : Analyse/04_Contrat_tables_ERP.md). Le portail n'écrit jamais dans ces tables.

public sealed record ClientSap(
    string Code,
    string RaisonSociale,
    string Pays,
    /// <summary>Code langue du client (FR, EN, DE, NL…), utilisé pour ouvrir le configurateur ; null si non renseigné.</summary>
    string? Langue,
    string? ModePaiement,
    /// <summary>Client en paiement VIC (paiement à la commande), indiqué par l'ERP.</summary>
    bool EstVic,
    /// <summary>Un client inactif n'est plus proposé et ne peut plus recevoir de nouvelle commande.</summary>
    bool Actif);

public sealed record AdresseLivraisonSap(
    string Id,
    string Libelle,
    bool EstPrincipale,
    string Nom,
    string Telephone,
    string Adresse,
    string? ComplementAdresse,
    string CodePostal,
    string Ville,
    string Pays);

public sealed record AgenceRetraitSap(string Code, string Libelle, string? Region);

public interface ISapClientService
{
    /// <summary>Clients actifs dont le code commence par, ou la raison sociale contient, le texte recherché.</summary>
    Task<IReadOnlyList<ClientSap>> RechercherAsync(string? recherche, int maximum, CancellationToken annulation = default);

    /// <summary>Client par son code, actif ou non (un dossier existant doit rester lisible).</summary>
    Task<ClientSap?> LireAsync(string code, CancellationToken annulation = default);

    /// <summary>Adresses de livraison du client, adresse principale en premier.</summary>
    Task<IReadOnlyList<AdresseLivraisonSap>> AdressesAsync(string code, CancellationToken annulation = default);
}

public interface ISapLogistiqueService
{
    /// <summary>Agences de retrait actives.</summary>
    Task<IReadOnlyList<AgenceRetraitSap>> AgencesAsync(CancellationToken annulation = default);
}
