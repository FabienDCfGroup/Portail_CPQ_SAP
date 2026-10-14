using System.Net;
using System.Text.Json;

namespace CfSaisie.Sap;

/// <summary>
/// Appel de l'API Planning CCX : GET {Url}?site=…&amp;nbJours=…, en-tête X-API-Key.
/// Gère les pièges du contrat : 200 sans corps (échec côté n8n), 5xx réessayé une fois, pas de cache.
/// </summary>
public sealed class PlanningCcxClient(HttpClient http, ParametresPlanningCcx parametres) : IPlanningCcxService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<ReponsePlanningCcx> LireAsync(string site, CancellationToken annulation = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(site);
        var url = $"{parametres.Url}?site={Uri.EscapeDataString(site)}&nbJours={parametres.NbJours}"
            + (string.IsNullOrWhiteSpace(parametres.Societe) ? string.Empty : $"&societe={Uri.EscapeDataString(parametres.Societe)}");

        var (statut, corps) = await AppelerAsync(url, annulation);
        if (statut >= HttpStatusCode.InternalServerError)
        {
            (statut, corps) = await AppelerAsync(url, annulation);
        }

        if (statut == HttpStatusCode.Forbidden)
        {
            throw new PlanningCcxException("API Planning CCX : clé refusée (403). Vérifiez PlanningCcx:CleApi.");
        }
        if ((int)statut is < 200 or > 299)
        {
            throw new PlanningCcxException($"API Planning CCX : HTTP {(int)statut}.");
        }

        ReponsePlanningCcx? reponse;
        try
        {
            reponse = string.IsNullOrWhiteSpace(corps) ? null : JsonSerializer.Deserialize<ReponsePlanningCcx>(corps, Json);
        }
        catch (JsonException ex)
        {
            throw new PlanningCcxException("API Planning CCX : réponse illisible.", ex);
        }

        // Un 200 sans « jours » est un échec du flux n8n, pas un calendrier vide.
        return reponse is { Jours: not null }
            ? reponse
            : throw new PlanningCcxException("API Planning CCX : réponse vide (échec côté serveur).");
    }

    private async Task<(HttpStatusCode Statut, string Corps)> AppelerAsync(string url, CancellationToken annulation)
    {
        using var requete = new HttpRequestMessage(HttpMethod.Get, url);
        requete.Headers.Add("X-API-Key", parametres.CleApi);
        requete.Headers.Accept.ParseAdd("application/json");
        try
        {
            using var reponse = await http.SendAsync(requete, annulation);
            return (reponse.StatusCode, await reponse.Content.ReadAsStringAsync(annulation));
        }
        catch (HttpRequestException ex)
        {
            throw new PlanningCcxException("API Planning CCX injoignable.", ex);
        }
        catch (TaskCanceledException ex) when (!annulation.IsCancellationRequested)
        {
            throw new PlanningCcxException("API Planning CCX : délai de réponse dépassé.", ex);
        }
    }
}

/// <summary>
/// API Planning CCX simulée (développement, tant que la clé n'est pas renseignée), au format du contrat :
/// codes L, LC, LGV, LGVP sur DEL_BRECE, week-ends fermés, délai minimum en jours ouvrés, charge pseudo-aléatoire stable.
/// </summary>
public sealed class PlanningCcxSimule(ParametresPlanningCcx parametres, TimeProvider horloge) : IPlanningCcxService
{
    private static readonly (string Code, string Libelle, int Delai, int Capacite)[] Codes =
    [
        ("L", "L. SIMP", 3, 8),
        ("LC", "L. COM", 8, 4),
        ("LGV", "L. GV", 6, 5),
        ("LGVP", "L. GV+", 6, 5),
    ];

    public Task<ReponsePlanningCcx> LireAsync(string site, CancellationToken annulation = default)
    {
        var aujourdhui = DateOnly.FromDateTime(horloge.GetLocalNow().DateTime);
        var jours = new List<JourCcx>();
        // Comme l'API réelle : un seul site paramétré ; ailleurs, aucun jour.
        if (string.Equals(site, "DEL_BRECE", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var (code, libelle, delai, capaciteJour) in Codes)
            {
                var premiere = AjouterJoursOuvres(aujourdhui, delai);
                for (var n = 0; n < parametres.NbJours; n++)
                {
                    var jour = aujourdhui.AddDays(n);
                    var ferme = jour.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                    var capacite = ferme ? 0 : capaciteJour;
                    var charge = ferme ? 0 : (jour.DayNumber * 7 + code.Length * 3) % (capaciteJour + 3);
                    var restante = capacite - charge;
                    var motif = ferme ? "Atelier ferme" : jour < premiere ? "Avant le delai mini du code" : restante <= 0 ? "Complet" : null;
                    jours.Add(new JourCcx(code, libelle, jour, capacite, charge, restante, motif is null, motif, n == 0 && motif is null, delai, premiere));
                }
            }
        }
        return Task.FromResult(new ReponsePlanningCcx(horloge.GetUtcNow().UtcDateTime, site, ["AX"], [], jours));
    }

    private static DateOnly AjouterJoursOuvres(DateOnly depart, int jours)
    {
        var date = depart;
        while (jours > 0)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) jours--;
        }
        return date;
    }
}
