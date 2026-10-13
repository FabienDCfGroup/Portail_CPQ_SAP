namespace CfSaisie.Data;

// Tables du schéma SQL « erp » : alimentées par le flux de l'ERP (SAP/AX), lues par le portail.
// Le contrat d'alimentation est décrit dans Analyse/04_Contrat_tables_ERP.md.
// DateMaj : date de dernière mise à jour par l'ERP (synchronisation incrémentale).

/// <summary>erp.Clients — clients pouvant passer commande.</summary>
public class ClientErp
{
    public string CodeClient { get; set; } = string.Empty;
    public string RaisonSociale { get; set; } = string.Empty;
    public string Pays { get; set; } = string.Empty;

    /// <summary>Code langue (FR, EN, DE, NL, PT…), envoyé au configurateur.</summary>
    public string? Langue { get; set; }

    public string? ModePaiement { get; set; }

    /// <summary>Client en paiement VIC (paiement à la commande) : alerte avant validation.</summary>
    public bool EstVic { get; set; }

    public bool Actif { get; set; } = true;
    public DateTime DateMaj { get; set; }

    public List<AdresseClientErp> Adresses { get; set; } = [];
}

/// <summary>erp.AdressesClients — adresses de livraison d'un client.</summary>
public class AdresseClientErp
{
    /// <summary>Identifiant de l'adresse dans l'ERP.</summary>
    public string IdAdresse { get; set; } = string.Empty;

    public string CodeClient { get; set; } = string.Empty;
    public ClientErp Client { get; set; } = null!;

    /// <summary>Ex. « Adresse principale », « Dépôt secondaire ».</summary>
    public string Libelle { get; set; } = string.Empty;

    /// <summary>Une seule adresse principale par client (index unique filtré).</summary>
    public bool EstPrincipale { get; set; }

    public string NomDestinataire { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public string Adresse { get; set; } = string.Empty;
    public string? ComplementAdresse { get; set; }
    public string CodePostal { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string Pays { get; set; } = string.Empty;
    public DateTime DateMaj { get; set; }
}

/// <summary>erp.ModesLivraison — modes de livraison référencés dans l'ERP.</summary>
public class ModeLivraisonErp
{
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public bool Actif { get; set; } = true;
    public DateTime DateMaj { get; set; }
}

/// <summary>erp.AgencesRetrait — agences où le client peut retirer sa commande.</summary>
public class AgenceRetraitErp
{
    public string CodeAgence { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public string? Region { get; set; }

    /// <summary>Adresse ou ville de l'agence.</summary>
    public string? Localisation { get; set; }

    public bool Actif { get; set; } = true;
    public DateTime DateMaj { get; set; }
}

/// <summary>erp.Commandes — suivi des commandes de l'ERP ; une commande porte au plus une configuration CPQ.</summary>
public class CommandeErp
{
    /// <summary>Numéro de commande SAP/AX.</summary>
    public string NumeroCommande { get; set; } = string.Empty;

    public string CodeClient { get; set; } = string.Empty;
    public ClientErp Client { get; set; } = null!;

    public string? ReferenceCommandeClient { get; set; }

    /// <summary>Référence du dossier du portail (ex. CF-2026-1001), si la commande en est issue.</summary>
    public string? ReferencePortail { get; set; }

    /// <summary>ConfigId CPQ (ex. Volet_00000561) : lien avec la configuration et avec Dossiers.ConfigId.</summary>
    public string? ConfigId { get; set; }

    public DateTime DateCreation { get; set; }
    public string StatutErp { get; set; } = string.Empty;
    public DateOnly? DateDebutProduction { get; set; }
    public DateOnly? DateFinProduction { get; set; }
    public DateOnly? DateLivraisonPrevue { get; set; }
    public DateOnly? DateLivraisonReelle { get; set; }
    public DateTime DateMaj { get; set; }
}
