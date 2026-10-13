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

public sealed class MiddlewareCpqException(string message, Exception? innerException = null) : Exception(message, innerException);

public interface IMiddlewareCpq
{
    Task<ResultatLancement> OuvrirAsync(OpenRequest requete, EnvironnementMiddleware environnement, CancellationToken annulation = default);
}

/// <summary>
/// Appel HTTP du Middleware CPQ : POST JSON sur {serveur}/Cfg_Middleware/Service.svc/Open.
/// Reproduit JsonHelpers.JsonRequest (SmartDataExchange.Launcher.dll) utilisé par Configurateur.LancerConfigurateurMiddleware.
/// </summary>
public sealed class MiddlewareCpqClient(HttpClient http) : IMiddlewareCpq
{
    private static readonly DataContractJsonSerializer LecteurReponse = new(typeof(OpenResponse));

    public async Task<ResultatLancement> OuvrirAsync(OpenRequest requete, EnvironnementMiddleware environnement, CancellationToken annulation = default)
    {
        ArgumentNullException.ThrowIfNull(requete);
        ArgumentNullException.ThrowIfNull(environnement);

        var url = environnement.UrlApplication + "/Service.svc/Open";
        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(OpenRequestSerializer.Serialiser(requete), Encoding.UTF8, "application/json"),
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

        using (reponse)
        {
            var corps = await reponse.Content.ReadAsByteArrayAsync(annulation);
            if (!reponse.IsSuccessStatusCode)
            {
                throw new MiddlewareCpqException(
                    $"Le Middleware CPQ a répondu {(int)reponse.StatusCode} ({url}) : {Extrait(corps)}");
            }

            OpenResponse? resultat;
            try
            {
                using var flux = new MemoryStream(corps);
                resultat = (OpenResponse?)LecteurReponse.ReadObject(flux);
            }
            catch (Exception ex) when (ex is System.Runtime.Serialization.SerializationException or System.Xml.XmlException)
            {
                throw new MiddlewareCpqException($"Réponse du Middleware CPQ illisible : {Extrait(corps)}", ex);
            }

            if (resultat is null)
            {
                throw new MiddlewareCpqException("Réponse du Middleware CPQ vide.");
            }

            return new ResultatLancement(resultat.Success, resultat.Message, resultat.Url, resultat.ConfigIdSimplifie);
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
}
