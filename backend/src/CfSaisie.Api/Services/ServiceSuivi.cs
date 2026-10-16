using CfSaisie.Data;
using Microsoft.EntityFrameworkCore;

namespace CfSaisie.Api.Services;

/// <summary>Ligne du suivi : dossier du portail, complété par la commande ERP correspondante quand l'ERP l'a transmise.</summary>
public sealed record SuiviDossierDto(
    int Id,
    string Reference,
    TypeDossier Type,
    OrigineDossier Origine,
    StatutDossier Statut,
    DateTime DateCreation,
    string CreePar,
    string CodeClient,
    string NomClient,
    string? ReferenceClient,
    string? NumeroSap,
    string NomProduit,
    string NomFamille,
    string? ConfigId,
    DateOnly? DateFinProductionSaisie,
    CommandeErpDto? Erp)
{
    /// <summary>Commande : devis dont elle est issue.</summary>
    public string? DevisSourceReference { get; init; }

    /// <summary>Devis : commande validée qui en est issue.</summary>
    public string? CommandeIssueReference { get; init; }

    /// <summary>Devis : commande en cours de transformation (pas encore validée).</summary>
    public string? CommandeEnCoursReference { get; init; }
}

/// <summary>Données de erp.Commandes (alimentées par le flux ERP, lues seulement).</summary>
public sealed record CommandeErpDto(
    string NumeroCommande,
    string StatutErp,
    DateOnly? DebutProduction,
    DateOnly? FinProduction,
    DateOnly? LivraisonPrevue,
    DateOnly? LivraisonReelle);

/// <summary>
/// Suivi des commandes et devis : la source est dbo.Dossiers (alimentée à la création et à la validation) ;
/// les informations ERP viennent de erp.Commandes, rapprochées par numéro SAP, sinon par ConfigId.
/// Le portail n'écrit jamais dans le schéma erp.
/// </summary>
public sealed class ServiceSuivi(CfSaisieContext db)
{
    // Garde-fou d'affichage : le suivi montre les dossiers les plus récents.
    private const int Maximum = 1000;

    public async Task<IReadOnlyList<SuiviDossierDto>> ListerAsync(CancellationToken annulation)
    {
        var dossiers = await db.Dossiers.AsNoTracking()
            .OrderByDescending(d => d.DateCreation)
            .Take(Maximum)
            .ToListAsync(annulation);

        var numeros = dossiers.Where(d => d.NumeroSap != null).Select(d => d.NumeroSap!).Distinct().ToList();
        var configIds = dossiers.Where(d => d.ConfigId != null).Select(d => d.ConfigId!).Distinct().ToList();
        var commandes = numeros.Count == 0 && configIds.Count == 0
            ? []
            : await db.CommandesErp.AsNoTracking()
                .Where(c => numeros.Contains(c.NumeroCommande) || (c.ConfigId != null && configIds.Contains(c.ConfigId)))
                .ToListAsync(annulation);
        var parNumero = commandes.ToDictionary(c => c.NumeroCommande, StringComparer.OrdinalIgnoreCase);
        // Plusieurs commandes ERP peuvent porter le même ConfigId : la plus récente fait foi.
        var parConfigId = commandes.Where(c => c.ConfigId != null)
            .GroupBy(c => c.ConfigId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.DateCreation).First(), StringComparer.OrdinalIgnoreCase);

        // Liens devis ↔ commande (DevisSourceId de la commande).
        var references = dossiers.ToDictionary(d => d.Id, d => d.Reference);
        var issues = dossiers.Where(d => d.DevisSourceId != null).ToLookup(d => d.DevisSourceId!.Value);

        return dossiers.Select(d =>
        {
            var commandesIssues = issues[d.Id].ToList();
            // Un devis transformé partage son ConfigId avec sa commande : les données ERP reviennent à la commande seule.
            var erp = d.NumeroSap is { } n && parNumero.TryGetValue(n, out var parN) ? parN
                : commandesIssues.Count == 0 && d.ConfigId is { } c && parConfigId.TryGetValue(c, out var parC) ? parC
                : null;
            return new SuiviDossierDto(
                d.Id, d.Reference, d.Type, d.Origine, d.Statut, d.DateCreation, d.CreePar, d.CodeClient, d.NomClient,
                d.ReferenceCommandeClient ?? d.ReferenceChantier, d.NumeroSap ?? erp?.NumeroCommande, d.NomProduit, d.NomFamille,
                d.ConfigId, d.DateFinProduction,
                erp is null ? null : new CommandeErpDto(erp.NumeroCommande, erp.StatutErp, erp.DateDebutProduction, erp.DateFinProduction,
                    erp.DateLivraisonPrevue, erp.DateLivraisonReelle))
            {
                DevisSourceReference = d.DevisSourceId is { } s ? references.GetValueOrDefault(s) : null,
                CommandeIssueReference = d.CommandeIssueReference,
                CommandeEnCoursReference = commandesIssues.FirstOrDefault(x => x.Statut == StatutDossier.EnCours)?.Reference,
            };
        }).ToList();
    }
}
