using System.Net;
using System.Text;
using SmartDataExchangeDefines.Models;

namespace CfSaisie.Cpq.Tests;

public class MiddlewareCpqClientTests
{
    private static readonly EnvironnementMiddleware Recette = new("test.cfgroup.smartconfigurator.fr");

    [Fact]
    public async Task OuvrirAsync_EnvoieLaRequeteEnPostJsonSurOpen()
    {
        var serveur = new ServeurFactice(HttpStatusCode.OK, """{"ConfigIdSimplifie":"Moon_00000001","Message":null,"Success":true,"Url":"https://cfg/moon"}""");
        var requete = Requete();

        await new MiddlewareCpqClient(new HttpClient(serveur)).OuvrirAsync(requete, Recette);

        Assert.Equal(HttpMethod.Post, serveur.Methode);
        Assert.Equal("https://test.cfgroup.smartconfigurator.fr/Cfg_Middleware/Service.svc/Open", serveur.Url);
        Assert.Equal("application/json", serveur.ContentType?.MediaType);
        Assert.Equal("utf-8", serveur.ContentType?.CharSet);
        Assert.Equal(OpenRequestSerializer.Serialiser(requete), serveur.Corps);
    }

    [Fact]
    public async Task OuvrirAsync_Succes_RenvoieUrlEtConfigId()
    {
        var serveur = new ServeurFactice(HttpStatusCode.OK, """{"ConfigIdSimplifie":"Moon_00000001","Message":"","Success":true,"Url":"https:\/\/cfg\/moon"}""");

        var resultat = await new MiddlewareCpqClient(new HttpClient(serveur)).OuvrirAsync(Requete(), Recette);

        Assert.True(resultat.PeutOuvrir);
        Assert.Equal("https://cfg/moon", resultat.Url);
        Assert.Equal("Moon_00000001", resultat.ConfigId);
    }

    [Fact]
    public async Task OuvrirAsync_SuccesAvecMessage_NePeutPasOuvrir()
    {
        var serveur = new ServeurFactice(HttpStatusCode.OK, """{"ConfigIdSimplifie":null,"Message":"Information","Success":true,"Url":null}""");

        var resultat = await new MiddlewareCpqClient(new HttpClient(serveur)).OuvrirAsync(Requete(), Recette);

        Assert.True(resultat.Succes);
        Assert.False(resultat.PeutOuvrir);
        Assert.Equal("Information", resultat.Message);
    }

    [Fact]
    public async Task OuvrirAsync_Echec_RenvoieLeMessageDuMiddleware()
    {
        var serveur = new ServeurFactice(HttpStatusCode.OK, """{"ConfigIdSimplifie":null,"Message":"Utilisateur inconnu","Success":false,"Url":null}""");

        var resultat = await new MiddlewareCpqClient(new HttpClient(serveur)).OuvrirAsync(Requete(), Recette);

        Assert.False(resultat.Succes);
        Assert.False(resultat.PeutOuvrir);
        Assert.Equal("Utilisateur inconnu", resultat.Message);
    }

    [Fact]
    public async Task OuvrirAsync_ErreurHttp_LeveUneMiddlewareCpqException()
    {
        var serveur = new ServeurFactice(HttpStatusCode.InternalServerError, "Erreur serveur");

        var erreur = await Assert.ThrowsAsync<MiddlewareCpqException>(
            () => new MiddlewareCpqClient(new HttpClient(serveur)).OuvrirAsync(Requete(), Recette));

        Assert.Contains("500", erreur.Message);
    }

    [Fact]
    public async Task LireXmlAsync_EnvoieLaRechercheSurGetXmlForSite()
    {
        var serveur = new ServeurFactice(HttpStatusCode.OK, """{"Message":null,"Success":true,"XMLResult":"<Dossier\/>"}""");

        await new MiddlewareCpqClient(new HttpClient(serveur)).LireXmlAsync(RequeteXml(), Recette);

        Assert.Equal(HttpMethod.Post, serveur.Methode);
        Assert.Equal("https://test.cfgroup.smartconfigurator.fr/Cfg_Middleware/Service.svc/GetXmlForSite", serveur.Url);
        Assert.Contains("\"ConfigurationId\":\"Liner_00000418\"", serveur.Corps);
        Assert.Contains("\"SiteId\":\"DEL_BRECE\"", serveur.Corps);
        Assert.Contains("\"AccountId\":\"102466\"", serveur.Corps);
    }

    [Fact]
    public async Task LireXmlAsync_Succes_RenvoieLeXml()
    {
        var serveur = new ServeurFactice(HttpStatusCode.OK, """{"Message":"","Success":true,"XMLResult":"<Dossier><Attributs\/><\/Dossier>"}""");

        var resultat = await new MiddlewareCpqClient(new HttpClient(serveur)).LireXmlAsync(RequeteXml(), Recette);

        Assert.True(resultat.Succes);
        Assert.Equal("<Dossier><Attributs/></Dossier>", resultat.Xml);
    }

    [Fact]
    public async Task LireXmlAsync_ConfigurationNonEnregistree_RenvoieLeMessageSansXml()
    {
        var serveur = new ServeurFactice(HttpStatusCode.OK, """{"Message":"No config found for this Id Config","Success":false,"XMLResult":""}""");

        var resultat = await new MiddlewareCpqClient(new HttpClient(serveur)).LireXmlAsync(RequeteXml(), Recette);

        Assert.False(resultat.Succes);
        Assert.Null(resultat.Xml);
        Assert.Equal("No config found for this Id Config", resultat.Message);
    }

    private static GetXMLRequest RequeteXml() => new()
    {
        Login = "cfgAxDelBrece@techform.fr",
        Password = "***",
        ConfigurationId = "Liner_00000418",
        ConfiguratorName = "Liner",
        SiteId = "DEL_BRECE",
        AccountId = "102466",
        ApplicationUrl = Recette.UrlApplication,
    };

    private static OpenRequest Requete() => new()
    {
        Login = "cfgAxDelBrece@techform.fr",
        Password = "***",
        ConfiguratorName = "Moon",
        DateValidite = new DateTime(2026, 10, 9, 8, 30, 0, DateTimeKind.Utc),
    };

    private sealed class ServeurFactice(HttpStatusCode statut, string reponse) : HttpMessageHandler
    {
        public HttpMethod? Methode { get; private set; }
        public string? Url { get; private set; }
        public System.Net.Http.Headers.MediaTypeHeaderValue? ContentType { get; private set; }
        public string? Corps { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requete, CancellationToken annulation)
        {
            Methode = requete.Method;
            Url = requete.RequestUri?.ToString();
            ContentType = requete.Content?.Headers.ContentType;
            Corps = requete.Content is null ? null : await requete.Content.ReadAsStringAsync(annulation);
            return new HttpResponseMessage(statut) { Content = new StringContent(reponse, Encoding.UTF8, "application/json") };
        }
    }
}
