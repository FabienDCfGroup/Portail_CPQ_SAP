using CfSaisie.Cpq;
using CfSaisie.Data;
using Microsoft.Extensions.Options;

namespace CfSaisie.Api.Services;

public sealed class EnvironnementInconnuException(string code) : Exception($"Environnement inconnu : « {code} ».");

/// <summary>Lance un configurateur pour un dossier via le Middleware CPQ.</summary>
public sealed class LanceurConfigurateur(
    OpenRequestBuilder requetes,
    IMiddlewareCpq middleware,
    IOptions<OptionsMiddleware> options,
    ILogger<LanceurConfigurateur> journal)
{
    public string Resoudre(string? environnement)
    {
        var code = string.IsNullOrWhiteSpace(environnement) ? options.Value.EnvironnementParDefaut : environnement;
        if (!options.Value.Environnements.ContainsKey(code))
        {
            throw new EnvironnementInconnuException(code);
        }
        return code.ToLowerInvariant();
    }

    public Task<ResultatLancement> LancerAsync(
        Dossier dossier,
        Configurateur configurateur,
        ActionConfigurateur action,
        string environnement,
        LangueConfigurateur? langue,
        CancellationToken annulation)
    {
        var demande = new DemandeLancement
        {
            Action = action,
            TypeDocument = dossier.Type == TypeDossier.Commande ? TypeDocument.Commande : TypeDocument.Devis,
            Configurateur = new ConfigurateurCpq(configurateur.Nom, configurateur.ProductionSite),
            CodeClient = dossier.CodeClient,
            ConfigId = action == ActionConfigurateur.Creation ? null : dossier.ConfigId,
            Langue = langue,
            ReferenceChantier = string.IsNullOrWhiteSpace(dossier.ReferenceChantier) ? null : dossier.ReferenceChantier,
        };
        return LancerAsync(demande, environnement, dossier.Reference, annulation);
    }

    /// <param name="origine">Ce qui est lancé (référence du dossier ou ConfigId), pour le journal.</param>
    public async Task<ResultatLancement> LancerAsync(DemandeLancement demande, string environnement, string origine, CancellationToken annulation)
    {
        var o = options.Value;
        var env = new EnvironnementMiddleware(o.Environnements[environnement].Serveur);
        var profil = o.ProfilDe(environnement);
        var requete = requetes.Construire(
            demande,
            new ProfilMiddleware(profil.Identifiant, profil.MotDePasse ?? string.Empty, profil.ModeErp),
            env);

        journal.LogInformation("Lancement {Configurateur} ({Action}, {Type}) pour {Origine} sur {Serveur}",
            demande.Configurateur.Nom, demande.Action, demande.TypeDocument, origine, env.Serveur);

        var resultat = await middleware.OuvrirAsync(requete, env, annulation);

        journal.LogInformation("Réponse Middleware pour {Origine} : succès={Succes}, configId={ConfigId}, message={Message}",
            origine, resultat.Succes, resultat.ConfigId, resultat.Message);
        return resultat;
    }

    /// <summary>Langue du client SAP (FR, EN, DE, NL…) convertie pour le configurateur ; vide si non gérée (ex. PT).</summary>
    public static LangueConfigurateur? Langue(string? codeLangue) => codeLangue?.ToUpperInvariant() switch
    {
        "FR" => LangueConfigurateur.Francais,
        "EN" => LangueConfigurateur.Anglais,
        "DE" => LangueConfigurateur.Allemand,
        "NL" => LangueConfigurateur.Neerlandais,
        _ => null,
    };
}
