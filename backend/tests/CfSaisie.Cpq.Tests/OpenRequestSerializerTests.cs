using SmartDataExchangeDefines.Models;

namespace CfSaisie.Cpq.Tests;

public class OpenRequestSerializerTests
{
    /// <summary>
    /// Le fichier de référence a été produit sous .NET Framework 4.x avec la même DLL et le même sérialiseur
    /// que l'ancien outil. Ce test garantit que la DLL se charge en .NET 8 et que le JSON produit est identique, caractère pour caractère.
    /// </summary>
    [Fact]
    public void Serialiser_ProduitLeMemeJsonQueDotNetFramework()
    {
        var requete = new OpenRequest
        {
            Login = "cfgAxDelBrece@techform.fr",
            Password = "***",
            TypeAppel = EnuAction.Creation,
            TypeTransaction = EnuTypetransaction.Order,
            DateValidite = new DateTime(2026, 10, 9, 8, 30, 0, DateTimeKind.Utc),
            ProductionSite = "DEL_BRECE",
            ConfiguratorName = "Volet",
            AccountId = "102466",
            Language = "FR",
            ApplicationUrl = "https://test.cfgroup.smartconfigurator.fr/Cfg_Middleware",
            ProjectRef = "AfficherBoutonPresenter",
            BusinessCode = "CHANTIER-é\"1",
            Email = "a@b.fr",
        };
        var attendu = File.ReadAllText(Path.Combine("Reference", "OpenRequest_NetFramework.json"));

        var json = OpenRequestSerializer.Serialiser(requete);

        Assert.Equal(attendu, json);
    }
}
