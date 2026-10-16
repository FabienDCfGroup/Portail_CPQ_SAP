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

        await db.SaveChangesAsync(annulation);
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
        // Pas d'accréditation initiale : les clients viennent de l'ERP, l'administrateur les accrédite dans le portail.
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
}
