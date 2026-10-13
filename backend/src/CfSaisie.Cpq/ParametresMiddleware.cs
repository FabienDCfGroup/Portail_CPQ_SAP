namespace CfSaisie.Cpq;

/// <summary>Profil technique utilisé pour appeler le Middleware CPQ.</summary>
/// <param name="ModeErp">
/// Comportement « appel depuis l'ERP » : AccountId toujours envoyé et ApplicationUrl renseignée.
/// Dans l'ancien outil, ce mode était déclenché par le login cfgAxDelBrece@techform.fr.
/// </param>
public sealed record ProfilMiddleware(string Identifiant, string MotDePasse, bool ModeErp)
{
    // Le mot de passe ne doit jamais apparaître dans les journaux.
    public override string ToString() => $"ProfilMiddleware {{ Identifiant = {Identifiant}, ModeErp = {ModeErp} }}";
}

/// <summary>Serveur hébergeant le Middleware CPQ (recette, production ou local).</summary>
public sealed record EnvironnementMiddleware(string Serveur)
{
    public string UrlApplication => $"https://{Serveur}/Cfg_Middleware";
}
