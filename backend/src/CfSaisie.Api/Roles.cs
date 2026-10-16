using System.Security.Claims;

namespace CfSaisie.Api;

/// <summary>
/// Rôles du portail, attribués à la connexion selon les groupes Entra ID (voir Authentification.cs)
/// et portés par la revendication « role » du cookie de session.
/// </summary>
public static class Roles
{
    public const string Utilisateur = "Utilisateur";
    public const string Administrateur = "Administrateur";
    public const string SuperAdministrateur = "SuperAdministrateur";

    /// <summary>Toute personne ayant un rôle du portail.</summary>
    public const string PolitiquePortail = "Portail";

    /// <summary>Écrans d'administration : administrateur ou super administrateur.</summary>
    public const string PolitiqueAdministration = "Administration";

    /// <summary>Gestion de la table Configurateurs : super administrateur seulement.</summary>
    public const string PolitiqueConfigurateurs = "Configurateurs";

    public static string? De(ClaimsPrincipal utilisateur) => utilisateur.FindFirst("role")?.Value;

    /// <summary>Nom affiché (et enregistré comme créateur des dossiers).</summary>
    public static string Nom(ClaimsPrincipal utilisateur) =>
        utilisateur.FindFirst("name")?.Value ?? utilisateur.FindFirst("preferred_username")?.Value ?? "Utilisateur inconnu";

    public static string? Compte(ClaimsPrincipal utilisateur) => utilisateur.FindFirst("preferred_username")?.Value;
}
