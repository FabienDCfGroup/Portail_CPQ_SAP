using CfSaisie.Cpq;
using CfSaisie.Data;
using CfSaisie.Sap;

namespace CfSaisie.Api;

public sealed record EnvironnementDto(string Code, string Libelle);

/// <summary>Session de la personne connectée (Entra ID) : nom, compte et rôle attribué par ses groupes.</summary>
public sealed record SessionDto(
    string Utilisateur,
    string Initiales,
    string? Compte,
    string Role,
    string Societe,
    IReadOnlyList<EnvironnementDto> Environnements,
    string EnvironnementParDefaut,
    IReadOnlyList<LangueDto> Langues,
    string LangueParDefaut);

/// <summary>Langue active proposée dans le portail (table Langues).</summary>
public sealed record LangueDto(string Code, string Libelle);

public sealed record ClientDto(string Code, string RaisonSociale, string Pays, string? Langue, bool EstVic)
{
    public static ClientDto De(ClientSap c) => new(c.Code, c.RaisonSociale, c.Pays, c.Langue, c.EstVic);
}

public sealed record ClientDetailDto(ClientDto Client, IReadOnlyList<AdresseLivraisonSap> Adresses);

public sealed record ProduitCatalogueDto(int Id, string Nom, string? Description, string? Image, string Configurateur);

public sealed record FamilleCatalogueDto(int Id, string Nom, string? Image, IReadOnlyList<ProduitCatalogueDto> Produits);

public sealed record LancementDto(bool Succes, bool PeutOuvrir, string? Message, string? Url, string? ConfigId);

/// <summary>Données saisies dans l'assistant de création (client, références, livraison, finalisation).</summary>
public sealed record SaisieDossierDto
{
    public TypeDossier Type { get; init; }
    public string? CodeClient { get; init; }
    public string? ReferenceChantier { get; init; }
    public string? TicketSalesforce { get; init; }
    public string? ModeLivraison { get; init; }
    public string? LivraisonCodeClient { get; init; }
    public string? LivraisonNomClient { get; init; }
    public string? LivraisonLibelleAdresse { get; init; }
    public string? LivraisonNom { get; init; }
    public string? LivraisonTelephone { get; init; }
    public string? LivraisonAdresse { get; init; }
    public string? LivraisonCodePostal { get; init; }
    public string? LivraisonVille { get; init; }
    public IReadOnlyList<string>? Acces { get; init; }
    public string? AccesAutre { get; init; }
    public string? AgenceRetrait { get; init; }
    public DateOnly? DateFinProduction { get; init; }
    public string? CommentaireExterne { get; init; }
    public string? CommentaireInterne { get; init; }
}

public sealed record CreationDossierDto(SaisieDossierDto Saisie, int ProduitId, string? Environnement);

public sealed record LancerConfigurateurDto(int? ProduitId, string? Environnement);

public sealed record DossierLanceDto(DossierDto Dossier, LancementDto Lancement);

/// <summary>Fin de configuration : nouvelle configuration à confirmer en cas de changement de produit (sinon vide).</summary>
public sealed record DemandeFinConfigurationDto(string? ConfigId, int? ProduitId, string? Environnement);

public enum IssueConfiguration
{
    /// <summary>Configuration terminée : la finalisation est possible.</summary>
    Enregistree,
    /// <summary>Configurateur fermé avant la fin : dossier « Abandonné ».</summary>
    Abandonnee,
    /// <summary>Nouveau produit non enregistré dans le configurateur : le dossier garde son produit et sa configuration.</summary>
    ProduitNonChange,
    /// <summary>Configuration déjà confirmée, ou dossier qui n'est plus en cours : rien à vérifier.</summary>
    Inchangee,
    /// <summary>Le Middleware n'a pas pu répondre : rien n'est abandonné.</summary>
    Indeterminee,
}

public sealed record FinConfigurationDto(DossierDto Dossier, IssueConfiguration Issue);

/// <summary>Ouverture d'une configuration existante par son ConfigId (modification ou consultation).</summary>
public sealed record OuvertureConfigurationDto(TypeDossier Type, ActionConfigurateur Action, string? ConfigId, string? CodeClient, string? Environnement);

