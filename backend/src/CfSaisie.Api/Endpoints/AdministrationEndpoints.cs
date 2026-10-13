using CfSaisie.Api.Services;
using CfSaisie.Cpq;
using CfSaisie.Data;
using CfSaisie.Sap;
using Microsoft.EntityFrameworkCore;

namespace CfSaisie.Api.Endpoints;

public sealed record ConfigurateurDto(int Id, string Nom, string? ProductionSite, bool Actif, int NombreProduits);
public sealed record ConfigurateurSaisieDto(string? Nom, string? ProductionSite, bool Actif);
public sealed record FamilleDto(int Id, string Nom, string? Image, bool Actif, int NombreProduits);
public sealed record FamilleSaisieDto(string? Nom, string? Image, bool Actif);
public sealed record ProduitDto(int Id, string Nom, string? Description, string? Image, string? LienConfigurateur, int ConfigurateurId, string Configurateur, int? FamilleId, string? Famille);
public sealed record ProduitSaisieDto(string? Nom, int ConfigurateurId, string? LienConfigurateur, string? Description, string? Image);
public sealed record AssociationDto(int? FamilleId);
public sealed record AccreditationClientDto(string CodeClient, string RaisonSociale, IReadOnlyList<int> Produits);
public sealed record AccreditationSaisieDto(IReadOnlyList<int> Produits);
public sealed record CommentaireFamilleSaisieDto(string? Nom, int Ordre);
public sealed record CommentaireModeleSaisieDto(int FamilleId, int Ordre, string? Texte);
public sealed record DeplacementDto(int Sens);
public sealed record MessageSaisieDto(string? Audience, string? Titre, string? Corps);
public sealed record UtilisateurDto(int Id, string Nom, string Role, string Statut);

/// <summary>
/// Écrans d'administration (rôle Administrateur) : catalogue, accréditations, messages, utilisateurs.
/// La gestion des configurateurs est réservée au super administrateur.
/// </summary>
public static class AdministrationEndpoints
{
    public static void MapAdministration(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin").AddEndpointFilter(async (contexte, suivant) =>
            Roles.EstAdministrateur(contexte.HttpContext)
                ? await suivant(contexte)
                : Results.Problem("Fonction réservée au rôle Administrateur.", statusCode: StatusCodes.Status403Forbidden));

        // ----- Configurateurs : lecture pour l'administrateur (choix du configurateur d'un produit) -----
        admin.MapGet("/configurateurs", ListerConfigurateurs);

        // Écriture réservée au super administrateur : le nom doit être celui déclaré dans le Middleware.
        var configurateurs = admin.MapGroup("/configurateurs").AddEndpointFilter(async (contexte, suivant) =>
            Roles.EstSuperAdministrateur(contexte.HttpContext)
                ? await suivant(contexte)
                : Results.Problem("Fonction réservée au rôle Super administrateur.", statusCode: StatusCodes.Status403Forbidden));

        configurateurs.MapPost("", async (ConfigurateurSaisieDto s, CfSaisieContext db, CancellationToken ct) =>
        {
            var nom = await VerifierConfigurateur(s, null, db, ct);
            db.Configurateurs.Add(new Configurateur { Nom = nom, ProductionSite = Texte(s.ProductionSite), Actif = s.Actif });
            await db.SaveChangesAsync(ct);
            return await ListerConfigurateurs(db, ct);
        });

        configurateurs.MapPut("/{id:int}", async (int id, ConfigurateurSaisieDto s, CfSaisieContext db, CancellationToken ct) =>
        {
            var configurateur = await db.Configurateurs.FindAsync([id], ct) ?? throw new RequeteInvalideException("Configurateur introuvable");
            var nom = await VerifierConfigurateur(s, id, db, ct);
            configurateur.Nom = nom;
            configurateur.ProductionSite = Texte(s.ProductionSite);
            configurateur.Actif = s.Actif;
            await db.SaveChangesAsync(ct);
            return await ListerConfigurateurs(db, ct);
        });

