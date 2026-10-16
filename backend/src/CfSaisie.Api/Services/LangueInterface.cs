using CfSaisie.Cpq;
using CfSaisie.Data;
using Microsoft.EntityFrameworkCore;

namespace CfSaisie.Api.Services;

/// <summary>
/// Langue choisie dans le portail (en-tête Accept-Language envoyé par l'écran), vérifiée contre la table Langues :
/// une langue inconnue ou désactivée est ignorée. C'est elle qui est envoyée au configurateur.
/// </summary>
public sealed class LangueInterface(IHttpContextAccessor http, CfSaisieContext db)
{
    /// <summary>Code de la langue demandée s'il est actif (ex. « en »), sinon null.</summary>
    public async Task<string?> CodeAsync(CancellationToken annulation)
    {
        // Le portail envoie un seul code ; on tolère aussi la forme navigateur « en-GB,en;q=0.9 ».
        var entete = http.HttpContext?.Request.Headers.AcceptLanguage.ToString();
        var code = entete?.Split(',')[0].Split(';')[0].Split('-')[0].Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }
        return await db.Langues.AnyAsync(l => l.Code == code && l.Actif, annulation) ? code : null;
    }

    /// <summary>Langue à envoyer au configurateur ; null (langue par défaut du configurateur) si elle n'y est pas gérée, ex. PT.</summary>
    public async Task<LangueConfigurateur?> PourConfigurateurAsync(CancellationToken annulation) =>
        LanceurConfigurateur.Langue(await CodeAsync(annulation));
}
