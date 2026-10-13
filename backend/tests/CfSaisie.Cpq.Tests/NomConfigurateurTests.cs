namespace CfSaisie.Cpq.Tests;

public class NomConfigurateurTests
{
    [Theory]
    [InlineData("Volet")]
    [InlineData("MA10PE")]
    [InlineData("CHIV_Initial")]
    [InlineData("CHIV_Supergreen")]
    [InlineData("VF510")]
    public void EstValide_NomsDuMiddleware(string nom)
    {
        Assert.True(NomConfigurateur.EstValide(nom));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" Volet")]
    [InlineData("Volet hors-sol")]
    [InlineData("Volet-Immerge")]
    [InlineData("_Volet")]
    [InlineData("Volet_")]
    [InlineData("CHIV__Initial")]
    [InlineData("Volé")]
    public void EstValide_RefuseLesNomsHorsFormat(string? nom)
    {
        Assert.False(NomConfigurateur.EstValide(nom));
    }

    [Fact]
    public void EstValide_RefuseUnNomTropLong()
    {
        Assert.False(NomConfigurateur.EstValide(new string('A', NomConfigurateur.LongueurMax + 1)));
    }
}
