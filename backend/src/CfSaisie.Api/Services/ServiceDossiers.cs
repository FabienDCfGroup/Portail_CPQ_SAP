using CfSaisie.Cpq;
using CfSaisie.Data;
using CfSaisie.Sap;
using Microsoft.EntityFrameworkCore;

namespace CfSaisie.Api.Services;

public sealed class RequeteInvalideException(string message) : Exception(message);

public sealed class DossierIntrouvableException(int id) : Exception($"Dossier {id} introuvable.");

/// <summary>Règles de gestion des commandes et devis (reprises de la maquette et de l'ancien outil).</summary>
public sealed class ServiceDossiers(
    CfSaisieContext db,
    ISapClientService clients,
    ISapCommandeService commandesSap,
    LanceurConfigurateur lanceur,
    TimeProvider horloge)
{
    public static readonly string[] ModesLivraison = ["Chez le client", "A saisir", "Retrait en agence"];

    public async Task<DossierDto> LireAsync(int id, CancellationToken annulation)
    {
        var dossier = await Charger(id, annulation);
        return await VersDto(dossier, annulation);
    }

    /// <summary>Crée le dossier « En cours » et lance le configurateur du produit choisi (création).</summary>
    public async Task<DossierLanceDto> CreerAsync(CreationDossierDto demande, string utilisateur, CancellationToken annulation)
    {
        var client = await ValiderSaisie(demande.Saisie, annulation);
        var produit = await ProduitAutorise(demande.ProduitId, client.Code, annulation);
        var environnement = lanceur.Resoudre(demande.Environnement);
        var maintenant = Maintenant();

        var dossier = new Dossier
        {
            Reference = await NouvelleReference(demande.Saisie.Type, annulation),
            Type = demande.Saisie.Type,
            Origine = OrigineDossier.CreationInterne,
            Statut = StatutDossier.EnCours,
            CreePar = utilisateur,
            DateCreation = maintenant,
        };
        Appliquer(dossier, demande.Saisie, client);
        AffecterProduit(dossier, produit);

        var resultat = await lanceur.LancerAsync(dossier, produit.Configurateur, ActionConfigurateur.Creation, environnement,
            LanceurConfigurateur.Langue(client.Langue), annulation);
        if (!resultat.Succes)
        {
            // Rien n'est enregistré si le Middleware refuse le lancement.
            return new DossierLanceDto(DossierDto.De(dossier), VersDto(resultat));
        }

        dossier.ConfigId = resultat.ConfigId;
        dossier.Environnement = environnement;
        db.Dossiers.Add(dossier);
        await db.SaveChangesAsync(annulation);
        return new DossierLanceDto(DossierDto.De(dossier), VersDto(resultat));
    }

    public async Task<DossierDto> MettreAJourAsync(int id, SaisieDossierDto saisie, CancellationToken annulation)
    {
        var dossier = await Charger(id, annulation);
        VerifierModifiable(dossier);
        var client = await ValiderSaisie(saisie with { Type = dossier.Type }, annulation);
        Appliquer(dossier, saisie, client);
        await db.SaveChangesAsync(annulation);
        return await VersDto(dossier, annulation);
    }

    /// <summary>Relance le configurateur d'un dossier : modification de la configuration existante, ou création si le produit change.</summary>
    public async Task<DossierLanceDto> RelancerAsync(int id, LancerConfigurateurDto demande, CancellationToken annulation)
    {
        var dossier = await Charger(id, annulation);
        VerifierModifiable(dossier);
        var client = await clients.LireAsync(dossier.CodeClient, annulation)
            ?? throw new RequeteInvalideException($"Client {dossier.CodeClient} introuvable dans SAP.");

        var changementProduit = demande.ProduitId is { } p && p != dossier.ProduitId;
        if (changementProduit)
        {
            AffecterProduit(dossier, await ProduitAutorise(demande.ProduitId!.Value, client.Code, annulation));
        }
        var produit = dossier.Produit ?? throw new RequeteInvalideException("Aucun produit n'est associé à ce dossier : choisissez un produit.");

        var modification = !changementProduit && !string.IsNullOrEmpty(dossier.ConfigId);
        // Une configuration existante n'est modifiable que dans l'environnement où elle a été créée.
        var environnement = modification && dossier.Environnement is not null
            ? dossier.Environnement
            : lanceur.Resoudre(demande.Environnement);

        var resultat = await lanceur.LancerAsync(dossier, produit.Configurateur,
            modification ? ActionConfigurateur.Modification : ActionConfigurateur.Creation, environnement,
            LanceurConfigurateur.Langue(client.Langue), annulation);

        if (resultat.Succes)
        {
            if (!modification)
            {
                dossier.ConfigId = resultat.ConfigId;
                dossier.Environnement = environnement;
                dossier.ConfigurationEnregistree = false;
            }
            dossier.DateModification = Maintenant();
            await db.SaveChangesAsync(annulation);
        }
        return new DossierLanceDto(await VersDto(dossier, annulation), VersDto(resultat));
    }

    /// <summary>Le configurateur a été fermé : la finalisation devient possible (comportement de la maquette).</summary>
    public async Task<DossierDto> ConfigurationEnregistreeAsync(int id, CancellationToken annulation)
    {
        var dossier = await Charger(id, annulation);
        dossier.ConfigurationEnregistree = true;
        dossier.DateModification = Maintenant();
        await db.SaveChangesAsync(annulation);
        return await VersDto(dossier, annulation);
    }

    /// <summary>« VALIDER COMMANDE / DEVIS » : enregistre la saisie et transmet le dossier à SAP.</summary>
    public async Task<DossierDto> ValiderAsync(int id, SaisieDossierDto saisie, CancellationToken annulation)
    {
        var dossier = await Charger(id, annulation);
        VerifierModifiable(dossier);
        var client = await ValiderSaisie(saisie with { Type = dossier.Type }, annulation);
        Appliquer(dossier, saisie, client);

        if (dossier.Produit is null) throw new RequeteInvalideException("Choisissez d'abord un produit / configurateur.");
        if (dossier.DateFinProduction is null) throw new RequeteInvalideException("Renseignez la date de fin de prod.");

        var envoi = await commandesSap.EnvoyerAsync(new CommandeAEnvoyer(
            dossier.Reference, dossier.Type == TypeDossier.Devis, dossier.CodeClient, dossier.NomProduit, dossier.ConfigId, dossier.DateFinProduction), annulation);
        if (!envoi.Succes)
        {
            dossier.Statut = StatutDossier.Erreur;
            await db.SaveChangesAsync(annulation);
            throw new RequeteInvalideException($"SAP a refusé le dossier : {envoi.Message}");
        }

        dossier.NumeroSap ??= envoi.NumeroSap;
        // Une commande du site internet contrôlée puis validée passe à « Validée » ; sinon elle est « Envoyée » à SAP.
        dossier.Statut = dossier.Origine == OrigineDossier.SiteInternet && dossier.Statut == StatutDossier.AValider
            ? StatutDossier.Validee
            : StatutDossier.Envoye;
        dossier.DateModification = Maintenant();
        await db.SaveChangesAsync(annulation);
        return await VersDto(dossier, annulation);
    }

    private async Task<ClientSap> ValiderSaisie(SaisieDossierDto s, CancellationToken annulation)
    {
        if (string.IsNullOrWhiteSpace(s.CodeClient)) throw new RequeteInvalideException("Choisissez un client.");
        var client = await clients.LireAsync(s.CodeClient.Trim(), annulation)
            ?? throw new RequeteInvalideException($"Client {s.CodeClient} introuvable dans SAP.");
        if (string.IsNullOrWhiteSpace(s.ReferenceChantier)) throw new RequeteInvalideException("La référence chantier est obligatoire.");
        if (string.IsNullOrWhiteSpace(s.TicketSalesforce)) throw new RequeteInvalideException("Le numéro de ticket Salesforce est obligatoire.");

        switch (s.ModeLivraison)
        {
            case "Chez le client" or "A saisir":
                if (new[] { s.LivraisonNom, s.LivraisonTelephone, s.LivraisonAdresse, s.LivraisonCodePostal, s.LivraisonVille }.Any(string.IsNullOrWhiteSpace))
                {
                    throw new RequeteInvalideException("L'adresse de livraison est incomplète (nom, téléphone, adresse, code postal, ville).");
                }
                break;
            case "Retrait en agence":
                if (string.IsNullOrWhiteSpace(s.AgenceRetrait)) throw new RequeteInvalideException("Choisissez l'agence de retrait.");
                break;
            default:
                throw new RequeteInvalideException("Choisissez le mode de livraison ou de retrait.");
        }
        return client;
    }

    private async Task<Produit> ProduitAutorise(int produitId, string codeClient, CancellationToken annulation)
    {
        var produit = await db.Produits.Include(p => p.Configurateur).Include(p => p.Famille)
            .FirstOrDefaultAsync(p => p.Id == produitId, annulation)
            ?? throw new RequeteInvalideException("Produit introuvable.");
        var autorise = await db.Accreditations.AnyAsync(a => a.CodeClient == codeClient && a.ProduitId == produitId, annulation);
        if (!autorise || produit.Famille is not { Actif: true })
        {
            throw new RequeteInvalideException($"Le produit {produit.Nom} n'est pas autorisé pour ce client.");
        }
        if (!produit.Configurateur.Actif)
        {
            throw new RequeteInvalideException($"Le configurateur {produit.Configurateur.Nom} est inactif.");
        }
        return produit;
    }

    private void Appliquer(Dossier d, SaisieDossierDto s, ClientSap client)
    {
        var retrait = s.ModeLivraison == "Retrait en agence";
        d.CodeClient = client.Code;
        d.NomClient = client.RaisonSociale;
        d.ReferenceChantier = Texte(s.ReferenceChantier);
        d.TicketSalesforce = Texte(s.TicketSalesforce);
        d.ModeLivraison = s.ModeLivraison;
        d.LivraisonCodeClient = s.ModeLivraison == "Chez le client" ? Texte(s.LivraisonCodeClient) : null;
        d.LivraisonNomClient = s.ModeLivraison == "Chez le client" ? Texte(s.LivraisonNomClient) : null;
        d.LivraisonLibelleAdresse = s.ModeLivraison == "Chez le client" ? Texte(s.LivraisonLibelleAdresse) : null;
        d.LivraisonNom = retrait ? null : Texte(s.LivraisonNom);
        d.LivraisonTelephone = retrait ? null : Texte(s.LivraisonTelephone);
        d.LivraisonAdresse = retrait ? null : Texte(s.LivraisonAdresse);
        d.LivraisonCodePostal = retrait ? null : Texte(s.LivraisonCodePostal);
        d.LivraisonVille = retrait ? null : Texte(s.LivraisonVille);
        d.Acces = retrait || s.Acces is null ? null : string.Join(';', s.Acces.Where(a => !string.IsNullOrWhiteSpace(a)));
        d.AccesAutre = retrait ? null : Texte(s.AccesAutre);
        d.AgenceRetrait = retrait ? Texte(s.AgenceRetrait) : null;
        d.DateFinProduction = s.DateFinProduction;
        d.CommentaireExterne = Texte(s.CommentaireExterne);
        d.CommentaireInterne = Texte(s.CommentaireInterne);
        d.DateModification = Maintenant();
    }

    private static void AffecterProduit(Dossier d, Produit p)
    {
        d.Produit = p;
        d.ProduitId = p.Id;
        d.NomProduit = p.Nom;
        d.NomFamille = p.Famille?.Nom ?? string.Empty;
    }

    private static void VerifierModifiable(Dossier d)
    {
        var modifiable = d.Statut == StatutDossier.EnCours || (d.Statut == StatutDossier.AValider && d.Origine == OrigineDossier.SiteInternet);
        if (!modifiable) throw new RequeteInvalideException("Ce dossier ne peut pas être rouvert dans cet état.");
    }

    private async Task<Dossier> Charger(int id, CancellationToken annulation) =>
        await db.Dossiers.Include(d => d.Produit).ThenInclude(p => p!.Configurateur)
            .Include(d => d.Produit).ThenInclude(p => p!.Famille)
            .FirstOrDefaultAsync(d => d.Id == id, annulation)
        ?? throw new DossierIntrouvableException(id);

    private async Task<string> NouvelleReference(TypeDossier type, CancellationToken annulation)
    {
        var prefixe = $"{(type == TypeDossier.Devis ? "DV" : "CF")}-{Maintenant().Year}-";
        var existantes = await db.Dossiers.Where(d => d.Reference.StartsWith(prefixe)).Select(d => d.Reference).ToListAsync(annulation);
        var dernier = existantes.Select(r => int.TryParse(r[prefixe.Length..], out var n) ? n : 0).DefaultIfEmpty(1000).Max();
        return prefixe + (Math.Max(dernier, 1000) + 1).ToString("D4");
    }

    private async Task<DossierDto> VersDto(Dossier d, CancellationToken annulation)
    {
        var source = d.DevisSourceId is { } s
            ? await db.Dossiers.Where(x => x.Id == s).Select(x => x.Reference).FirstOrDefaultAsync(annulation)
            : null;
        return DossierDto.De(d, source);
    }

    private static LancementDto VersDto(ResultatLancement r) => new(r.Succes, r.PeutOuvrir, r.Message, r.Url, r.ConfigId);

    private DateTime Maintenant() => horloge.GetLocalNow().DateTime;

    private static string? Texte(string? valeur) => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();
}
