using System.Net;
using System.Text;

namespace CfSaisie.Sap.Tests;

public class PlanningCcxClientTests
{
    private static readonly ParametresPlanningCcx Parametres = new()
    {
        Url = "https://test.n8n.fr.cf.group/webhook/planning-ccx",
        CleApi = "cle-de-test",
        NbJours = 90,
    };

    // Exemple au format du contrat d'interface (champs non utilisés par le portail inclus).
    private const string ReponseContrat = """
        {
          "genereLe": "2026-10-13T09:21:51.818Z",
          "societe": "cec",
          "site": "DEL_BRECE",
          "horizonJours": 90,
          "origines": ["AX"],
          "premiereDateDisponibleParCode": { "L": "2026-10-13", "LC": "2026-10-23" },
          "anomalies": [{ "type": "charge_sans_capacite", "detail": ["LC|2026-11-04"] }],
          "jours": [
            { "codeCCX": "L", "libelleCCX": "L. SIMP", "jour": "2026-10-13", "jourSemaine": "Tuesday", "dansNJours": 0,
              "capacite": 10, "charge": 23, "capaciteRestante": -13, "tauxChargePct": 230, "selectionnable": false,
              "motifIndisponible": "Complet", "confirmationRequise": false, "delaiMiniJoursOuvres": 0, "premiereDateAutorisee": "2026-10-13" },
            { "codeCCX": "L", "libelleCCX": "L. SIMP", "jour": "2026-10-17", "jourSemaine": "Saturday", "dansNJours": 4,
              "capacite": 0, "charge": 0, "capaciteRestante": 0, "tauxChargePct": null, "selectionnable": false,
              "motifIndisponible": "Atelier ferme", "confirmationRequise": false, "delaiMiniJoursOuvres": 0, "premiereDateAutorisee": "2026-10-13" }
          ]
        }
        """;

    [Fact]
    public async Task LireAsync_EnvoieLeSiteLHorizonEtLaCle()
    {
        var serveur = new ServeurFactice((HttpStatusCode.OK, ReponseContrat));

        await new PlanningCcxClient(new HttpClient(serveur), Parametres).LireAsync("DEL_BRECE");

        Assert.Equal(HttpMethod.Get, serveur.Methodes.Single());
        Assert.Equal("https://test.n8n.fr.cf.group/webhook/planning-ccx?site=DEL_BRECE&nbJours=90", serveur.Urls.Single());
        Assert.Equal("cle-de-test", serveur.Cles.Single());
    }

    [Fact]
    public async Task LireAsync_LitLaReponseDuContrat()
    {
        var serveur = new ServeurFactice((HttpStatusCode.OK, ReponseContrat));

        var reponse = await new PlanningCcxClient(new HttpClient(serveur), Parametres).LireAsync("DEL_BRECE");

        Assert.Equal(["AX"], reponse.Origines);
        Assert.Single(reponse.Anomalies!);
        var jour = reponse.Jours![0];
        Assert.Equal(new DateOnly(2026, 10, 13), jour.Jour);
        Assert.Equal(-13, jour.CapaciteRestante);
        Assert.False(jour.Selectionnable);
        Assert.Equal("Complet", jour.MotifIndisponible);
        Assert.Equal(new DateOnly(2026, 10, 13), jour.PremiereDateAutorisee);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("""{"site":"DEL_BRECE"}""")]
    public async Task LireAsync_200SansJours_EstUneErreur(string corps)
    {
        // Contrat : quand un nœud n8n échoue, la plateforme répond 200 sans corps.
        var serveur = new ServeurFactice((HttpStatusCode.OK, corps));

        await Assert.ThrowsAsync<PlanningCcxException>(() => new PlanningCcxClient(new HttpClient(serveur), Parametres).LireAsync("DEL_BRECE"));
    }

    [Fact]
    public async Task LireAsync_CleRefusee()
    {
        var serveur = new ServeurFactice((HttpStatusCode.Forbidden, ""));

        var erreur = await Assert.ThrowsAsync<PlanningCcxException>(() => new PlanningCcxClient(new HttpClient(serveur), Parametres).LireAsync("DEL_BRECE"));

        Assert.Contains("403", erreur.Message);
    }

    [Fact]
    public async Task LireAsync_Erreur500_ReessaieUneFois()
    {
        var serveur = new ServeurFactice((HttpStatusCode.InternalServerError, ""), (HttpStatusCode.OK, ReponseContrat));

        var reponse = await new PlanningCcxClient(new HttpClient(serveur), Parametres).LireAsync("DEL_BRECE");

        Assert.Equal(2, serveur.Urls.Count);
        Assert.NotEmpty(reponse.Jours!);
    }

    [Fact]
    public async Task LireAsync_Erreur500Persistante_EstUneErreur()
    {
        var serveur = new ServeurFactice((HttpStatusCode.InternalServerError, ""), (HttpStatusCode.InternalServerError, ""));

        await Assert.ThrowsAsync<PlanningCcxException>(() => new PlanningCcxClient(new HttpClient(serveur), Parametres).LireAsync("DEL_BRECE"));
        Assert.Equal(2, serveur.Urls.Count);
    }

    private sealed class ServeurFactice(params (HttpStatusCode Statut, string Corps)[] reponses) : HttpMessageHandler
    {
        private int _appel;

        public List<HttpMethod> Methodes { get; } = [];
        public List<string?> Urls { get; } = [];
        public List<string?> Cles { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage requete, CancellationToken annulation)
        {
            Methodes.Add(requete.Method);
            Urls.Add(requete.RequestUri?.ToString());
            Cles.Add(requete.Headers.TryGetValues("X-API-Key", out var v) ? v.Single() : null);
            var (statut, corps) = reponses[Math.Min(_appel++, reponses.Length - 1)];
            return Task.FromResult(new HttpResponseMessage(statut) { Content = new StringContent(corps, Encoding.UTF8, "application/json") });
        }
    }
}