        configurateurs.MapPost("/{id:int}/basculer", async (int id, CfSaisieContext db, CancellationToken ct) =>
        {
            var configurateur = await db.Configurateurs.FindAsync([id], ct) ?? throw new RequeteInvalideException("Configurateur introuvable");
            configurateur.Actif = !configurateur.Actif;
            await db.SaveChangesAsync(ct);
            return await ListerConfigurateurs(db, ct);
        });

        // Un configurateur utilisé par un produit ne peut pas être supprimé : il faut le désactiver.
        configurateurs.MapDelete("/{id:int}", async (int id, CfSaisieContext db, CancellationToken ct) =>
        {
            var configurateur = await db.Configurateurs.FindAsync([id], ct) ?? throw new RequeteInvalideException("Configurateur introuvable");
            var produits = await db.Produits.CountAsync(p => p.ConfigurateurId == id, ct);
            if (produits > 0)
            {
                throw new RequeteInvalideException($"{configurateur.Nom} est utilisé par {produits} produit(s) : désactivez-le plutôt que de le supprimer");
            }
            db.Configurateurs.Remove(configurateur);
            await db.SaveChangesAsync(ct);
            return await ListerConfigurateurs(db, ct);
        });

        // ----- Familles -----
        admin.MapGet("/familles", ListerFamilles);

        admin.MapPost("/familles", async (FamilleSaisieDto s, CfSaisieContext db, CancellationToken ct) =>
        {
            var nom = Obligatoire(s.Nom, "Le nom de la famille est obligatoire");
            if (await db.Familles.AnyAsync(f => f.Nom == nom, ct)) throw new RequeteInvalideException("Cette famille existe déjà");
            db.Familles.Add(new Famille { Nom = nom, Image = Texte(s.Image), Actif = s.Actif });
            await db.SaveChangesAsync(ct);
            return await ListerFamilles(db, ct);
        });

        admin.MapPut("/familles/{id:int}", async (int id, FamilleSaisieDto s, CfSaisieContext db, CancellationToken ct) =>
        {
            var famille = await db.Familles.FindAsync([id], ct) ?? throw new RequeteInvalideException("Famille introuvable");
            var nom = Obligatoire(s.Nom, "Le nom de la famille est obligatoire");
            if (await db.Familles.AnyAsync(f => f.Id != id && f.Nom == nom, ct)) throw new RequeteInvalideException("Cette famille existe déjà");
            famille.Nom = nom;
            famille.Image = Texte(s.Image);
            famille.Actif = s.Actif;
            await db.SaveChangesAsync(ct);
            return await ListerFamilles(db, ct);
        });

        admin.MapPost("/familles/{id:int}/basculer", async (int id, CfSaisieContext db, CancellationToken ct) =>
        {
            var famille = await db.Familles.FindAsync([id], ct) ?? throw new RequeteInvalideException("Famille introuvable");
            famille.Actif = !famille.Actif;
            await db.SaveChangesAsync(ct);
            return await ListerFamilles(db, ct);
        });

        // Comme la maquette : supprimer une famille supprime ses produits (et donc leurs accréditations).
        admin.MapDelete("/familles/{id:int}", async (int id, CfSaisieContext db, CancellationToken ct) =>
        {
            var famille = await db.Familles.Include(f => f.Produits).FirstOrDefaultAsync(f => f.Id == id, ct)
                ?? throw new RequeteInvalideException("Famille introuvable");
            db.Produits.RemoveRange(famille.Produits);
            db.Familles.Remove(famille);
            await db.SaveChangesAsync(ct);
            return await ListerFamilles(db, ct);
        });

        // ----- Produits -----
        admin.MapGet("/produits", ListerProduits);

        admin.MapPost("/produits", async (ProduitSaisieDto s, CfSaisieContext db, CancellationToken ct) =>
        {
            var nom = await VerifierProduit(s, null, db, ct);
            db.Produits.Add(new Produit
            {
                Nom = nom,
                ConfigurateurId = s.ConfigurateurId,
                LienConfigurateur = Texte(s.LienConfigurateur),
                Description = Texte(s.Description),
                Image = Texte(s.Image),
            });
            await db.SaveChangesAsync(ct);
            return await ListerProduits(db, ct);
        });

