using System.Text.Json;
using SmartDataExchangeDefines.Models;

namespace CfSaisie.Cpq.Tests;

public class OpenRequestBuilderTests
{
    private static readonly DateTimeOffset Maintenant = new(2026, 10, 9, 8, 30, 0, TimeSpan.Zero);

    private static readonly OpenRequestBuilder Builder = new(new HorlogeFixe(Maintenant));

    private static readonly string DossierAttendu = Path.Combine(AppContext.BaseDirectory, "Attendu");

    public static TheoryData<string> CasAttendus()
    {
        var cas = new TheoryData<string>();
        foreach (var fichier in Directory.GetFiles(DossierAttendu, "*.json").Order())
        {
            cas.Add(Path.GetFileName(fichier));
        }
        return cas;
    }

    /// <summary>
    /// Compare la requête produite à la requête attendue (voir Attendu/README.md) :
    /// champs listés = valeur attendue, autres champs = valeur d'un new OpenRequest(), DateValidite exclue.
    /// </summary>
    [Theory]
    [MemberData(nameof(CasAttendus))]
    public void Construire_ProduitLaRequeteAttendue(string fichier)
    {
        using var cas = JsonDocument.Parse(File.ReadAllText(Path.Combine(DossierAttendu, fichier)));
        var entree = cas.RootElement.GetProperty("entree");
        var attendu = cas.RootElement.GetProperty("requeteAttendue");

        var requete = Builder.Construire(LireDemande(entree), LireProfil(entree), LireEnvironnement(entree));

        using var produit = JsonDocument.Parse(OpenRequestSerializer.Serialiser(requete));
        // DateValidite doit être renseignée : DateTime.MinValue n'est pas sérialisable sur un fuseau en avance sur UTC.
        using var parDefaut = JsonDocument.Parse(OpenRequestSerializer.Serialiser(new OpenRequest { DateValidite = Maintenant.UtcDateTime }));
        var ecarts = new List<string>();

        foreach (var champ in attendu.EnumerateObject())
        {
            if (!produit.RootElement.TryGetProperty(champ.Name, out _))
            {
                ecarts.Add($"{champ.Name} : champ inconnu dans OpenRequest");
            }
        }

        foreach (var champ in produit.RootElement.EnumerateObject().Where(c => c.Name != nameof(OpenRequest.DateValidite)))
        {
            var valeurAttendue = attendu.TryGetProperty(champ.Name, out var a) ? a : parDefaut.RootElement.GetProperty(champ.Name);
            if (!Equals(Valeur(valeurAttendue), Valeur(champ.Value)))
            {
                ecarts.Add($"{champ.Name} : attendu {valeurAttendue.GetRawText()}, obtenu {champ.Value.GetRawText()}");
            }
        }

        Assert.True(ecarts.Count == 0, string.Join(Environment.NewLine, ecarts));
    }

    [Fact]
    public void Construire_DateValiditeEstLHeureLocaleDeLAppel()
    {
        var requete = Builder.Construire(DemandeCreation(), ProfilErp(), Recette());

        Assert.Equal(DateTimeKind.Local, requete.DateValidite.Kind);
        Assert.Equal(Maintenant.UtcDateTime, requete.DateValidite.ToUniversalTime());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Construire_SansConfigurateur_EstRefuse(string nom)
    {
        var demande = DemandeCreation() with { Configurateur = new ConfigurateurCpq(nom) };

        Assert.Throws<DemandeLancementInvalideException>(() => Builder.Construire(demande, ProfilErp(), Recette()));
    }

    [Theory]
    [InlineData(ActionConfigurateur.Modification, null)]
    [InlineData(ActionConfigurateur.Consultation, "")]
    public void Construire_SansConfigIdHorsCreation_EstRefuse(ActionConfigurateur action, string? configId)
    {
        var demande = DemandeCreation() with { Action = action, ConfigId = configId };

        Assert.Throws<DemandeLancementInvalideException>(() => Builder.Construire(demande, ProfilErp(), Recette()));
    }

    [Fact]
    public void ProfilMiddleware_NAffichePasLeMotDePasse()
    {
        var profil = new ProfilMiddleware("cfgAxDelBrece@techform.fr", "secret-a-ne-pas-journaliser", ModeErp: true);

        Assert.DoesNotContain("secret-a-ne-pas-journaliser", profil.ToString());
    }

    private static DemandeLancement DemandeCreation() => new()
    {
        Action = ActionConfigurateur.Creation,
        TypeDocument = TypeDocument.Devis,
        Configurateur = new ConfigurateurCpq("Volet", "DEL_BRECE"),
        CodeClient = "102466",
        Langue = LangueConfigurateur.Francais,
    };

    private static ProfilMiddleware ProfilErp() => new("cfgAxDelBrece@techform.fr", "***", ModeErp: true);

    private static EnvironnementMiddleware Recette() => new("test.cfgroup.smartconfigurator.fr");

    private static ProfilMiddleware LireProfil(JsonElement entree)
    {
        var profil = entree.GetProperty("profil");
        return new ProfilMiddleware(
            profil.GetProperty("identifiant").GetString()!,
            profil.GetProperty("motDePasse").GetString()!,
            profil.GetProperty("modeErp").GetBoolean());
    }

    private static EnvironnementMiddleware LireEnvironnement(JsonElement entree) =>
        new(entree.GetProperty("environnement").GetProperty("serveur").GetString()!);

    private static DemandeLancement LireDemande(JsonElement entree)
    {
        var configurateur = entree.GetProperty("configurateur");
        return new DemandeLancement
        {
            Action = Enum.Parse<ActionConfigurateur>(entree.GetProperty("action").GetString()!),
            TypeDocument = Enum.Parse<TypeDocument>(entree.GetProperty("typeDocument").GetString()!),
            Configurateur = new ConfigurateurCpq(
                configurateur.GetProperty("nom").GetString()!,
                configurateur.GetProperty("productionSite").GetString()),
            CodeClient = Texte(entree, "codeClient"),
            ConfigId = Texte(entree, "configId"),
            Langue = Texte(entree, "langue") is { } langue ? Enum.Parse<LangueConfigurateur>(langue) : null,
            ReferenceChantier = Texte(entree, "referenceChantier"),
            Email = Texte(entree, "email"),
            LangueNotice = Texte(entree, "langueNotice"),
        };
    }

    private static string? Texte(JsonElement element, string nom) =>
        element.TryGetProperty(nom, out var valeur) ? valeur.GetString() : null;

    private static object? Valeur(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => element.GetRawText(),
    };

    private sealed class HorlogeFixe(DateTimeOffset maintenant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => maintenant;
    }
}
