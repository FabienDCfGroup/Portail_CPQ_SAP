using CfSaisie.Data;
using CfSaisie.Sap;
using Microsoft.EntityFrameworkCore;

namespace CfSaisie.Api.Services;

/// <summary>
/// Clients, adresses de livraison et agences de retrait, lus dans les tables du schéma « erp »
/// (erp.Clients, erp.AdressesClients, erp.AgencesRetrait) alimentées par le flux ERP. Lecture seule.
/// </summary>
public sealed class DonneesErp(CfSaisieContext db) : ISapClientService, ISapLogistiqueService
{
    public async Task<IReadOnlyList<ClientSap>> RechercherAsync(string? recherche, int maximum, CancellationToken annulation = default)
    {
        var requete = db.ClientsErp.AsNoTracking().Where(c => c.Actif);
        var q = recherche?.Trim();
        if (!string.IsNullOrEmpty(q))
        {
            // Collation insensible à la casse : « alb » trouve « ALBE OCCITANIE ».
            requete = requete.Where(c => c.CodeClient.StartsWith(q) || c.RaisonSociale.Contains(q));
        }
        return await requete
            .OrderBy(c => c.RaisonSociale).ThenBy(c => c.CodeClient)
            .Take(maximum)
            .Select(c => new ClientSap(c.CodeClient, c.RaisonSociale, c.Pays, c.Langue, c.ModePaiement, c.EstVic, c.Actif))
            .ToListAsync(annulation);
    }

    public async Task<ClientSap?> LireAsync(string code, CancellationToken annulation = default) =>
        await db.ClientsErp.AsNoTracking()
            .Where(c => c.CodeClient == code)
            .Select(c => new ClientSap(c.CodeClient, c.RaisonSociale, c.Pays, c.Langue, c.ModePaiement, c.EstVic, c.Actif))
            .FirstOrDefaultAsync(annulation);

    public async Task<IReadOnlyList<AdresseLivraisonSap>> AdressesAsync(string code, CancellationToken annulation = default) =>
        await db.AdressesClientsErp.AsNoTracking()
            .Where(a => a.CodeClient == code)
            .OrderByDescending(a => a.EstPrincipale).ThenBy(a => a.Libelle).ThenBy(a => a.IdAdresse)
            .Select(a => new AdresseLivraisonSap(a.IdAdresse, a.Libelle, a.EstPrincipale, a.NomDestinataire, a.Telephone ?? string.Empty,
                a.Adresse, a.ComplementAdresse, a.CodePostal, a.Ville, a.Pays))
            .ToListAsync(annulation);

    public async Task<IReadOnlyList<AgenceRetraitSap>> AgencesAsync(CancellationToken annulation = default) =>
        await db.AgencesRetraitErp.AsNoTracking()
            .Where(a => a.Actif)
            .OrderBy(a => a.Libelle)
            .Select(a => new AgenceRetraitSap(a.CodeAgence, a.Libelle, a.Region))
            .ToListAsync(annulation);
}
