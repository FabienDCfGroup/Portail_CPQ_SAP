using System.Security.Claims;
using CfSaisie.Api.Services;
using CfSaisie.Data;
using CfSaisie.Sap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CfSaisie.Api.Endpoints;

/// <summary>Routes de l'écran de création, du suivi des commandes et de l'ouverture des configurations existantes.</summary>
public static class PortailEndpoints
{
    public static void MapPortail(this IEndpointRouteBuilder app)
    {
        // Toute personne connectée avec un rôle du portail (groupes Entra ID).
        var api = app.MapGroup("/api").RequireAuthorization(Roles.PolitiquePortail);

        api.MapGet("/session", async (ClaimsPrincipal utilisateur, IOptions<OptionsMiddleware> middleware, IOptions<OptionsPortail> portail, CfSaisieContext db, CancellationToken ct) =>
        {
            var langues = await db.Langues.Where(l => l.Actif).OrderBy(l => l.Ordre).Select(l => new LangueDto(l.Code, l.Libelle)).ToListAsync(ct);
            // Français par défaut s'il est actif, sinon la première langue active.
            var langueParDefaut = langues.Any(l => l.Code == "fr") ? "fr" : langues.FirstOrDefault()?.Code ?? "fr";
            var nom = Roles.Nom(utilisateur);
            var initiales = string.Concat(nom.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(m => char.ToUpperInvariant(m[0]))).ToString();
            var environnements = middleware.Value.Environnements
                .Select(e => new EnvironnementDto(e.Key.ToLowerInvariant(), e.Value.Libelle))
                .ToList();
            return new SessionDto(nom, initiales, Roles.Compte(utilisateur), Roles.De(utilisateur)!, portail.Value.Societe, environnements,
                middleware.Value.EnvironnementParDefaut, langues, langueParDefaut);
        });

        api.MapGet("/messages/tableau-bord", async (string? client, CfSaisieContext db, CancellationToken ct) =>
            await db.Messages
                .Where(m => m.Audience == "Tous" || (client != null && m.Audience == client))
                .OrderByDescending(m => m.DateCreation)
                .Select(m => new MessageDto(m.Id, m.Audience, m.Titre, m.Corps, m.DateCreation))
                .ToListAsync(ct));

        api.MapGet("/clients", async (string? q, ISapClientService sap, CancellationToken ct) =>
            (await sap.RechercherAsync(q, 20, ct)).Select(ClientDto.De));

        api.MapGet("/clients/{code}", async (string code, ISapClientService sap, CancellationToken ct) =>
            await sap.LireAsync(code, ct) is { } client
                ? Results.Ok(new ClientDetailDto(ClientDto.De(client), await sap.AdressesAsync(code, ct)))
                : Results.NotFound());

        // Familles actives et produits autorisés pour le client (accréditations).
        api.MapGet("/clients/{code}/catalogue", async (string code, CfSaisieContext db, CancellationToken ct) =>
        {
            var produits = await db.Produits
                .Include(p => p.Famille)
                .Include(p => p.Configurateur)
                .Where(p => p.Famille != null && p.Famille.Actif && p.Configurateur.Actif
                    && db.Accreditations.Any(a => a.CodeClient == code && a.ProduitId == p.Id))
                .ToListAsync(ct);
            return produits
                .GroupBy(p => p.Famille!)
                .OrderBy(g => g.Key.Id)
                .Select(g => new FamilleCatalogueDto(g.Key.Id, g.Key.Nom, g.Key.Image,
                    g.OrderBy(p => p.Id).Select(p => new ProduitCatalogueDto(p.Id, p.Nom, p.Description, p.Image, p.Configurateur.Nom)).ToList()))
                .ToList();
        });

        api.MapGet("/agences", async (ISapLogistiqueService sap, CancellationToken ct) => await sap.AgencesAsync(ct));

        api.MapGet("/commentaires", async (CfSaisieContext db, CancellationToken ct) =>
            (await db.CommentaireFamilles.Include(f => f.Modeles).AsNoTracking().ToListAsync(ct))
                .OrderBy(f => f.Ordre).ThenBy(f => f.Nom)
                .Select(f => new CommentaireFamilleDto(f.Id, f.Nom, f.Ordre,
                    f.Modeles.OrderBy(m => m.Ordre).ThenBy(m => m.Texte).Select(m => new CommentaireModeleDto(m.Id, m.FamilleId, m.Ordre, m.Texte)).ToList())));

        // Dossiers (commandes et devis)
        var dossiers = api.MapGroup("/dossiers");

        // Suivi des commandes et devis (dossiers du portail + données de erp.Commandes).
        dossiers.MapGet("", (ServiceSuivi service, CancellationToken ct) => service.ListerAsync(ct));

        dossiers.MapPost("/{id:int}/consulter", (int id, ServiceDossiers service, CancellationToken ct) =>
            service.ConsulterAsync(id, ct));

        // Suivi : un devis transmis devient une commande sur la même configuration (rouverte ensuite en commande).
        dossiers.MapPost("/{id:int}/transformer-en-commande", (int id, ServiceDossiers service, ClaimsPrincipal utilisateur, CancellationToken ct) =>
            service.TransformerEnCommandeAsync(id, Roles.Nom(utilisateur), ct));

        dossiers.MapGet("/{id:int}",(int id, ServiceDossiers service, CancellationToken ct) => service.LireAsync(id, ct));

        dossiers.MapPost("", (CreationDossierDto demande, ServiceDossiers service, ClaimsPrincipal utilisateur, CancellationToken ct) =>
            service.CreerAsync(demande, Roles.Nom(utilisateur), ct));

        dossiers.MapPut("/{id:int}", (int id, SaisieDossierDto saisie, ServiceDossiers service, CancellationToken ct) =>
            service.MettreAJourAsync(id, saisie, ct));

        dossiers.MapPost("/{id:int}/configurateur", (int id, LancerConfigurateurDto demande, ServiceDossiers service, CancellationToken ct) =>
            service.RelancerAsync(id, demande, ct));

        // Planning de fin de production : calendrier si le XML de fin de configuration contient un code CCX, date libre sinon.
        dossiers.MapGet("/{id:int}/planning", (int id, ServicePlanning service, CancellationToken ct) =>
            service.PlanningAsync(id, ct));

        // Fenêtre du configurateur fermée : configuration terminée, ou dossier abandonné.
        dossiers.MapPost("/{id:int}/fin-configuration", (int id, DemandeFinConfigurationDto demande, ServiceDossiers service, CancellationToken ct) =>
            service.FinConfigurationAsync(id, demande, ct));

        dossiers.MapPost("/{id:int}/valider", (int id, SaisieDossierDto saisie, ServiceDossiers service, ClaimsPrincipal utilisateur, CancellationToken ct) =>
            service.ValiderAsync(id, saisie, Roles.Nom(utilisateur), ct));

        // Configuration existante rouverte par son ConfigId (modification / consultation), sans dossier du portail.
        api.MapPost("/lancements", (OuvertureConfigurationDto demande, ServiceLancements service, CancellationToken ct) =>
            service.OuvrirAsync(demande, ct));
    }
}
