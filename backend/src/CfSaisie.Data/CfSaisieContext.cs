using Microsoft.EntityFrameworkCore;

namespace CfSaisie.Data;

public class CfSaisieContext(DbContextOptions<CfSaisieContext> options) : DbContext(options)
{
    public DbSet<Configurateur> Configurateurs => Set<Configurateur>();
    public DbSet<Famille> Familles => Set<Famille>();
    public DbSet<Produit> Produits => Set<Produit>();
    public DbSet<Accreditation> Accreditations => Set<Accreditation>();
    public DbSet<Dossier> Dossiers => Set<Dossier>();
    public DbSet<CommentaireFamille> CommentaireFamilles => Set<CommentaireFamille>();
    public DbSet<CommentaireModele> CommentaireModeles => Set<CommentaireModele>();
    public DbSet<MessageTableauBord> Messages => Set<MessageTableauBord>();
    public DbSet<JournalConnexion> JournalConnexions => Set<JournalConnexion>();
    public DbSet<Langue> Langues => Set<Langue>();

    // Schéma « erp » : alimenté par l'ERP, en lecture seule pour le portail.
    public DbSet<ClientErp> ClientsErp => Set<ClientErp>();
    public DbSet<AdresseClientErp> AdressesClientsErp => Set<AdresseClientErp>();
    public DbSet<ModeLivraisonErp> ModesLivraisonErp => Set<ModeLivraisonErp>();
    public DbSet<AgenceRetraitErp> AgencesRetraitErp => Set<AgenceRetraitErp>();
    public DbSet<CommandeErp> CommandesErp => Set<CommandeErp>();

    public const string SchemaErp = "erp";

