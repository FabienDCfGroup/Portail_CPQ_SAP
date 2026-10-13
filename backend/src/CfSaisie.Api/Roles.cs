namespace CfSaisie.Api;

/// <summary>
/// Rôles simulés, transmis par l'écran dans l'en-tête X-Role en attendant l'authentification réelle.
/// Le super administrateur n'existe que sur le poste de développement (mode Development) : il gère la table Configurateurs.
/// </summary>
public static class Roles
{
    public const string Utilisateur = "Utilisateur";
    public const string Administrateur = "Administrateur";
    public const string SuperAdministrateur = "SuperAdministrateur";

    public static string Courant(HttpContext contexte) => contexte.Request.Headers["X-Role"].ToString();

    public static bool SuperAdministrateurDisponible(IHostEnvironment environnement) => environnement.IsDevelopment();

    public static bool EstSuperAdministrateur(HttpContext contexte) =>
        Courant(contexte) == SuperAdministrateur
        && SuperAdministrateurDisponible(contexte.RequestServices.GetRequiredService<IHostEnvironment>());

    public static bool EstAdministrateur(HttpContext contexte) =>
        Courant(contexte) == Administrateur || EstSuperAdministrateur(contexte);
}
