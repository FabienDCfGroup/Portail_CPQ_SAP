namespace CfSaisie.Cpq.Tests;

public class ConfigIdCpqTests
{
    private static readonly string[] Configurateurs = ["Volet", "Moon", "CHIV_Initial", "CHIV_Supergreen", "MA10", "MA10PE", "MA10PEDEL"];

    [Theory]
    [InlineData("Volet_00000561", "Volet")]
    [InlineData("Moon_00006869", "Moon")]
    [InlineData("  Volet_00000561 ", "Volet")]
    [InlineData("volet_00000561", "Volet")]
    // Écart volontaire avec l'ancien outil, qui enverrait « CHIV » (voir Attendu/README.md, cas 07).
    [InlineData("CHIV_Initial_00000012", "CHIV_Initial")]
    [InlineData("CHIV_Supergreen_00000003", "CHIV_Supergreen")]
    // Noms qui sont le préfixe d'un autre : le « _ » délimite le configurateur.
    [InlineData("MA10_00000001", "MA10")]
    [InlineData("MA10PE_00000001", "MA10PE")]
    [InlineData("MA10PEDEL_00000001", "MA10PEDEL")]
    public void Configurateur_DeduitDuPrefixe(string configId, string attendu)
    {
        Assert.Equal(attendu, ConfigIdCpq.Configurateur(configId, Configurateurs));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    // Sans « _ », l'ancien outil bloquait : « Le ConfiguratorName est manquant dans le configID ».
    [InlineData("00000561")]
    [InlineData("Volet")]
    [InlineData("Volet_")]
    [InlineData("Inconnu_00000001")]
    public void Configurateur_IntrouvableRenvoieNull(string? configId)
    {
        Assert.Null(ConfigIdCpq.Configurateur(configId, Configurateurs));
    }
}