        admin.MapPut("/produits/{id:int}", async (int id, ProduitSaisieDto s, CfSaisieContext db, CancellationToken ct) =>
        {
            var produit = await db.Produits.FindAsync([id], ct) ?? throw new RequeteInvalideException("Produit introuvable");
            produit.Nom = await VerifierProduit(s, id, db, ct);
            produit.ConfigurateurId = s.ConfigurateurId;
            produit.LienConfigurateur = Texte(s.LienConfigurateur);
            produit.Description = Texte(s.Description);
            produit.Image = Texte(s.Image);
            await db.SaveChangesAsync(ct);
            return await ListerProduits(db, ct);
        });

        admin.MapDelete("/produits/{id:int}", async (int id, CfSaisieContext db, CancellationToken ct) =>
        {
            var produit = await db.Produits.FindAsync([id], ct) ?? throw new RequeteInvalideException("Produit introuvable");
            db.Produits.Remove(produit);
            await db.SaveChangesAsync(ct);
            return await ListerProduits(db, ct);
        });

        admin.MapPut("/produits/{id:int}/famille", async (int id, AssociationDto s, CfSaisieContext db, CancellationToken ct) =>
        {
            var produit = await db.Produits.FindAsync([id], ct) ?? throw new RequeteInvalideException("Choisissez un produit");
            if (s.FamilleId is null || !await db.Familles.AnyAsync(f => f.Id == s.FamilleId, ct)) throw new RequeteInvalideException("Choisissez une famille");
            produit.FamilleId = s.FamilleId;
            await db.SaveChangesAsync(ct);
            return await ListerProduits(db, ct);
        });

        // ----- Accréditations clients / produits -----
        admin.MapGet("/accreditations", ListerAccreditations);

        admin.MapPut("/accreditations/{code}", async (string code, AccreditationSaisieDto s, CfSaisieContext db, ISapClientService sap, CancellationToken ct) =>
        {
            if (await sap.LireAsync(code, ct) is null) throw new RequeteInvalideException($"Client {code} introuvable dans SAP.");
            var existantes = await db.Accreditations.Where(a => a.CodeClient == code).ToListAsync(ct);
            db.Accreditations.RemoveRange(existantes);
            var produits = await db.Produits.Where(p => s.Produits.Contains(p.Id)).Select(p => p.Id).ToListAsync(ct);
            db.Accreditations.AddRange(produits.Select(p => new Accreditation { CodeClient = code, ProduitId = p }));
            await db.SaveChangesAsync(ct);
            return await ListerAccreditations(db, sap, ct);
        });

