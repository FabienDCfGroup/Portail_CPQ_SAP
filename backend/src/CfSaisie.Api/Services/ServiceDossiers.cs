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
    ISapLogistiqueService logistique,
    ExportXmlDossier export,
    LanceurConfigurateur lanceur,
    ServicePlanning planning,
    LangueInterface langue,
    TimeProvider horloge,
    ILogger<ServiceDossiers> journal)
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
            await langue.PourConfigurateurAsync(annulation), annulation);
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
            ?? throw new RequeteInvalideException($"Client {dossier.CodeClient} introuvable dans les données ERP.");

        var changementProduit = demande.ProduitId is { } p && p != dossier.ProduitId;
        if (changementProduit && dossier.DevisSourceId is not null)
        {
            // Une nouvelle configuration romprait le lien avec le devis : la commande reprend la configuration du devis.
            throw new RequeteInvalideException("Une commande issue d'un devis reprend la configuration du devis : le produit ne peut pas être changé.");
        }
        // Nouveau produit : le dossier n'est modifié qu'à la fin de la configuration, si elle a été enregistrée (FinConfigurationAsync).
        var produit = changementProduit
            ? await ProduitAutorise(demande.ProduitId!.Value, client.Code, annulation)
            : dossier.Produit ?? throw new RequeteInvalideException("Aucun produit n'est associé à ce dossier : choisissez un produit.");

        var modification = !changementProduit && !string.IsNullOrEmpty(dossier.ConfigId);
        // Une configuration existante n'est modifiable que dans l'environnement où elle a été créée.
        var environnement = modification && dossier.Environnement is not null
            ? dossier.Environnement
            : lanceur.Resoudre(demande.Environnement);

        var resultat = await lanceur.LancerAsync(dossier, produit.Configurateur,
            modification ? ActionConfigurateur.Modification : ActionConfigurateur.Creation, environnement,
            await langue.PourConfigurateurAsync(annulation), annulation);

        if (resultat.Succes)
        {
            if (!modification && string.IsNullOrEmpty(dossier.ConfigId))
            {
                // Dossier encore sans configuration : la nouvelle devient la sienne (confirmée à la fin de la configuration).
                AffecterProduit(dossier, produit);
                dossier.ConfigId = resultat.ConfigId;
                dossier.Environnement = environnement;
                dossier.ConfigurationEnregistree = false;
            }
            dossier.DateModification = Maintenant();
            await db.SaveChangesAsync(annulation);
        }
        return new DossierLanceDto(await VersDto(dossier, annulation), VersDto(resultat));
    }

    /// <summary>
    /// La fenêtre du configurateur a été fermée : le Middleware dit si la configuration a été terminée (enregistrée ou validée).
    /// Non terminée, un dossier jamais confirmé ou une commande issue d'un devis passe à « Abandonné » ; un changement de produit
    /// n'est appliqué que si la nouvelle configuration a été enregistrée. La modification d'une configuration déjà enregistrée
    /// ne change rien (le Middleware ne permet pas de distinguer « enregistrer » d'« abandonner » dans ce cas).
    /// </summary>
    public async Task<FinConfigurationDto> FinConfigurationAsync(int id, DemandeFinConfigurationDto demande, CancellationToken annulation)
    {
        var dossier = await Charger(id, annulation);
        if (dossier.Statut != StatutDossier.EnCours)
        {
            return new FinConfigurationDto(await VersDto(dossier, annulation), IssueConfiguration.Inchangee);
        }

        // Changement de produit : nouvelle configuration en attente de confirmation.
        if (!string.IsNullOrWhiteSpace(demande.ConfigId) && demande.ConfigId != dossier.ConfigId && demande.ProduitId is { } produitId)
        {
            var produit = await ProduitAutorise(produitId, dossier.CodeClient, annulation);
            var environnement = lanceur.Resoudre(demande.Environnement);
            var etatNouvelle = await EtatConfigurationAsync(dossier, produit.Configurateur, demande.ConfigId, environnement, annulation);
            if (etatNouvelle is EtatConfiguration.Absente or EtatConfiguration.Indeterminee)
            {
                return new FinConfigurationDto(await VersDto(dossier, annulation),
                    etatNouvelle == EtatConfiguration.Absente ? IssueConfiguration.ProduitNonChange : IssueConfiguration.Indeterminee);
            }
            AffecterProduit(dossier, produit);
            dossier.ConfigId = demande.ConfigId;
            dossier.Environnement = environnement;
            dossier.ConfigurationEnregistree = true;
            // La fin de production dépend de la configuration : elle est à rechoisir.
            dossier.DateFinProduction = null;
            dossier.DateModification = Maintenant();
            await db.SaveChangesAsync(annulation);
            return new FinConfigurationDto(await VersDto(dossier, annulation), IssueConfiguration.Enregistree);
        }

        // Configuration déjà confirmée : rien à vérifier.
        if (dossier.ConfigurationEnregistree || string.IsNullOrEmpty(dossier.ConfigId) || dossier.Produit is null)
        {
            return new FinConfigurationDto(await VersDto(dossier, annulation), IssueConfiguration.Inchangee);
        }

        var etat = await EtatConfigurationAsync(dossier, dossier.Produit.Configurateur, dossier.ConfigId, lanceur.Resoudre(dossier.Environnement), annulation);
        // Commande issue d'un devis : terminée seulement si la configuration a été validée en commande (nature C).
        var terminee = dossier.DevisSourceId is not null
            ? etat == EtatConfiguration.Commande
            : etat is EtatConfiguration.Commande or EtatConfiguration.Devis or EtatConfiguration.Presente;
        IssueConfiguration issue;
        if (terminee)
        {
            dossier.ConfigurationEnregistree = true;
            issue = IssueConfiguration.Enregistree;
        }
        else if (etat == EtatConfiguration.Indeterminee)
        {
            // Middleware injoignable ou réponse inattendue : rien n'est abandonné, la finalisation reste possible.
            dossier.ConfigurationEnregistree = true;
            issue = IssueConfiguration.Indeterminee;
        }
        else
        {
            dossier.Statut = StatutDossier.Abandonne;
            issue = IssueConfiguration.Abandonnee;
        }
        dossier.DateModification = Maintenant();
        await db.SaveChangesAsync(annulation);
        return new FinConfigurationDto(await VersDto(dossier, annulation), issue);
    }

    private enum EtatConfiguration { Absente, Devis, Commande, Presente, Indeterminee }

    /// <summary>État d'une configuration dans le Middleware, d'après son XML de fin de configuration (GetXmlForSite).</summary>
    private async Task<EtatConfiguration> EtatConfigurationAsync(Dossier dossier, Configurateur configurateur, string configId, string environnement, CancellationToken annulation)
    {
        ResultatXmlConfiguration xml;
        try
        {
            xml = await lanceur.LireXmlAsync(dossier, configurateur, environnement, annulation, configId);
        }
        catch (Exception ex) when (ex is MiddlewareCpqException or HttpRequestException or TaskCanceledException)
        {
            journal.LogWarning(ex, "État de la configuration {ConfigId} indéterminé", configId);
            return EtatConfiguration.Indeterminee;
        }
        if (!xml.Succes || xml.Xml is null)
        {
            // « No config found for this Id Config » : rien d'enregistré. Toute autre réponse est une anomalie du Middleware.
            return xml.Message is null || xml.Message.Contains("Id Config", StringComparison.OrdinalIgnoreCase)
                ? EtatConfiguration.Absente
                : EtatConfiguration.Indeterminee;
        }
        try
        {
            return XmlConfigurationCpq.Lire(xml.Xml).Nature switch
            {
                "C" => EtatConfiguration.Commande,
                "D" => EtatConfiguration.Devis,
                _ => EtatConfiguration.Presente,
            };
        }
        catch (FormatException)
        {
            return EtatConfiguration.Presente;
        }
    }

    /// <summary>
    /// Suivi : transforme un devis transmis en commande. Crée une commande « En cours » liée au devis, sur la même configuration
    /// (même ConfigId, donc même fichier XML, remplacé à la validation de la commande). La configuration est ensuite rouverte
    /// en commande (TypeTransaction = Order) depuis l'assistant : le Middleware la fait passer de devis à commande.
    /// Une transformation déjà en cours est reprise au lieu d'en créer une seconde.
    /// </summary>
    public async Task<DossierDto> TransformerEnCommandeAsync(int devisId, string utilisateur, CancellationToken annulation)
    {
        var devis = await Charger(devisId, annulation);
        if (devis.Type != TypeDossier.Devis) throw new RequeteInvalideException($"{devis.Reference} n'est pas un devis.");
        if (devis.Statut is not (StatutDossier.Envoye or StatutDossier.Validee))
        {
            throw new RequeteInvalideException($"Le devis {devis.Reference} doit être finalisé avant d'être transformé en commande.");
        }
        if (!string.IsNullOrEmpty(devis.CommandeIssueReference))
        {
            throw new RequeteInvalideException($"Le devis {devis.Reference} a déjà été transformé en commande ({devis.CommandeIssueReference}).");
        }
        if (string.IsNullOrEmpty(devis.ConfigId) || devis.Produit is null)
        {
            throw new RequeteInvalideException($"Le devis {devis.Reference} n'a pas de configuration : il ne peut pas être transformé en commande.");
        }

        var enCours = await db.Dossiers.Where(d => d.DevisSourceId == devis.Id && d.Statut == StatutDossier.EnCours)
            .Select(d => d.Id).FirstOrDefaultAsync(annulation);
        if (enCours != 0)
        {
            return await LireAsync(enCours, annulation);
        }

        var maintenant = Maintenant();
        var commande = new Dossier
        {
            Reference = await NouvelleReference(TypeDossier.Commande, annulation),
            Type = TypeDossier.Commande,
            Origine = OrigineDossier.CreationInterne,
            Statut = StatutDossier.EnCours,
            CreePar = utilisateur,
            DateCreation = maintenant,
            DateModification = maintenant,
            CodeClient = devis.CodeClient,
            NomClient = devis.NomClient,
            ReferenceChantier = devis.ReferenceChantier,
            TicketSalesforce = devis.TicketSalesforce,
            ReferenceCommandeClient = devis.ReferenceCommandeClient,
            ConfigId = devis.ConfigId,
            Environnement = devis.Environnement,
            // La configuration doit être rouverte et validée en commande dans le configurateur avant la finalisation.
            ConfigurationEnregistree = false,
            // La fin de production est à choisir pour la commande (planning du jour).
            DateFinProduction = null,
            CommentaireExterne = devis.CommentaireExterne,
            CommentaireInterne = devis.CommentaireInterne,
            ModeLivraison = devis.ModeLivraison,
            LivraisonCodeClient = devis.LivraisonCodeClient,
            LivraisonNomClient = devis.LivraisonNomClient,
            LivraisonLibelleAdresse = devis.LivraisonLibelleAdresse,
            LivraisonNom = devis.LivraisonNom,
            LivraisonTelephone = devis.LivraisonTelephone,
            LivraisonAdresse = devis.LivraisonAdresse,
            LivraisonCodePostal = devis.LivraisonCodePostal,
            LivraisonVille = devis.LivraisonVille,
            Acces = devis.Acces,
            AccesAutre = devis.AccesAutre,
            AgenceRetrait = devis.AgenceRetrait,
            DateLivraisonSouhaitee = devis.DateLivraisonSouhaitee,
            DevisSourceId = devis.Id,
        };
        AffecterProduit(commande, devis.Produit);
        db.Dossiers.Add(commande);
        await db.SaveChangesAsync(annulation);
        return await VersDto(commande, annulation);
    }

    /// <summary>Suivi : ouvre la configuration d'un dossier en consultation (lecture seule), dans l'environnement où elle a été créée.</summary>
    public async Task<DossierLanceDto> ConsulterAsync(int id, CancellationToken annulation)
    {
        var dossier = await Charger(id, annulation);
        if (string.IsNullOrEmpty(dossier.ConfigId) || dossier.Produit is null)
        {
            throw new RequeteInvalideException($"Le dossier {dossier.Reference} n'a pas de configuration à consulter.");
        }
        var resultat = await lanceur.LancerAsync(dossier, dossier.Produit.Configurateur, ActionConfigurateur.Consultation,
            lanceur.Resoudre(dossier.Environnement), await langue.PourConfigurateurAsync(annulation), annulation);
        return new DossierLanceDto(await VersDto(dossier, annulation), VersDto(resultat));
    }

    /// <summary>
    /// « VALIDER COMMANDE / DEVIS » : enregistre la saisie finale et passe le dossier au statut « Envoyé ».
    /// Pas d'appel SAP : le numéro de commande SAP arrivera par le flux ERP (erp.Commandes, rapprochement par ConfigId).
    /// </summary>
    public async Task<DossierDto> ValiderAsync(int id, SaisieDossierDto saisie, string validePar, CancellationToken annulation)
    {
        var dossier = await Charger(id, annulation);
        VerifierModifiable(dossier);
        var client = await ValiderSaisie(saisie with { Type = dossier.Type }, annulation);
        Appliquer(dossier, saisie, client);

        if (dossier.Produit is null) throw new RequeteInvalideException("Choisissez d'abord un produit / configurateur.");
        if (dossier.DevisSourceId is not null && !dossier.ConfigurationEnregistree)
        {
            // Sans ce passage, la configuration resterait un devis dans le configurateur (pas de nomenclature, pas d'envoi ERP).
            throw new RequeteInvalideException("Ouvrez la configuration en commande dans le configurateur et validez-la avant de finaliser la commande.");
        }
        if (dossier.DateFinProduction is null) throw new RequeteInvalideException("Renseignez la date de fin de prod.");
        // Saisie libre comme calendrier : une fin de production ne peut pas être dans le passé.
        if (dossier.DateFinProduction < DateOnly.FromDateTime(Maintenant()))
        {
            throw new RequeteInvalideException("La date de fin de prod ne peut pas être antérieure à aujourd'hui.");
        }
        var codeCcx = await planning.VerifierDateAsync(dossier, dossier.DateFinProduction.Value, annulation);

        // Une commande du site internet contrôlée puis validée passe à « Validée » ; sinon elle est « Envoyée ».
        dossier.Statut = dossier.Origine == OrigineDossier.SiteInternet && dossier.Statut == StatutDossier.AValider
            ? StatutDossier.Validee
            : StatutDossier.Envoye;
        dossier.DateModification = Maintenant();

        // Commande issue d'un devis : le devis pointe désormais vers elle.
        if (dossier.DevisSourceId is { } devisId && await db.Dossiers.FirstOrDefaultAsync(d => d.Id == devisId, annulation) is { } devis)
        {
            devis.CommandeIssueReference = dossier.Reference;
            devis.DateModification = dossier.DateModification;
        }

        // XML de fin de configuration du CPQ, joint à l'export : sans lui, rien n'est envoyé à SAP.
        var xmlCpq = await lanceur.LireXmlAsync(dossier, dossier.Produit.Configurateur, lanceur.Resoudre(dossier.Environnement), annulation);
        if (!xmlCpq.Succes || xmlCpq.Xml is null)
        {
            throw new RequeteInvalideException(
                $"Le XML de la configuration {dossier.ConfigId} n'a pas pu être lu dans le Middleware ({xmlCpq.Message ?? "réponse vide"}) : enregistrez la configuration, puis réessayez.");
        }

        // Répertoire {ConfigId} : {ConfigId}_portailSAP.xml et {ConfigId}.xml (mêmes noms pour un devis et la commande
        // qui en est issue : les fichiers du devis sont remplacés). Écrits avant l'enregistrement du statut.
        var fichiers = await export.EcrireAsync(await DonneesExportAsync(dossier, client, codeCcx, validePar, annulation), xmlCpq.Xml, annulation);
        await db.SaveChangesAsync(annulation);
        return await VersDto(dossier, annulation) with { FichierXml = Path.GetFileName(fichiers.Repertoire) };
    }

    /// <summary>Données ERP retrouvées pour le fichier XML : adresse ERP choisie (« Chez le client ») ou agence de retrait.</summary>
    private async Task<DonneesExport> DonneesExportAsync(Dossier d, ClientSap client, string? codeCcx, string validePar, CancellationToken annulation)
    {
        AdresseLivraisonSap? adresse = null;
        if (d.ModeLivraison == "Chez le client" && !string.IsNullOrEmpty(d.LivraisonCodeClient))
        {
            adresse = (await clients.AdressesAsync(d.LivraisonCodeClient, annulation)).FirstOrDefault(a => a.Libelle == d.LivraisonLibelleAdresse);
        }
        AgenceRetraitSap? agence = null;
        if (d.ModeLivraison == "Retrait en agence" && !string.IsNullOrEmpty(d.AgenceRetrait))
        {
            agence = (await logistique.AgencesAsync(annulation)).FirstOrDefault(a => a.Libelle == d.AgenceRetrait);
        }
        var devisSource = d.DevisSourceId is { } s
            ? await db.Dossiers.Where(x => x.Id == s).Select(x => x.Reference).FirstOrDefaultAsync(annulation)
            : null;
        return new DonneesExport(d, client, adresse, agence, devisSource, codeCcx, validePar, Maintenant());
    }

    private async Task<ClientSap> ValiderSaisie(SaisieDossierDto s, CancellationToken annulation)
    {
        if (string.IsNullOrWhiteSpace(s.CodeClient)) throw new RequeteInvalideException("Choisissez un client.");
        var client = await clients.LireAsync(s.CodeClient.Trim(), annulation)
            ?? throw new RequeteInvalideException($"Client {s.CodeClient} introuvable dans les données ERP.");
        if (!client.Actif) throw new RequeteInvalideException($"Le client {client.Code} est inactif dans l'ERP.");
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
