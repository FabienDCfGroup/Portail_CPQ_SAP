using System.Globalization;
using System.Text;

namespace CfSaisie.Sap;

// API Planning CCX (n8n) : disponibilités de fabrication, jour par jour, par ligne de fabrication (code CCX).
// Contrat : « API Planning CCX — contrat d'interface » (équipe n8n). Le code CCX vient du XML de fin de configuration.

/// <summary>Paramètres de l'API Planning CCX (section « PlanningCcx » de la configuration).</summary>
public sealed class ParametresPlanningCcx
{
    public const string Section = "PlanningCcx";

    /// <summary>Ex. https://test.n8n.fr.cf.group/webhook/planning-ccx (instance de test).</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>En-tête X-API-Key. Secret : user-secrets en local, secret du conteneur ensuite. Vide = API simulée (développement).</summary>
    public string? CleApi { get; set; }

    /// <summary>Profondeur du calendrier en jours calendaires (paramètre nbJours).</summary>
    public int NbJours { get; set; } = 90;

    /// <summary>Code société ; à ne passer qu'en cas de besoin explicite.</summary>
    public string? Societe { get; set; }

    /// <summary>Jusqu'à ce nombre de places restantes, le jour est affiché « places limitées ».</summary>
    public int SeuilPlacesLimitees { get; set; } = 2;

    public bool Simulation => string.IsNullOrWhiteSpace(CleApi);
}

/// <summary>Réponse de l'API (champs utilisés par le portail).</summary>
public sealed record ReponsePlanningCcx(
    DateTime GenereLe,
    string? Site,
    IReadOnlyList<string>? Origines,
    IReadOnlyList<AnomaliePlanningCcx>? Anomalies,
    IReadOnlyList<JourCcx>? Jours);

public sealed record AnomaliePlanningCcx(string? Type, IReadOnlyList<string>? Detail);

/// <param name="Jour">Date de fin de production (AAAA-MM-JJ, sans fuseau).</param>
/// <param name="Selectionnable">Seul champ qui autorise ou non la sélection : ne jamais le recalculer.</param>
public sealed record JourCcx(
    string CodeCCX,
    string? LibelleCCX,
    DateOnly Jour,
    decimal Capacite,
    decimal Charge,
    decimal CapaciteRestante,
    bool Selectionnable,
    string? MotifIndisponible,
    bool ConfirmationRequise,
    int DelaiMiniJoursOuvres,
    DateOnly? PremiereDateAutorisee);

public sealed class PlanningCcxException(string message, Exception? innerException = null) : Exception(message, innerException);

public interface IPlanningCcxService
{
    /// <summary>Disponibilités de tous les codes CCX d'un site, sur l'horizon paramétré. Pas de cache : la charge évolue en continu.</summary>
    Task<ReponsePlanningCcx> LireAsync(string site, CancellationToken annulation = default);
}

public enum EtatJourProduction
{
    Disponible,
    Limite,
    Complet,
    AvantDelai,
}

/// <param name="PlacesRestantes">Nombre de places affichable (jamais négatif) ; null quand il ne faut pas en afficher.</param>
public sealed record JourProduction(
    DateOnly Date,
    EtatJourProduction Etat,
    string Libelle,
    bool Selectionnable,
    bool ConfirmationRequise,
    int? PlacesRestantes,
    string? Motif);

/// <param name="ChargeIncomplete">Vrai tant que SAP ne fait pas partie des origines : la charge affichée peut être sous-évaluée.</param>
public sealed record CalendrierProduction(
    string CodeCcx,
    string? LibelleCcx,
    string Site,
    int DelaiMiniJoursOuvres,
    DateOnly? PremiereDateAutorisee,
    IReadOnlyList<string> Origines,
    bool ChargeIncomplete,
    int NombreAnomalies,
    IReadOnlyList<JourProduction> Jours);

/// <summary>Transforme la réponse de l'API en calendrier d'un code CCX, selon les règles d'affichage du contrat.</summary>
public static class CalendrierCcx
{
    /// <returns>Null si le code CCX n'a aucun jour sur le site (pas de planning : date libre).</returns>
    public static CalendrierProduction? Construire(ReponsePlanningCcx reponse, string codeCcx, string site, int seuilPlacesLimitees)
    {
        ArgumentNullException.ThrowIfNull(reponse);
        var jours = (reponse.Jours ?? [])
            .Where(j => string.Equals(j.CodeCCX, codeCcx, StringComparison.OrdinalIgnoreCase))
            .OrderBy(j => j.Jour)
            .ToList();
        if (jours.Count == 0)
        {
            return null;
        }

        var origines = reponse.Origines ?? [];
        var premier = jours[0];
        return new CalendrierProduction(
            premier.CodeCCX,
            premier.LibelleCCX,
            reponse.Site ?? site,
            premier.DelaiMiniJoursOuvres,
            premier.PremiereDateAutorisee,
            origines,
            !origines.Any(o => string.Equals(o, "SAP", StringComparison.OrdinalIgnoreCase)),
            reponse.Anomalies?.Count ?? 0,
            jours.Where(j => !EstFerme(j)).Select(j => Classer(j, seuilPlacesLimitees)).ToList());
    }

    /// <summary>Un jour fermé (capacité nulle : week-end, férié) n'est pas affiché.</summary>
    public static bool EstFerme(JourCcx jour) =>
        jour.Capacite <= 0 || Normaliser(jour.MotifIndisponible).Contains("ferme");

    public static JourProduction Classer(JourCcx jour, int seuilPlacesLimitees)
    {
        if (jour.Selectionnable)
        {
            // La capacité restante peut être négative (l'ERP tolère les dépassements) : ne jamais l'afficher telle quelle.
            var places = (int)Math.Floor(jour.CapaciteRestante);
            return places > seuilPlacesLimitees
                ? new JourProduction(jour.Jour, EtatJourProduction.Disponible, "Disponible", true, jour.ConfirmationRequise, places, null)
                : places >= 1
                    ? new JourProduction(jour.Jour, EtatJourProduction.Limite, places == 1 ? "1 place" : $"{places} places", true, jour.ConfirmationRequise, places, null)
                    : new JourProduction(jour.Jour, EtatJourProduction.Limite, "Places limitées", true, jour.ConfirmationRequise, null, null);
        }

        var motif = Normaliser(jour.MotifIndisponible);
        if (motif.Contains("delai"))
        {
            var auPlusTot = jour.PremiereDateAutorisee is { } d ? $"Au plus tôt le {d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}" : jour.MotifIndisponible;
            return new JourProduction(jour.Jour, EtatJourProduction.AvantDelai, "Trop tôt", false, false, null, auPlusTot);
        }
        return new JourProduction(jour.Jour, EtatJourProduction.Complet, "Complet", false, false, null, jour.MotifIndisponible);
    }

    // Les motifs sont des libellés ERP (« Atelier ferme », « Avant le delai mini du code ») : comparaison sans accents ni casse.
    private static string Normaliser(string? texte)
    {
        if (string.IsNullOrEmpty(texte)) return string.Empty;
        var decompose = texte.Normalize(NormalizationForm.FormD);
        var sansAccents = new StringBuilder(decompose.Length);
        foreach (var c in decompose.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
        {
            sansAccents.Append(char.ToLowerInvariant(c));
        }
        return sansAccents.ToString();
    }
}
