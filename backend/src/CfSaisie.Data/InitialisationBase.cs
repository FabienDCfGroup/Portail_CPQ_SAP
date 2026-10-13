using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace CfSaisie.Data;

/// <summary>
/// Applique les migrations puis alimente les tables vides :
/// configurateurs de l'ancien outil (Produits.csv) et données de démonstration de la maquette.
/// </summary>
public static class InitialisationBase
{
    private static readonly string[] NomsConfigurateurs =
    [
        "Azteck", "Bassin", "BulleAlveolaire", "BulleAzteck", "BulleGeobubble", "BulleJumbo", "BulleRonde", "BulleSO", "CAB",
        "CaillebotisHorsEau", "CaillebotisImmerge", "CHIV_Initial", "CHIV_Supergreen", "Contura", "Deepeo", "Ivernea", "JDAC",
        "Liner", "MA10", "MA10PE", "Moon", "Mooveo", "CouvertureMousse", "Perseo", "PoutrelleHorsEau", "PoutrelleImmergee",
        "RecupEau", "Rolleasy2", "Rollenergy", "Rollin", "Rollinside", "RollOver", "Securiwall", "SIGN", "Tablier69", "Tablier84",
        "Tixit", "Volet", "VPHS", "VF510", "MembraneArmee", "MA10PEDEL", "VF513", "Jupiter", "Neptune",
    ];

    public static async Task InitialiserAsync(CfSaisieContext db, CancellationToken annulation = default)
    {
        await db.Database.MigrateAsync(annulation);

        if (!await db.Configurateurs.AnyAsync(annulation))
        {
            db.Configurateurs.AddRange(NomsConfigurateurs.Select(n => new Configurateur { Nom = n }));
            await db.SaveChangesAsync(annulation);
        }

        if (!await db.Familles.AnyAsync(annulation))
        {
            await CatalogueDemoAsync(db, annulation);
        }

        if (!await db.CommentaireFamilles.AnyAsync(annulation))
        {
            CommentairesDemo(db);
        }

        if (!await db.Messages.AnyAsync(annulation))
        {
            db.Messages.Add(new MessageTableauBord
            {
                Audience = "Tous",
                Titre = "Information",
                Corps = "Bienvenue dans CF Saisie commandes. Utilisez le nouveau parcours guidé pour sélectionner un client et son configurateur.",
                DateCreation = DateTime.Now,
            });
        }

        if (!await db.Utilisateurs.AnyAsync(annulation))
        {
            db.Utilisateurs.AddRange(
                new Utilisateur { Nom = "Thomas Martin", Role = "Utilisateur" },
                new Utilisateur { Nom = "Laura Dupuis", Role = "Administrateur" },
                new Utilisateur { Nom = "Sophie Caron", Role = "Utilisateur" },
                new Utilisateur { Nom = "Julien Dubois", Role = "Utilisateur" });
        }

        await db.SaveChangesAsync(annulation);

        if (!await db.Dossiers.AnyAsync(annulation))
        {
            await DossiersDemoAsync(db, annulation);
        }
    }

    private static async Task CatalogueDemoAsync(CfSaisieContext db, CancellationToken annulation)
    {
        var configurateurs = await db.Configurateurs.ToDictionaryAsync(c => c.Nom, annulation);
        var familles = new[] { "Liner", "Volet hors-sol", "Volet immergé", "Couverture d'hivernage" }
            .ToDictionary(n => n, n => new Famille { Nom = n });

        // Produits de la maquette, reliés aux vrais noms de configurateurs du Middleware.
        (string Nom, string Famille, string Configurateur, string Description)[] produits =
        [
            ("Liner sur mesure", "Liner", "Liner", "Configurer le liner, les dimensions, le coloris et les finitions."),
            ("Rolleasy2", "Volet hors-sol", "Rolleasy2", "Volet hors-sol compact et simple à configurer."),
            ("Moon", "Volet hors-sol", "Moon", "Volet hors-sol Moon et ses options disponibles."),
            ("Contura", "Volet hors-sol", "Contura", "Volet hors-sol Contura selon les caractéristiques du bassin."),
            ("Rollover", "Volet hors-sol", "RollOver", "Volet hors-sol Rollover avec choix des équipements."),
            ("Rollin", "Volet immergé", "Rollin", "Volet immergé Rollin et ses équipements associés."),
            ("Rollinside", "Volet immergé", "Rollinside", "Volet immergé Rollinside intégré au bassin."),
            ("Rolenergy", "Volet immergé", "Rollenergy", "Volet immergé Rolenergy avec options énergétiques."),
            ("Initial", "Couverture d'hivernage", "CHIV_Initial", "Couverture d’hivernage Initial pour protéger le bassin."),
            ("Supergreen", "Couverture d'hivernage", "CHIV_Supergreen", "Couverture d’hivernage Supergreen, protection renforcée."),
        ];

        var entites = produits.ToDictionary(p => p.Nom, p => new Produit
        {
            Nom = p.Nom,
            Description = p.Description,
            Famille = familles[p.Famille],
            Configurateur = configurateurs[p.Configurateur],
        });
        db.Produits.AddRange(entites.Values);

        Dictionary<string, string[]> autorises = new()
        {
            ["100124"] = ["Liner sur mesure", "Rolleasy2", "Moon", "Initial"],
            ["100811"] = ["Rollin", "Rollinside", "Initial", "Supergreen"],
            ["100442"] = ["Liner sur mesure", "Rolleasy2", "Contura", "Rollover", "Rollin"],
            ["100287"] = ["Liner sur mesure", "Initial", "Supergreen"],
            ["100932"] = ["Moon", "Contura", "Rollin", "Rolenergy", "Supergreen"],
            ["102466"] = [.. entites.Keys], // Compte client de test : accès complet (maquette)
        };
        foreach (var (code, noms) in autorises)
        {
            db.Accreditations.AddRange(noms.Select(n => new Accreditation { CodeClient = code, Produit = entites[n] }));
        }

        await db.SaveChangesAsync(annulation);
    }

