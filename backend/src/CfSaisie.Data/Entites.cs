namespace CfSaisie.Data;

/// <summary>Configurateur du Middleware CPQ (repris de Produits.csv de l'ancien outil).</summary>
public class Configurateur
{
    public int Id { get; set; }

    /// <summary>Nom exact attendu par le Middleware (ConfiguratorName).</summary>
    public string Nom { get; set; } = string.Empty;

    /// <summary>Site de production ; DEL_BRECE par défaut s'il est vide.</summary>
    public string? ProductionSite { get; set; }

    public bool Actif { get; set; } = true;
}

public class Famille
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Image { get; set; }
    public bool Actif { get; set; } = true;
    public List<Produit> Produits { get; set; } = [];
}

public class Produit
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Image { get; set; }

    /// <summary>Lien libre conservé de la maquette (non utilisé pour le lancement, qui passe par le Middleware).</summary>
    public string? LienConfigurateur { get; set; }

    public int ConfigurateurId { get; set; }
    public Configurateur Configurateur { get; set; } = null!;

    public int? FamilleId { get; set; }
    public Famille? Famille { get; set; }
}

/// <summary>Produit accessible à un client (le client lui-même vient de SAP).</summary>
public class Accreditation
{
    public string CodeClient { get; set; } = string.Empty;
    public int ProduitId { get; set; }
    public Produit Produit { get; set; } = null!;
}

public enum TypeDossier
{
    Commande,
    Devis,
}

public enum StatutDossier
{
    EnCours,
    Envoye,
    EnAttente,
    AValider,
    Validee,
    Erreur,
    /// <summary>Configurateur fermé avant la fin de la configuration : dossier conservé pour trace, masqué du suivi.</summary>
    Abandonne,
}

public enum OrigineDossier
{
    CreationInterne,
    SiteInternet,
}

/// <summary>Commande ou devis saisi dans le portail.</summary>
public class Dossier
{
    public int Id { get; set; }

    /// <summary>CF-2026-1054 (commande), DV-2026-1051 (devis), WEB-… (site internet).</summary>
    public string Reference { get; set; } = string.Empty;

    public TypeDossier Type { get; set; }
    public OrigineDossier Origine { get; set; }
    public StatutDossier Statut { get; set; }

    public string CodeClient { get; set; } = string.Empty;
    public string NomClient { get; set; } = string.Empty;

    public int? ProduitId { get; set; }
    public Produit? Produit { get; set; }
    public string NomProduit { get; set; } = string.Empty;
    public string NomFamille { get; set; } = string.Empty;

    public string CreePar { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public DateTime DateModification { get; set; }

    public string? ReferenceChantier { get; set; }
    public string? TicketSalesforce { get; set; }

    // Configuration CPQ
    public string? ConfigId { get; set; }

    /// <summary>Environnement Middleware où la configuration existe (test ou production).</summary>
    public string? Environnement { get; set; }

    public bool ConfigurationEnregistree { get; set; }

    // Finalisation
    public DateOnly? DateFinProduction { get; set; }
    public string? CommentaireExterne { get; set; }
    public string? CommentaireInterne { get; set; }

    // Livraison / retrait
    public string? ModeLivraison { get; set; }
    public string? LivraisonCodeClient { get; set; }
    public string? LivraisonNomClient { get; set; }
    public string? LivraisonLibelleAdresse { get; set; }
    public string? LivraisonNom { get; set; }
    public string? LivraisonTelephone { get; set; }
    public string? LivraisonAdresse { get; set; }
    public string? LivraisonCodePostal { get; set; }
    public string? LivraisonVille { get; set; }

    /// <summary>Types d'accès séparés par « ; » (Semi-remorque, Porteur, VL, Autre).</summary>
    public string? Acces { get; set; }
    public string? AccesAutre { get; set; }
    public string? AgenceRetrait { get; set; }

    // SAP
    public string? NumeroSap { get; set; }

    // Transformation devis → commande
    public int? DevisSourceId { get; set; }
    public string? CommandeIssueReference { get; set; }
    public string? ReferenceCommandeClient { get; set; }
    public DateOnly? DateCommande { get; set; }
    public DateOnly? DateLivraisonSouhaitee { get; set; }
}

public class CommentaireFamille
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public int Ordre { get; set; }
    public List<CommentaireModele> Modeles { get; set; } = [];
}

public class CommentaireModele
{
    public int Id { get; set; }
    public int FamilleId { get; set; }
    public CommentaireFamille Famille { get; set; } = null!;
    public int Ordre { get; set; }
    public string Texte { get; set; } = string.Empty;
}

public class MessageTableauBord
{
    public int Id { get; set; }

    /// <summary>"Tous" ou la raison sociale d'un client.</summary>
    public string Audience { get; set; } = "Tous";
    public string Titre { get; set; } = string.Empty;
    public string Corps { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
}

/// <summary>
/// Journal des connexions au portail (authentification Microsoft Entra ID) : qui, quand, avec quel rôle, et les refus.
/// Les utilisateurs eux-mêmes ne sont pas stockés : ils viennent de l'annuaire.
/// </summary>
public class JournalConnexion
{
    public long Id { get; set; }
    public DateTime Date { get; set; }

    /// <summary>Résultat : « Succes », « RefuseSansGroupe » (aucun groupe du portail) ou « Echec » (erreur d'authentification).</summary>
    public string Resultat { get; set; } = string.Empty;

    /// <summary>Identifiant d'objet Entra ID (oid) ; null si l'authentification a échoué avant de connaître l'utilisateur.</summary>
    public string? ObjetId { get; set; }

    /// <summary>Compte de connexion (UPN, ex. prenom.nom@cf.group).</summary>
    public string? Compte { get; set; }

    public string? Nom { get; set; }

    /// <summary>Rôle attribué par les groupes (Utilisateur, Administrateur, SuperAdministrateur).</summary>
    public string? Role { get; set; }

    public string? Message { get; set; }
    public string? AdresseIp { get; set; }
    public string? Navigateur { get; set; }
}

/// <summary>
/// Langue de l'interface, aussi envoyée au configurateur. Une langue désactivée n'est plus proposée.
/// Les configurateurs gèrent FR, EN, DE et NL ; PT est présente mais désactivée (aucun configurateur traduit).
/// </summary>
public class Langue
{
    /// <summary>Code ISO 639-1 en minuscules (fr, en, de, nl, pt).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Nom de la langue dans cette langue (Français, English…).</summary>
    public string Libelle { get; set; } = string.Empty;

    public bool Actif { get; set; } = true;
    public int Ordre { get; set; }
}
