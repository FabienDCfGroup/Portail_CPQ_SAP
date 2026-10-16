using CfSaisie.Cpq;
using CfSaisie.Data;
using CfSaisie.Sap;
using Microsoft.EntityFrameworkCore;

namespace CfSaisie.Api.Services;

public enum ModePlanning
{
    /// <summary>Le configurateur a écrit un code CCX : dates proposées par l'API Planning CCX.</summary>
    Calendrier,

    /// <summary>Pas de code CCX (ou pas de planning pour ce code) : date de fin de production saisie librement.</summary>
    Libre,

    /// <summary>Le XML n'existe pas encore : la configuration n'a pas été enregistrée dans le configurateur.</summary>
    ConfigurationNonEnregistree,
}

public sealed record JourPlanningDto(DateOnly Date, string Etat, string Libelle, bool Selectionnable, bool ConfirmationRequise, int? PlacesRestantes, string? Motif);

public sealed record PlanningDossierDto(
    ModePlanning Mode,
    string? Message,
    string? CodeCcx,
    string? LibelleCcx,
    string? Site,
    int? DelaiMiniJoursOuvres,
    DateOnly? PremiereDateAutorisee,
    bool ChargeIncomplete,
    IReadOnlyList<string> Origines,
    IReadOnlyList<JourPlanningDto> Jours)
{
    public static PlanningDossierDto Sans(ModePlanning mode, string? message) => new(mode, message, null, null, null, null, null, false, [], []);
}

/// <summary>
/// Planning de fin de production d'un dossier : le code CCX est lu dans le XML de fin de configuration
/// (calculé par le configurateur), puis les disponibilités de ce code sont demandées à l'API Planning CCX.
/// </summary>
public sealed class ServicePlanning(
    CfSaisieContext db,
    LanceurConfigurateur lanceur,
    IPlanningCcxService planningCcx,
    ParametresPlanningCcx parametres)
{
    public async Task<PlanningDossierDto> PlanningAsync(int dossierId, CancellationToken annulation)
    {
        var dossier = await db.Dossiers.Include(d => d.Produit).ThenInclude(p => p!.Configurateur).AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dossierId, annulation)
            ?? throw new DossierIntrouvableException(dossierId);
        return await CalculerAsync(dossier, annulation);
    }

    public async Task<PlanningDossierDto> CalculerAsync(Dossier dossier, CancellationToken annulation)
    {
        if (string.IsNullOrEmpty(dossier.ConfigId) || dossier.Produit?.Configurateur is not { } configurateur)
        {
            return PlanningDossierDto.Sans(ModePlanning.ConfigurationNonEnregistree, "Lancez le configurateur et enregistrez la configuration.");
        }

        var xml = await lanceur.LireXmlAsync(dossier, configurateur, lanceur.Resoudre(dossier.Environnement), annulation);
        if (!xml.Succes || xml.Xml is null)
        {
            // « No config found for this Id Config » : rien d'enregistré pour ce ConfigId. Toute autre réponse
            // (ex. « … Id Site » : configuration trouvée mais XML introuvable pour le site) est une anomalie du Middleware.
            if (xml.Message is null || xml.Message.Contains("Id Config", StringComparison.OrdinalIgnoreCase))
            {
                return PlanningDossierDto.Sans(ModePlanning.ConfigurationNonEnregistree,
                    $"La configuration {dossier.ConfigId} n'est pas encore enregistrée dans le configurateur : enregistrez-la, puis réessayez.");
            }
            throw new MiddlewareCpqException(
                $"Le Middleware ne renvoie pas le XML de la configuration {dossier.ConfigId} (réponse : « {xml.Message} ») : planning de fabrication indisponible.");
        }

        XmlConfigurationCpq configuration;
        try
        {
            configuration = XmlConfigurationCpq.Lire(xml.Xml);
        }
        catch (FormatException ex)
        {
            throw new MiddlewareCpqException($"XML de la configuration {dossier.ConfigId} illisible : {ex.Message}", ex);
        }

        if (configuration.CodeCcx is null)
        {
            return PlanningDossierDto.Sans(ModePlanning.Libre, null);
        }

        var site = configuration.ProductionSite
            ?? (string.IsNullOrWhiteSpace(configurateur.ProductionSite) ? OpenRequestBuilder.ProductionSiteParDefaut : configurateur.ProductionSite);
        var calendrier = CalendrierCcx.Construire(await planningCcx.LireAsync(site, annulation), configuration.CodeCcx, site, parametres.SeuilPlacesLimitees);
        if (calendrier is null)
        {
            return PlanningDossierDto.Sans(ModePlanning.Libre,
                $"Pas de planning de fabrication pour la ligne {configuration.CodeCcx} sur le site {site} : saisissez la date de fin de production.")
                with { CodeCcx = configuration.CodeCcx };
        }

        return new PlanningDossierDto(
            ModePlanning.Calendrier,
            null,
            calendrier.CodeCcx,
            calendrier.LibelleCcx,
            calendrier.Site,
            calendrier.DelaiMiniJoursOuvres,
            calendrier.PremiereDateAutorisee,
            calendrier.ChargeIncomplete,
            calendrier.Origines,
            calendrier.Jours.Select(j => new JourPlanningDto(j.Date, j.Etat.ToString(), j.Libelle, j.Selectionnable, j.ConfirmationRequise, j.PlacesRestantes, j.Motif)).ToList());
    }

    /// <summary>
    /// Contrôle à la validation : la charge a pu évoluer depuis le choix de la date, elle est donc revérifiée.
    /// </summary>
    /// <returns>Code CCX de la configuration (attribut ccx du XML du configurateur), null s'il n'y en a pas.</returns>
    public async Task<string?> VerifierDateAsync(Dossier dossier, DateOnly date, CancellationToken annulation)
    {
        var planning = await CalculerAsync(dossier, annulation);
        switch (planning.Mode)
        {
            case ModePlanning.ConfigurationNonEnregistree:
                throw new RequeteInvalideException(planning.Message ?? "Enregistrez la configuration avant de valider.");
            case ModePlanning.Calendrier when planning.Jours.FirstOrDefault(j => j.Date == date) is not { Selectionnable: true }:
                throw new RequeteInvalideException(
                    $"La date de fin de production du {date:dd/MM/yyyy} n'est plus disponible pour la ligne {planning.CodeCcx} : choisissez une autre date.");
        }
        return planning.CodeCcx;
    }
}
