using SmartDataExchangeDefines.Models;

namespace CfSaisie.Cpq;

public sealed class DemandeLancementInvalideException(string message) : Exception(message);

/// <summary>
/// Construit la requête de lancement d'un configurateur envoyée au Middleware CPQ.
/// Reprend les règles de l'ancien outil « Lancement configurateur » :
/// Configurateur.CreeRequete et les paramètres complémentaires de PanneauPrincipal.ExecuterConfigurateur.
/// </summary>
public sealed class OpenRequestBuilder(TimeProvider horloge)
{
    public const string ProductionSiteParDefaut = "DEL_BRECE";

    // Détourne ProjectRef pour faire afficher le bouton « Présenter » dans le configurateur.
    public const string ProjectRefParDefaut = "AfficherBoutonPresenter";

    public OpenRequest Construire(DemandeLancement demande, ProfilMiddleware profil, EnvironnementMiddleware environnement)
    {
        ArgumentNullException.ThrowIfNull(demande);
        ArgumentNullException.ThrowIfNull(profil);
        ArgumentNullException.ThrowIfNull(environnement);
        Valider(demande);

        var codeClient = demande.CodeClient?.Trim();

        var requete = new OpenRequest
        {
            Login = profil.Identifiant,
            Password = profil.MotDePasse,
            TypeAppel = ConvertirAction(demande.Action),
            TypeTransaction = demande.TypeDocument == TypeDocument.Commande ? EnuTypetransaction.Order : EnuTypetransaction.Quote,
            // Équivalent de DateTime.Now : heure locale du serveur.
            DateValidite = horloge.GetUtcNow().LocalDateTime,
            ConfiguratorName = demande.Configurateur.Nom,
            ProductionSite = string.IsNullOrWhiteSpace(demande.Configurateur.ProductionSite)
                ? ProductionSiteParDefaut
                : demande.Configurateur.ProductionSite,
            Language = ConvertirLangue(demande.Langue),
            ProjectRef = ProjectRefParDefaut,
        };

        if (demande.Action == ActionConfigurateur.Creation)
        {
            requete.AccountId = codeClient;
        }
        else
        {
            requete.ConfigId = demande.ConfigId;
        }

        if (profil.ModeErp)
        {
            requete.AccountId = codeClient;
            requete.ApplicationUrl = environnement.UrlApplication;
        }

        // Paramètres complémentaires (grille « Paramètres » de l'ancien outil)
        if (demande.Email is not null)
        {
            requete.Email = demande.Email;
        }
        if (demande.ReferenceChantier is not null)
        {
            requete.BusinessCode = demande.ReferenceChantier;
        }
        if (!string.IsNullOrEmpty(demande.LangueNotice))
        {
            requete.ClientLangueNotice = demande.LangueNotice;
        }

        return requete;
    }

    private static void Valider(DemandeLancement demande)
    {
        if (string.IsNullOrWhiteSpace(demande.Configurateur?.Nom))
        {
            throw new DemandeLancementInvalideException("Le configurateur est obligatoire.");
        }

        if (demande.Action != ActionConfigurateur.Creation && string.IsNullOrWhiteSpace(demande.ConfigId))
        {
            throw new DemandeLancementInvalideException("Le ConfigId est obligatoire en modification et en consultation.");
        }
    }

    private static EnuAction ConvertirAction(ActionConfigurateur action) => action switch
    {
        ActionConfigurateur.Creation => EnuAction.Creation,
        ActionConfigurateur.Modification => EnuAction.Modification,
        ActionConfigurateur.Consultation => EnuAction.Consultation,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    // Casse reprise telle quelle de l'ancien outil : "FR" en majuscules, les autres en minuscules.
    private static string ConvertirLangue(LangueConfigurateur? langue) => langue switch
    {
        LangueConfigurateur.Francais => "FR",
        LangueConfigurateur.Anglais => "en",
        LangueConfigurateur.Allemand => "de",
        LangueConfigurateur.Neerlandais => "nl",
        _ => string.Empty,
    };
}