    protected override void OnModelCreating(ModelBuilder modele)
    {
        modele.Entity<Configurateur>(e =>
        {
            e.Property(x => x.Nom).HasMaxLength(100);
            e.HasIndex(x => x.Nom).IsUnique();
            e.Property(x => x.ProductionSite).HasMaxLength(50);
        });

        modele.Entity<Famille>(e =>
        {
            e.Property(x => x.Nom).HasMaxLength(150);
            e.HasIndex(x => x.Nom).IsUnique();
        });

        modele.Entity<Produit>(e =>
        {
            e.Property(x => x.Nom).HasMaxLength(150);
            e.HasIndex(x => x.Nom).IsUnique();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.LienConfigurateur).HasMaxLength(500);
            e.HasOne(x => x.Configurateur).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Famille).WithMany(f => f.Produits).OnDelete(DeleteBehavior.SetNull);
        });

        modele.Entity<Accreditation>(e =>
        {
            e.HasKey(x => new { x.CodeClient, x.ProduitId });
            e.Property(x => x.CodeClient).HasMaxLength(20);
            e.HasOne(x => x.Produit).WithMany().OnDelete(DeleteBehavior.Cascade);
        });

        modele.Entity<Dossier>(e =>
        {
            e.Property(x => x.Reference).HasMaxLength(30);
            e.HasIndex(x => x.Reference).IsUnique();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Statut).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Origine).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.CodeClient).HasMaxLength(20);
            e.Property(x => x.NomClient).HasMaxLength(200);
            e.Property(x => x.NomProduit).HasMaxLength(150);
            e.Property(x => x.NomFamille).HasMaxLength(150);
            e.Property(x => x.CreePar).HasMaxLength(100);
            e.Property(x => x.ConfigId).HasMaxLength(100);
            e.Property(x => x.Environnement).HasMaxLength(20);
            e.Property(x => x.NumeroSap).HasMaxLength(30);
            e.HasOne(x => x.Produit).WithMany().OnDelete(DeleteBehavior.SetNull);
        });

        modele.Entity<CommentaireModele>(e =>
            e.HasOne(x => x.Famille).WithMany(f => f.Modeles).OnDelete(DeleteBehavior.Cascade));

        modele.Entity<JournalConnexion>(e =>
        {
            e.ToTable("JournalConnexions");
            e.Property(x => x.Resultat).HasMaxLength(30);
            e.Property(x => x.ObjetId).HasMaxLength(50);
            e.Property(x => x.Compte).HasMaxLength(256);
            e.Property(x => x.Nom).HasMaxLength(200);
            e.Property(x => x.Role).HasMaxLength(30);
            e.Property(x => x.Message).HasMaxLength(500);
            e.Property(x => x.AdresseIp).HasMaxLength(64);
            e.Property(x => x.Navigateur).HasMaxLength(512);
            e.HasIndex(x => x.Date);
            e.HasIndex(x => x.Compte);
        });

        modele.Entity<Langue>(e =>
        {
            e.HasKey(x => x.Code);
            e.Property(x => x.Code).HasMaxLength(5);
            e.Property(x => x.Libelle).HasMaxLength(50);
            // Langues initiales : celles gérées par les configurateurs, PT désactivée (aucun configurateur traduit en portugais).
            e.HasData(
                new Langue { Code = "fr", Libelle = "Français", Actif = true, Ordre = 1 },
                new Langue { Code = "en", Libelle = "English", Actif = true, Ordre = 2 },
                new Langue { Code = "de", Libelle = "Deutsch", Actif = true, Ordre = 3 },
                new Langue { Code = "nl", Libelle = "Nederlands", Actif = true, Ordre = 4 },
                new Langue { Code = "pt", Libelle = "Português", Actif = false, Ordre = 5 });
        });

        ConfigurerErp(modele);
    }

    /// <summary>
    /// Tables alimentées par l'ERP. Les valeurs par défaut SQL (Actif, EstVic, EstPrincipale, DateMaj)
    /// simplifient les insertions du flux ; ValueGeneratedNever : le portail, lui, n'écrit jamais ces valeurs implicitement.
    /// </summary>
    private static void ConfigurerErp(ModelBuilder modele)
    {
        modele.Entity<ClientErp>(e =>
        {
            e.ToTable("Clients", SchemaErp);
            e.HasKey(x => x.CodeClient);
            e.Property(x => x.CodeClient).HasMaxLength(20);
            e.Property(x => x.RaisonSociale).HasMaxLength(200);
            e.Property(x => x.Pays).HasMaxLength(100);
            e.Property(x => x.Langue).HasMaxLength(5);
            e.Property(x => x.ModePaiement).HasMaxLength(50);
            e.Property(x => x.EstVic).HasDefaultValue(false).ValueGeneratedNever();
            e.Property(x => x.Actif).HasDefaultValue(true).ValueGeneratedNever();
            e.Property(x => x.DateMaj).HasDefaultValueSql("SYSDATETIME()");
            e.HasIndex(x => x.RaisonSociale);
        });

        modele.Entity<AdresseClientErp>(e =>
        {
            e.ToTable("AdressesClients", SchemaErp);
            e.HasKey(x => x.IdAdresse);
            e.Property(x => x.IdAdresse).HasMaxLength(50);
            e.Property(x => x.CodeClient).HasMaxLength(20);
            e.Property(x => x.Libelle).HasMaxLength(100);
            e.Property(x => x.EstPrincipale).HasDefaultValue(false).ValueGeneratedNever();
            e.Property(x => x.NomDestinataire).HasMaxLength(200);
            e.Property(x => x.Telephone).HasMaxLength(30);
            e.Property(x => x.Adresse).HasMaxLength(250);
            e.Property(x => x.ComplementAdresse).HasMaxLength(250);
            e.Property(x => x.CodePostal).HasMaxLength(20);
            e.Property(x => x.Ville).HasMaxLength(100);
            e.Property(x => x.Pays).HasMaxLength(100);
            e.Property(x => x.DateMaj).HasDefaultValueSql("SYSDATETIME()");
            // Supprimer un client supprime ses adresses.
            e.HasOne(x => x.Client).WithMany(c => c.Adresses).HasForeignKey(x => x.CodeClient).OnDelete(DeleteBehavior.Cascade);
            // Recherche des adresses d'un client ; et une seule adresse principale par client (index unique filtré).
            e.HasIndex(x => x.CodeClient, "IX_AdressesClients_CodeClient");
            e.HasIndex(x => x.CodeClient, "UX_AdressesClients_AdressePrincipale").IsUnique().HasFilter("[EstPrincipale] = 1");
        });

        modele.Entity<ModeLivraisonErp>(e =>
        {
            e.ToTable("ModesLivraison", SchemaErp);
            e.HasKey(x => x.Code);
            e.Property(x => x.Code).HasMaxLength(20);
            e.Property(x => x.Libelle).HasMaxLength(100);
            e.Property(x => x.Actif).HasDefaultValue(true).ValueGeneratedNever();
            e.Property(x => x.DateMaj).HasDefaultValueSql("SYSDATETIME()");
        });

        modele.Entity<AgenceRetraitErp>(e =>
        {
            e.ToTable("AgencesRetrait", SchemaErp);
            e.HasKey(x => x.CodeAgence);
            e.Property(x => x.CodeAgence).HasMaxLength(20);
            e.Property(x => x.Libelle).HasMaxLength(150);
            e.Property(x => x.Region).HasMaxLength(100);
            e.Property(x => x.Localisation).HasMaxLength(250);
            e.Property(x => x.Actif).HasDefaultValue(true).ValueGeneratedNever();
            e.Property(x => x.DateMaj).HasDefaultValueSql("SYSDATETIME()");
        });

        modele.Entity<CommandeErp>(e =>
        {
            e.ToTable("Commandes", SchemaErp);
            e.HasKey(x => x.NumeroCommande);
            e.Property(x => x.NumeroCommande).HasMaxLength(30);
            e.Property(x => x.CodeClient).HasMaxLength(20);
            e.Property(x => x.ReferenceCommandeClient).HasMaxLength(100);
            e.Property(x => x.ReferencePortail).HasMaxLength(30);
            e.Property(x => x.ConfigId).HasMaxLength(100);
            e.Property(x => x.StatutErp).HasMaxLength(50);
            e.Property(x => x.DateMaj).HasDefaultValueSql("SYSDATETIME()");
            // L'historique des commandes est conservé : un client qui a des commandes ne peut pas être supprimé.
            e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.CodeClient).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ConfigId);
            e.HasIndex(x => x.ReferencePortail);
            e.HasIndex(x => x.DateCreation);
        });
    }
}
