namespace CfSaisie.Sap.Tests;

public class CalendrierCcxTests
{
    private static readonly DateOnly Lundi = new(2026, 10, 19);

    private static JourCcx Jour(DateOnly date, decimal capacite = 6, decimal restante = 5, bool selectionnable = true, string? motif = null,
        string code = "L", bool confirmation = false) =>
        new(code, "L. SIMP", date, capacite, capacite - restante, restante, selectionnable, motif, confirmation, 3, Lundi);

    private static ReponsePlanningCcx Reponse(IReadOnlyList<JourCcx> jours, params string[] origines) =>
        new(DateTime.UtcNow, "DEL_BRECE", origines.Length == 0 ? ["AX"] : origines, [], jours);

    [Theory]
    [InlineData(5, EtatJourProduction.Disponible, "Disponible")]
    [InlineData(3, EtatJourProduction.Disponible, "Disponible")]
    [InlineData(2, EtatJourProduction.Limite, "2 places")]
    [InlineData(1, EtatJourProduction.Limite, "1 place")]
    [InlineData(1.6, EtatJourProduction.Limite, "1 place")]
    public void Classer_SelectionnableSelonLeSeuilDe2Places(double restante, EtatJourProduction etat, string libelle)
    {
        var jour = CalendrierCcx.Classer(Jour(Lundi, restante: (decimal)restante), seuilPlacesLimitees: 2);

        Assert.Equal(etat, jour.Etat);
        Assert.Equal(libelle, jour.Libelle);
        Assert.True(jour.Selectionnable);
    }

    [Fact]
    public void Classer_CapaciteNegativeJamaisAffichee()
    {
        // L'ERP tolère les dépassements : -13 est une valeur normale, à ne pas afficher.
        var jour = CalendrierCcx.Classer(Jour(Lundi, restante: -13), 2);

        Assert.Null(jour.PlacesRestantes);
        Assert.DoesNotContain("-", jour.Libelle);
    }

    [Fact]
    public void Classer_CompletNonSelectionnable()
    {
        var jour = CalendrierCcx.Classer(Jour(Lundi, restante: -2, selectionnable: false, motif: "Complet"), 2);

        Assert.Equal(EtatJourProduction.Complet, jour.Etat);
        Assert.False(jour.Selectionnable);
        Assert.Null(jour.PlacesRestantes);
    }

    [Fact]
    public void Classer_AvantLeDelaiMini_IndiqueLaPremiereDateAutorisee()
    {
        var jour = CalendrierCcx.Classer(Jour(Lundi.AddDays(-3), selectionnable: false, motif: "Avant le delai mini du code"), 2);

        Assert.Equal(EtatJourProduction.AvantDelai, jour.Etat);
        Assert.Equal("Au plus tôt le 19/10/2026", jour.Motif);
    }

    [Fact]
    public void Classer_SeFieASelectionnableSansRecalculer()
    {
        // Capacité restante positive mais non sélectionnable (règle ERP) : on ne propose pas le jour.
        var jour = CalendrierCcx.Classer(Jour(Lundi, restante: 4, selectionnable: false, motif: "Complet"), 2);

        Assert.False(jour.Selectionnable);
    }

    [Fact]
    public void Construire_FiltreLeCodeMasqueLesJoursFermesEtTrie()
    {
        var reponse = Reponse([
            Jour(Lundi.AddDays(1)),
            Jour(Lundi.AddDays(-1), capacite: 0, restante: 0, selectionnable: false, motif: "Atelier ferme"),
            Jour(Lundi),
            Jour(Lundi, code: "LC"),
        ]);

        var calendrier = CalendrierCcx.Construire(reponse, "l", "DEL_BRECE", 2)!;

        Assert.Equal("L", calendrier.CodeCcx);
        Assert.Equal([Lundi, Lundi.AddDays(1)], calendrier.Jours.Select(j => j.Date));
        Assert.Equal(3, calendrier.DelaiMiniJoursOuvres);
        Assert.Equal(Lundi, calendrier.PremiereDateAutorisee);
    }

    [Fact]
    public void Construire_CodeAbsent_RenvoieNull()
    {
        Assert.Null(CalendrierCcx.Construire(Reponse([Jour(Lundi, code: "LC")]), "LGV", "DEL_BRECE", 2));
    }

    [Fact]
    public void Construire_ChargeIncompleteTantQueSapNEstPasUneOrigine()
    {
        Assert.True(CalendrierCcx.Construire(Reponse([Jour(Lundi)], "AX"), "L", "DEL_BRECE", 2)!.ChargeIncomplete);
        Assert.False(CalendrierCcx.Construire(Reponse([Jour(Lundi)], "AX", "SAP"), "L", "DEL_BRECE", 2)!.ChargeIncomplete);
    }

    [Fact]
    public void Construire_ConfirmationRequiseConservee()
    {
        var calendrier = CalendrierCcx.Construire(Reponse([Jour(Lundi, confirmation: true)]), "L", "DEL_BRECE", 2)!;

        Assert.True(calendrier.Jours[0].ConfirmationRequise);
    }
}
