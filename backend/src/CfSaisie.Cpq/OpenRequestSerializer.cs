using System.Runtime.Serialization.Json;
using System.Text;
using SmartDataExchangeDefines.Models;

namespace CfSaisie.Cpq;

/// <summary>
/// Sérialise une <see cref="OpenRequest"/> au format attendu par le Middleware CPQ.
/// Reproduit le comportement de JsonHelpers.JsonRequest (SmartDataExchange.Launcher.dll) utilisé par l'ancien outil :
/// DataContractJsonSerializer, tous les champs émis (y compris null), énumérations en entiers, dates au format WCF "\/Date(...)\/".
/// </summary>
public static class OpenRequestSerializer
{
    private static readonly DataContractJsonSerializer Serializer = new(typeof(OpenRequest));

    public static string Serialiser(OpenRequest requete)
    {
        ArgumentNullException.ThrowIfNull(requete);

        using var flux = new MemoryStream();
        Serializer.WriteObject(flux, requete);
        return Encoding.UTF8.GetString(flux.ToArray());
    }
}