        // ----- Bibliothèque de commentaires -----
        admin.MapPost("/commentaires/familles", async (CommentaireFamilleSaisieDto s, CfSaisieContext db, CancellationToken ct) =>
        {
            db.CommentaireFamilles.Add(new CommentaireFamille { Nom = Obligatoire(s.Nom, "Saisissez le nom de la famille"), Ordre = Math.Max(1, s.Ordre) });
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        admin.MapPut("/commentaires/familles/{id:int}", async (int id, CommentaireFamilleSaisieDto s, CfSaisieContext db, CancellationToken ct) =>
        {
            var famille = await db.CommentaireFamilles.FindAsync([id], ct) ?? throw new RequeteInvalideException("Famille introuvable");
            famille.Nom = Obligatoire(s.Nom, "Saisissez le nom de la famille");
            famille.Ordre = Math.Max(1, s.Ordre);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        admin.MapDelete("/commentaires/familles/{id:int}", async (int id, CfSaisieContext db, CancellationToken ct) =>
        {
            await db.CommentaireFamilles.Where(f => f.Id == id).ExecuteDeleteAsync(ct);
            return Results.NoContent();
        });

        // Échange l'ordre avec la famille voisine (flèches ↑ ↓ de la maquette).
        admin.MapPost("/commentaires/familles/{id:int}/deplacer", async (int id, DeplacementDto s, CfSaisieContext db, CancellationToken ct) =>
        {
            var familles = (await db.CommentaireFamilles.ToListAsync(ct)).OrderBy(f => f.Ordre).ThenBy(f => f.Nom).ToList();
            Echanger(familles, familles.FindIndex(f => f.Id == id), s.Sens, f => f.Ordre, (f, o) => f.Ordre = o);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        admin.MapPost("/commentaires/modeles", async (CommentaireModeleSaisieDto s, CfSaisieContext db, CancellationToken ct) =>
        {
            if (!await db.CommentaireFamilles.AnyAsync(f => f.Id == s.FamilleId, ct)) throw new RequeteInvalideException("Choisissez une famille et saisissez le message");
            db.CommentaireModeles.Add(new CommentaireModele
            {
                FamilleId = s.FamilleId,
                Ordre = Math.Max(1, s.Ordre),
                Texte = Obligatoire(s.Texte, "Choisissez une famille et saisissez le message"),
            });
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        admin.MapPut("/commentaires/modeles/{id:int}", async (int id, CommentaireModeleSaisieDto s, CfSaisieContext db, CancellationToken ct) =>
        {
            var modele = await db.CommentaireModeles.FindAsync([id], ct) ?? throw new RequeteInvalideException("Message introuvable");
            if (!await db.CommentaireFamilles.AnyAsync(f => f.Id == s.FamilleId, ct)) throw new RequeteInvalideException("Choisissez une famille et saisissez le message");
            modele.FamilleId = s.FamilleId;
            modele.Ordre = Math.Max(1, s.Ordre);
            modele.Texte = Obligatoire(s.Texte, "Choisissez une famille et saisissez le message");
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        admin.MapDelete("/commentaires/modeles/{id:int}", async (int id, CfSaisieContext db, CancellationToken ct) =>
        {
            await db.CommentaireModeles.Where(m => m.Id == id).ExecuteDeleteAsync(ct);
            return Results.NoContent();
        });

        admin.MapPost("/commentaires/modeles/{id:int}/deplacer", async (int id, DeplacementDto s, CfSaisieContext db, CancellationToken ct) =>
        {
            var modele = await db.CommentaireModeles.FindAsync([id], ct) ?? throw new RequeteInvalideException("Message introuvable");
            var modeles = (await db.CommentaireModeles.Where(m => m.FamilleId == modele.FamilleId).ToListAsync(ct)).OrderBy(m => m.Ordre).ThenBy(m => m.Texte).ToList();
            Echanger(modeles, modeles.FindIndex(m => m.Id == id), s.Sens, m => m.Ordre, (m, o) => m.Ordre = o);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        // ----- Messages du tableau de bord -----
        admin.MapGet("/messages", async (CfSaisieContext db, CancellationToken ct) =>
            await db.Messages.OrderByDescending(m => m.DateCreation).Select(m => new MessageDto(m.Id, m.Audience, m.Titre, m.Corps, m.DateCreation)).ToListAsync(ct));

        admin.MapPost("/messages", async (MessageSaisieDto s, CfSaisieContext db, TimeProvider horloge, CancellationToken ct) =>
        {
            db.Messages.Add(new MessageTableauBord
            {
                Audience = Texte(s.Audience) ?? "Tous",
                Titre = Obligatoire(s.Titre, "Complétez le titre et le message"),
                Corps = Obligatoire(s.Corps, "Complétez le titre et le message"),
                DateCreation = horloge.GetLocalNow().DateTime,
            });
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        admin.MapDelete("/messages/{id:int}", async (int id, CfSaisieContext db, CancellationToken ct) =>
        {
            await db.Messages.Where(m => m.Id == id).ExecuteDeleteAsync(ct);
            return Results.NoContent();
        });

        // ----- Utilisateurs -----
        admin.MapGet("/utilisateurs", async (CfSaisieContext db, CancellationToken ct) =>
            await db.Utilisateurs.OrderBy(u => u.Id).Select(u => new UtilisateurDto(u.Id, u.Nom, u.Role, u.Statut)).ToListAsync(ct));
    }

    private static async Task<List<FamilleDto>> ListerFamilles(CfSaisieContext db, CancellationToken ct) =>
        await db.Familles.OrderBy(f => f.Id).Select(f => new FamilleDto(f.Id, f.Nom, f.Image, f.Actif, f.Produits.Count)).ToListAsync(ct);

    private static async Task<List<ProduitDto>> ListerProduits(CfSaisieContext db, CancellationToken ct) =>
        await db.Produits.OrderBy(p => p.Id)
            .Select(p => new ProduitDto(p.Id, p.Nom, p.Description, p.Image, p.LienConfigurateur, p.ConfigurateurId, p.Configurateur.Nom, p.FamilleId,
                p.Famille != null ? p.Famille.Nom : null))
            .ToListAsync(ct);

    private static async Task<List<AccreditationClientDto>> ListerAccreditations(CfSaisieContext db, ISapClientService sap, CancellationToken ct)
    {
        var clients = await sap.RechercherAsync(null, 500, ct);
        var accreditations = await db.Accreditations.ToListAsync(ct);
        return clients
            .Select(c => new AccreditationClientDto(c.Code, c.RaisonSociale, accreditations.Where(a => a.CodeClient == c.Code).Select(a => a.ProduitId).ToList()))
            .ToList();
    }

    private static async Task<List<ConfigurateurDto>> ListerConfigurateurs(CfSaisieContext db, CancellationToken ct) =>
        await db.Configurateurs.OrderBy(c => c.Nom)
            .Select(c => new ConfigurateurDto(c.Id, c.Nom, c.ProductionSite, c.Actif, db.Produits.Count(p => p.ConfigurateurId == c.Id)))
            .ToListAsync(ct);

    private static async Task<string> VerifierConfigurateur(ConfigurateurSaisieDto s, int? id, CfSaisieContext db, CancellationToken ct)
    {
        var nom = Obligatoire(s.Nom, "Le nom du configurateur est obligatoire");
        if (!NomConfigurateur.EstValide(nom))
        {
            throw new RequeteInvalideException("Nom invalide : lettres, chiffres et « _ » uniquement, sans espace ni « _ » en début ou en fin (ex. CHIV_Initial)");
        }
        // Collation SQL Server insensible à la casse : « volet » et « Volet » sont le même configurateur.
        if (await db.Configurateurs.AnyAsync(c => c.Id != id && c.Nom == nom, ct)) throw new RequeteInvalideException("Ce configurateur existe déjà");
        if (Texte(s.ProductionSite) is { Length: > 50 }) throw new RequeteInvalideException("Le site de production ne doit pas dépasser 50 caractères");
        return nom;
    }

    private static async Task<string> VerifierProduit(ProduitSaisieDto s, int? id, CfSaisieContext db, CancellationToken ct)
    {
        var nom = Obligatoire(s.Nom, "Produit et configurateur sont obligatoires");
        if (!await db.Configurateurs.AnyAsync(c => c.Id == s.ConfigurateurId, ct)) throw new RequeteInvalideException("Produit et configurateur sont obligatoires");
        if (Texte(s.LienConfigurateur) is { } lien && !lien.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !lien.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new RequeteInvalideException("Le lien configurateur doit commencer par http:// ou https://");
        }
        if (await db.Produits.AnyAsync(p => p.Id != id && p.Nom == nom, ct)) throw new RequeteInvalideException("Ce produit existe déjà");
        return nom;
    }

    private static void Echanger<T>(List<T> liste, int i, int sens, Func<T, int> ordre, Action<T, int> definir)
    {
        var j = i + Math.Sign(sens);
        if (i < 0 || j < 0 || j >= liste.Count) return;
        var (a, b) = (liste[i], liste[j]);
        var (oa, ob) = (ordre(a), ordre(b));
        // Ordres identiques : on force un écart pour que le déplacement soit visible.
        if (oa == ob) ob = oa + Math.Sign(sens);
        definir(a, ob);
        definir(b, oa);
    }

    private static string Obligatoire(string? valeur, string message) =>
        string.IsNullOrWhiteSpace(valeur) ? throw new RequeteInvalideException(message) : valeur.Trim();

    private static string? Texte(string? valeur) => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();
}
