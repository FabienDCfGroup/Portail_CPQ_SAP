namespace CfSaisie.Cpq.Tests;

public class XmlConfigurationCpqTests
{
    // Structure reprise d'un XML Liner réel (C:\Techform\Fichiers de sortie\FTP\CPQ\OUT\Ax\Liner_00000418.xml), données réduites.
    private const string XmlLiner = """
        <?xml version="1.0" encoding="utf-8"?>
        <Dossier xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
          <Entete>
            <Nature>C</Nature>
            <IDConfig>Liner_00000418</IDConfig>
            <ConfiguratorName>Liner</ConfiguratorName>
            <AccountId>102466</AccountId>
            <ProductionSite>DEL_BRECE</ProductionSite>
          </Entete>
          <Lignes>
            <Ligne VenteItemId="V-LLSI10000" LineNum="1" Designation="Liner SUPERIEUR Imprimé forme classique" Qty="68" Prix="0" Calculation="true" />
          </Lignes>
          <Nomenclatures />
          <Attributs>
            <Attribut Key="couleur" Value="Ardoise" AttributGroup="InfoProd" />
            <Attribut Key="ccx" Value="L" AttributGroup="InfoProd" />
          </Attributs>
        </Dossier>
        """;

    [Fact]
    public void Lire_RenvoieLeCodeCcxEtLeSite()
    {
        var xml = XmlConfigurationCpq.Lire(XmlLiner);

        Assert.Equal("L", xml.CodeCcx);
        Assert.Equal("DEL_BRECE", xml.ProductionSite);
    }

    [Theory]
    [InlineData("C", "C")]
    [InlineData("D", "D")]
    [InlineData(" d ", "D")]
    public void Lire_RenvoieLaNatureDeLaDerniereValidation(string ecrite, string attendue)
    {
        var xml = XmlConfigurationCpq.Lire(XmlLiner.Replace("<Nature>C</Nature>", $"<Nature>{ecrite}</Nature>"));

        Assert.Equal(attendue, xml.Nature);
    }

    [Fact]
    public void Lire_SansNatureRenvoieNull()
    {
        Assert.Null(XmlConfigurationCpq.Lire(XmlLiner.Replace("<Nature>C</Nature>", "")).Nature);
    }

    [Theory]
    [InlineData("LC")]
    [InlineData("LGV")]
    [InlineData("LGVP")]
    public void Lire_RenvoieLaValeurEcriteParLeConfigurateur(string code)
    {
        var xml = XmlConfigurationCpq.Lire(XmlLiner.Replace("""Key="ccx" Value="L" """, $"""Key="ccx" Value="{code}" """));

        Assert.Equal(code, xml.CodeCcx);
    }

    [Fact]
    public void Lire_CleCcxInsensibleALaCasse()
    {
        var xml = XmlConfigurationCpq.Lire(XmlLiner.Replace("""Key="ccx" """, """Key="CCX" """));

        Assert.Equal("L", xml.CodeCcx);
    }

    [Theory]
    [InlineData("""<Attribut Key="ccx" Value="L" AttributGroup="InfoProd" />""", "")]
    [InlineData("""Value="L" """, """Value="  " """)]
    public void Lire_SansCodeCcxRenvoieNull(string remplace, string par)
    {
        var xml = XmlConfigurationCpq.Lire(XmlLiner.Replace(remplace, par));

        Assert.Null(xml.CodeCcx);
    }

    [Theory]
    [InlineData("pas du xml")]
    [InlineData("<Autre><Attributs /></Autre>")]
    public void Lire_XmlInattenduEstRefuse(string xml)
    {
        Assert.Throws<FormatException>(() => XmlConfigurationCpq.Lire(xml));
    }
}