    private static void CommentairesDemo(CfSaisieContext db)
    {
        (string Famille, string Texte)[] modeles =
        [
            ("Conformité", "Merci de vérifier la conformité des informations avant validation."),
            ("Paiement", "Commande soumise à validation des conditions de paiement."),
            ("Export", "Merci de joindre les documents nécessaires à l’export."),
            ("Transport", "Merci de confirmer les contraintes d’accès et de livraison."),
            ("Autres", "Information complémentaire à préciser avec le client."),
        ];
        for (var i = 0; i < modeles.Length; i++)
        {
            db.CommentaireFamilles.Add(new CommentaireFamille
            {
                Nom = modeles[i].Famille,
                Ordre = i + 1,
                Modeles = [new CommentaireModele { Ordre = 1, Texte = modeles[i].Texte }],
            });
        }
    }

    private static async Task DossiersDemoAsync(CfSaisieContext db, CancellationToken annulation)
    {
        var produits = await db.Produits.ToDictionaryAsync(p => p.Nom, annulation);
        Dictionary<string, string> codes = new()
        {
            ["Dupont Piscines"] = "100124", ["Aqua Services"] = "100811", ["Waterlijn BV"] = "100442",
            ["Piscinas Atlântico"] = "100287", ["Blue Horizon"] = "100932", ["Tony Stark"] = "102466",
        };

        Dossier D(string reference, string client, string produit, string famille, string creePar, string date, StatutDossier statut,
            string? chantier, string? sap = null, OrigineDossier origine = OrigineDossier.CreationInterne)
        {
            var dateCreation = DateTime.ParseExact(date, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            return new Dossier
            {
                Reference = reference,
                Type = reference.StartsWith("DV-") ? TypeDossier.Devis : TypeDossier.Commande,
                Origine = origine,
                Statut = statut,
                CodeClient = codes[client],
                NomClient = client,
                Produit = produits.GetValueOrDefault(produit),
                NomProduit = produit,
                NomFamille = famille,
                CreePar = creePar,
                DateCreation = dateCreation,
                DateModification = dateCreation,
                ReferenceChantier = chantier,
                NumeroSap = sap,
            };
        }

        static void Livrer(Dossier d, string mode, string? codeSource, string? nomSource, string? libelle, string nom, string tel,
            string adresse, string cp, string ville, string acces)
        {
            d.ModeLivraison = mode;
            d.LivraisonCodeClient = codeSource;
            d.LivraisonNomClient = nomSource;
            d.LivraisonLibelleAdresse = libelle;
            d.LivraisonNom = nom;
            d.LivraisonTelephone = tel;
            d.LivraisonAdresse = adresse;
            d.LivraisonCodePostal = cp;
            d.LivraisonVille = ville;
            d.Acces = acces;
        }

        static void Finaliser(Dossier d, string ticket, string dateFin, string externe, string interne)
        {
            d.TicketSalesforce = ticket;
            d.DateFinProduction = DateOnly.Parse(dateFin, CultureInfo.InvariantCulture);
            d.ConfigurationEnregistree = true;
            d.CommentaireExterne = externe;
            d.CommentaireInterne = interne;
        }

        var d1051 = D("DV-2026-1051", "Tony Stark", "Rolleasy2", "Volet hors-sol", "Thomas Martin", "02/10/2026 13:48", StatutDossier.EnCours, "STARK-MALIBU");
        Finaliser(d1051, "SF-1051", "2026-10-20", "Livraison à confirmer avec le client.", "Client VIC — vérifier le paiement avant validation.");
        Livrer(d1051, "Chez le client", "102466", "Tony Stark", "Adresse principale", "Tony Stark", "01 02 46 60 00", "10880 Malibu Point", "75008", "Paris", "VL");

        var d1048 = D("CF-2026-1048", "Waterlijn BV", "Rollizi", "Volet", "Laura Dupuis", "02/10/2026 11:36", StatutDossier.EnCours, "WL-ROTTERDAM", "45001048");
        Finaliser(d1048, "SF-1048", "2026-10-23", "Prévenir le client avant expédition.", "Contrôler les documents export.");
        Livrer(d1048, "A saisir", null, null, null, "Waterlijn BV - Chantier Rotterdam", "+31 10 555 10 48", "22 Maasboulevard", "3011 AA", "Rotterdam", "Porteur");

        var d1030 = D("DV-2026-1030", "Piscinas Atlântico", "Supergreen", "Couverture d'hivernage", "Julien Dubois", "01/10/2026 09:18", StatutDossier.EnCours, "ATL-PORTO");
        Finaliser(d1030, "SF-1030", "2026-10-16", "Livraison sur dépôt Nord.", "Dossier export Portugal.");
        Livrer(d1030, "Chez le client", "100287", "Piscinas Atlântico", "Dépôt Nord", "Piscinas Atlântico - Norte", "+351 22 100 02 88", "27 Rua das Piscinas", "4000-010", "Porto", "Semi-remorque");

        var w2107 = D("WEB-2026-2107", "Dupont Piscines", "Rolleasy2", "Volet hors-sol", "Site internet", "08/10/2026 10:22", StatutDossier.AValider, "WEB-DUP-2107", origine: OrigineDossier.SiteInternet);
        Finaliser(w2107, "WEB-2107", "2026-10-27", "Commande créée depuis le site internet.", "Commande intégrée automatiquement — à contrôler avant validation.");
        Livrer(w2107, "Chez le client", "100124", "Dupont Piscines", "Adresse principale", "Dupont Piscines", "02 99 00 12 24", "12 rue des Bassins", "35000", "Rennes", "Porteur");

        var w2108 = D("WEB-2026-2108", "Aqua Services", "Rollin", "Volet immergé", "Site internet", "08/10/2026 11:05", StatutDossier.AValider, "WEB-AQUA-2108", origine: OrigineDossier.SiteInternet);
        Finaliser(w2108, "WEB-2108", "2026-10-30", "Commande transmise depuis le site internet.", "Vérifier les informations avant envoi à SAP.");
        Livrer(w2108, "Chez le client", "100811", "Aqua Services", "Adresse principale", "Aqua Services", "02 40 81 08 11", "25 avenue de l’Eau", "44000", "Nantes", "Semi-remorque");

        db.Dossiers.AddRange(
            D("CF-2026-1054", "Dupont Piscines", "Couverture à barres", "Couvertures", "Thomas Martin", "02/10/2026 14:12", StatutDossier.AValider, "CHANTIER-DUP-1054", "45001054"),
            d1051,
            d1048,
            D("DV-2026-1044", "Aqua Services", "Rollin", "Volet immergé", "Sophie Caron", "02/10/2026 10:05", StatutDossier.Envoye, "AQUA-NANTES"),
            D("CF-2026-1037", "Aqua Services", "Rollin", "Volet", "Sophie Caron", "01/10/2026 16:22", StatutDossier.EnAttente, "AQUA-1037", "45001037"),
            d1030,
            D("CF-2026-1021", "Piscinas Atlântico", "Liner sur mesure", "Liner", "Julien Dubois", "30/09/2026 09:40", StatutDossier.Validee, "ATL-LISBOA", "45001021"),
            D("DV-2026-1008", "Blue Horizon", "Moon", "Volet hors-sol", "Laura Dupuis", "29/09/2026 15:26", StatutDossier.Envoye, "BH-BRUXELLES"),
            D("CF-2026-0996", "Blue Horizon", "Couverture d'hivernage", "Couverture d'hivernage", "Thomas Martin", "28/09/2026 13:17", StatutDossier.Erreur, "BH-0996", "45000996"),
            w2107,
            w2108);

        await db.SaveChangesAsync(annulation);
    }
}