public sealed record DossierDto
{
    public int Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public TypeDossier Type { get; init; }
    public OrigineDossier Origine { get; init; }
    public StatutDossier Statut { get; init; }
    public string CodeClient { get; init; } = string.Empty;
    public string NomClient { get; init; } = string.Empty;
    public int? ProduitId { get; init; }
    public string NomProduit { get; init; } = string.Empty;
    public string NomFamille { get; init; } = string.Empty;
    public string? Configurateur { get; init; }
    public string CreePar { get; init; } = string.Empty;
    public DateTime DateCreation { get; init; }
    public DateTime DateModification { get; init; }
    public string? ReferenceChantier { get; init; }
    public string? TicketSalesforce { get; init; }
    public string? ConfigId { get; init; }
    public string? Environnement { get; init; }
    public bool ConfigurationEnregistree { get; init; }
    public DateOnly? DateFinProduction { get; init; }
    public string? CommentaireExterne { get; init; }
    public string? CommentaireInterne { get; init; }
    public string? ModeLivraison { get; init; }
    public string? LivraisonCodeClient { get; init; }
    public string? LivraisonNomClient { get; init; }
    public string? LivraisonLibelleAdresse { get; init; }
    public string? LivraisonNom { get; init; }
    public string? LivraisonTelephone { get; init; }
    public string? LivraisonAdresse { get; init; }
    public string? LivraisonCodePostal { get; init; }
    public string? LivraisonVille { get; init; }
    public IReadOnlyList<string> Acces { get; init; } = [];
    public string? AccesAutre { get; init; }
    public string? AgenceRetrait { get; init; }
    public string? NumeroSap { get; init; }
    public string? DevisSourceReference { get; init; }
    public string? CommandeIssueReference { get; init; }
    public string? ReferenceCommandeClient { get; init; }
    public DateOnly? DateCommande { get; init; }
    public DateOnly? DateLivraisonSouhaitee { get; init; }

    /// <summary>Répertoire d'export ({ConfigId}, contenant les XML du portail et du CPQ : {ConfigId}_portailSAP.xml et {ConfigId}.xml), renseigné dans la réponse à la validation.</summary>
    public string? FichierXml { get; init; }

    public static DossierDto De(Dossier d, string? devisSourceReference = null) => new()
    {
        Id = d.Id,
        Reference = d.Reference,
        Type = d.Type,
        Origine = d.Origine,
        Statut = d.Statut,
        CodeClient = d.CodeClient,
        NomClient = d.NomClient,
        ProduitId = d.ProduitId,
        NomProduit = d.NomProduit,
        NomFamille = d.NomFamille,
        Configurateur = d.Produit?.Configurateur?.Nom,
        CreePar = d.CreePar,
        DateCreation = d.DateCreation,
        DateModification = d.DateModification,
        ReferenceChantier = d.ReferenceChantier,
        TicketSalesforce = d.TicketSalesforce,
        ConfigId = d.ConfigId,
        Environnement = d.Environnement,
        ConfigurationEnregistree = d.ConfigurationEnregistree,
        DateFinProduction = d.DateFinProduction,
        CommentaireExterne = d.CommentaireExterne,
        CommentaireInterne = d.CommentaireInterne,
        ModeLivraison = d.ModeLivraison,
        LivraisonCodeClient = d.LivraisonCodeClient,
        LivraisonNomClient = d.LivraisonNomClient,
        LivraisonLibelleAdresse = d.LivraisonLibelleAdresse,
        LivraisonNom = d.LivraisonNom,
        LivraisonTelephone = d.LivraisonTelephone,
        LivraisonAdresse = d.LivraisonAdresse,
        LivraisonCodePostal = d.LivraisonCodePostal,
        LivraisonVille = d.LivraisonVille,
        Acces = string.IsNullOrEmpty(d.Acces) ? [] : d.Acces.Split(';', StringSplitOptions.RemoveEmptyEntries),
        AccesAutre = d.AccesAutre,
        AgenceRetrait = d.AgenceRetrait,
        NumeroSap = d.NumeroSap,
        DevisSourceReference = devisSourceReference,
        CommandeIssueReference = d.CommandeIssueReference,
        ReferenceCommandeClient = d.ReferenceCommandeClient,
        DateCommande = d.DateCommande,
        DateLivraisonSouhaitee = d.DateLivraisonSouhaitee,
    };
}

public sealed record CommentaireModeleDto(int Id, int FamilleId, int Ordre, string Texte);

public sealed record CommentaireFamilleDto(int Id, string Nom, int Ordre, IReadOnlyList<CommentaireModeleDto> Modeles);

public sealed record MessageDto(int Id, string Audience, string Titre, string Corps, DateTime DateCreation);
