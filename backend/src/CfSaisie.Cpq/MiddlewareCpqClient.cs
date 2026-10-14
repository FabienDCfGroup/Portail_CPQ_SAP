using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using SmartDataExchangeDefines.Models;

namespace CfSaisie.Cpq;

/// <summary>Réponse du Middleware à une demande d'ouverture de configurateur.</summary>
/// <param name="Url">URL du configurateur à ouvrir dans le navigateur.</param>
/// <param name="ConfigId">Identifiant simplifié de la configuration (ex. "Volet_00000561"), à conserver sur le dossier.</param>
public sealed record ResultatLancement(bool Succes, string? Message, string? Url, string? ConfigId)
{
    /// <summary>Lancement abouti sans message : comportement « ouvrir l'URL » de l'ancien outil.</summary>
    public bool PeutOuvrir => Succes && string.IsNullOrEmpty(Message) && !string.IsNullOrEmpty(Url);
}

/// <summary>Réponse du Middleware à GetXmlForSite : XML généré par le configurateur à l'enregistrement ou à la validation.</summary>
/// <param name="Xml">Null si la configuration n'a pas encore été enregistrée dans le configurateur.</param>
public sealed record ResultatXmlConfiguration(bool Succes, string? Message, string? Xml);

public sealed class MiddlewareCpqException(string message, Exception? innerException = null) : Exception(message, innerException);

public interface IMiddlewareCpq
{
    Task<ResultatLancement> OuvrirAsync(OpenRequest requete, EnvironnementMiddleware environnement, CancellationToken annulation = default);

    /// <summary>
    /// XML de fin de configuration (service GetXmlForSite), recherché par ConfigId, compte client, configurateur et site de production.
    /// Le Middleware le conserve dès que l'utilisateur enregistre ou valide la configuration.
    /// </summary>
    Task<ResultatXmlConfiguration> LireXmlAsync(GetXMLRequest requete, EnvironnementMiddleware environnement, CancellationToken annulation = default);
}

/// <summary>
/// Appel HTTP du Middleware CPQ : POST JSON sur {serveur}/Cfg_Middleware/Service.svc/Open.
/// Reproduit JsonHelpers.JsonRequest (SmartDataExchange.Launcher.dll) utilisé par Configurateur.LancerConfigurateurMiddleware.
/// </summary>
public sealed class MiddlewareCpqClient(HttpClient http) : IMiddlewareCpq
{
    private static readonly DataContractJsonSerializer EcrivainXml = new(typeof(GetXMLRequest));

    public async Task<ResultatLancement> OuvrirAsync(OpenRequest requete, EnvironnementMiddleware environnement, CancellationToken annulation = default)
    {
        ArgumentNullException.ThrowIfNull(requete);
        ArgumentNullException.ThrowIfNull(environnement);

        var resultat = await EnvoyerAsync<OpenResponse>(environnement, "Open", OpenRequestSerializer.Serialiser(requete), annulation);
        return new ResultatLancement(resultat.Success, resultat.Message, resultat.Url, resultat.ConfigIdSimplifie);
    }

    public async Task<ResultatXmlConfiguration> LireXmlAsync(GetXMLRequest requete, EnvironnementMiddleware environnement, CancellationToken annulation = default)
    {
        ArgumentNullException.ThrowIfNull(requete);
        ArgumentNullException.ThrowIfNull(environnement);

        using var flux = new MemoryStream();
        EcrivainXml.WriteObject(flux, requete);
        var resultat = await EnvoyerAsync<GetXMLResponse>(environnement, "GetXmlForSite", Encoding.UTF8.GetString(flux.ToArray()), annulation);
        return new ResultatXmlConfiguration(resultat.Success, resultat.Message, string.IsNullOrWhiteSpace(resultat.XMLResult) ? null : resultat.XMLResult);
    }

    /// <summary>POST JSON sur {serveur}/Cfg_Middleware/Service.svc/{operation} ; réponse lue comme le faisait l'ancien outil (DataContractJsonSerializer).</summary>
    private async Task<TReponse> EnvoyerAsync<TReponse>(EnvironnementMiddleware environnement, string operation, string corpsJson, CancellationToken annulation)
        where TReponse : class
    {
        var url = $"{environnement.UrlApplication}/Service.svc/{operation}";
        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(corpsJson, Encoding.UTF8, "application/json"),
        };
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage reponse;
        try
        {
            reponse = await http.SendAsync(message, annulation);
        }
        catch (HttpRequestException ex)
        {
            throw new MiddlewareCpqException($"Le Middleware CPQ est injoignable ({url}).", ex);
        }
        catch (TaskCanceledException ex) when (!annulation.IsCancellationRequested)
        {
            throw new MiddlewareCpqException($"Le Middleware CPQ n'a pas répondu à temps ({url}).", ex);
        }

        using (reponse)
        {
            var corps = await reponse.Content.ReadAsByteArrayAsync(annulation);
            if (!reponse.IsSuccessStatusCode)
            {
                throw new MiddlewareCpqException(
                    $"Le Middleware CPQ a répondu {(int)reponse.StatusCode} ({url}) : {Extrait(corps)}");
            }

            TReponse? resultat;
            try
            {
                using var flux = new MemoryStream(corps);
                resultat = (TReponse?)new DataContractJsonSerializer(typeof(TReponse)).ReadObject(flux);
            }
            // Toute réponse non conforme (ex. page d'erreur HTML pendant un redémarrage IIS) est signalée comme telle.
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new MiddlewareCpqException($"Réponse du Middleware CPQ illisible (Middleware en cours de redémarrage ?) : {Extrait(corps)}", ex);
            }

            return resultat ?? throw new MiddlewareCpqException("Réponse du Middleware CPQ vide.");
        }
    }

    private static string Extrait(byte[] corps)
    {
        var texte = Encoding.UTF8.GetString(corps);
        return texte.Length <= 500 ? texte : texte[..500] + "…";
    }
}

/// <summary>Middleware simulé pour le développement : renvoie une URL fictive sans appel réseau.</summary>
public sealed class MiddlewareCpqSimule : IMiddlewareCpq
{
    private int _compteur;

    public Task<ResultatLancement> OuvrirAsync(OpenRequest requete, EnvironnementMiddleware environnement, CancellationToken annulation = default)
    {
        var configId = requete.ConfigId ?? $"{requete.ConfiguratorName}_{Interlocked.Increment(ref _compteur):D8}";
        var url = $"about:blank#simulation-{Uri.EscapeDataString(configId)}";
        return Task.FromResult(new ResultatLancement(true, null, url, configId));
    }

    /// <summary>XML minimal au format « Dossier » : avec un code CCX « L » pour le Liner (comme les XML réels), sans ccx sinon.</summary>
    public Task<ResultatXmlConfiguration> LireXmlAsync(GetXMLRequest requete, EnvironnementMiddleware environnement, CancellationToken annulation = default)
    {
        var attributs = string.Equals(requete.ConfiguratorName, "Liner", StringComparison.OrdinalIgnoreCase)
            ? """<Attribut Key="ccx" Value="L" AttributGroup="InfoProd" />"""
            : string.Empty;
        var xml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Dossier>
              <Entete><IDConfig>{requete.ConfigurationId}</IDConfig><ConfiguratorName>{requete.ConfiguratorName}</ConfiguratorName><ProductionSite>{requete.SiteId}</ProductionSite></Entete>
              <Attributs>{attributs}</Attributs>
            </Dossier>
            """;
        return Task.FromResult(new ResultatXmlConfiguration(true, null, xml));
    }
}
