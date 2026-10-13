namespace CfSaisie.Sap;

/// <summary>
/// SAP simulé pour le développement. Données et règles reprises de la maquette
/// (CF_Saisie_commandes_interactive_v21.html) : clients, adresses, agences, délais et planning.
/// </summary>
public sealed class SapSimule : ISapClientService, ISapLogistiqueService, ISapCommandeService
{
    private static readonly ClientSap[] Clients =
    [
        new("100124", "Dupont Piscines", "France", "FR", null),
        new("100811", "Aqua Services", "France", "FR", null),
        new("100442", "Waterlijn BV", "Pays-Bas", "NL", null),
        new("100287", "Piscinas Atlântico", "Portugal", "PT", null),
        new("100932", "Blue Horizon", "Belgique", "FR", null),
        new("102466", "Tony Stark", "France", "FR", "VIC"),
    ];

    private static readonly Dictionary<string, AdresseLivraisonSap[]> Adresses = new()
    {
        ["100124"] =
        [
            new("principal", "Adresse principale", true, "Dupont Piscines", "02 99 00 12 24", "12 rue des Bassins", "35000", "Rennes", "France"),
            new("secondaire-1", "Dépôt secondaire", false, "Dupont Piscines - Dépôt", "02 99 00 12 25", "4 rue de la Piscine", "35510", "Cesson-Sévigné", "France"),
        ],
        ["100811"] =
        [
            new("principal", "Adresse principale", true, "Aqua Services", "02 40 81 08 11", "25 avenue de l’Eau", "44000", "Nantes", "France"),
            new("secondaire-1", "Agence secondaire", false, "Aqua Services - Agence Sud", "02 40 81 08 12", "8 rue des Océans", "44400", "Rezé", "France"),
        ],
        ["100442"] =
        [
            new("principal", "Adresse principale", true, "Waterlijn BV", "+31 20 100 44 20", "18 Waterstraat", "1012 AB", "Amsterdam", "Pays-Bas"),
            new("secondaire-1", "Entrepôt", false, "Waterlijn BV - Warehouse", "+31 20 100 44 21", "42 Havenweg", "1043 AP", "Amsterdam", "Pays-Bas"),
        ],
        ["100287"] =
        [
            new("principal", "Adresse principale", true, "Piscinas Atlântico", "+351 21 100 02 87", "15 Rua do Mar", "1200-001", "Lisboa", "Portugal"),
            new("secondaire-1", "Dépôt Nord", false, "Piscinas Atlântico - Norte", "+351 22 100 02 88", "27 Rua das Piscinas", "4000-010", "Porto", "Portugal"),
        ],
        ["100932"] =
        [
            new("principal", "Adresse principale", true, "Blue Horizon", "+32 2 100 09 32", "9 Avenue des Eaux", "1000", "Bruxelles", "Belgique"),
            new("secondaire-1", "Agence Wallonie", false, "Blue Horizon - Wallonie", "+32 4 100 09 33", "31 Quai de la Meuse", "4000", "Liège", "Belgique"),
        ],
        ["102466"] =
        [
            new("principal", "Adresse principale", true, "Tony Stark", "01 02 46 60 00", "10880 Malibu Point", "75008", "Paris", "France"),
            new("secondaire-1", "Adresse secondaire", false, "Tony Stark - Atelier", "01 02 46 60 01", "42 avenue des Industries", "92100", "Boulogne-Billancourt", "France"),
        ],
    };

    private static readonly AgenceRetraitSap[] Agences =
    [
        new("OUEST", "Ouest"),
        new("RHONE-ALPES", "Rhône-Alpes"),
        new("PACA", "PACA"),
        new("AQUITAINE", "Aquitaine"),
        new("PAYS-BASQUE", "Pays Basque"),
        new("OCCITANIE", "Occitanie"),
    ];

    // Délais indicatifs de fabrication (jours) de la maquette, par nom de produit.
    private static readonly Dictionary<string, int> Delais = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Liner sur mesure"] = 8, ["Rolleasy2"] = 10, ["Moon"] = 12, ["Contura"] = 14, ["Rollover"] = 11,
        ["Rollin"] = 15, ["Rollinside"] = 17, ["Rolenergy"] = 18, ["Initial"] = 7, ["Supergreen"] = 9,
    };

    private int _compteurSap = 1_000_000 + Random.Shared.Next(8_000_000);

    public Task<IReadOnlyList<ClientSap>> RechercherAsync(string? recherche, int maximum, CancellationToken annulation = default)
    {
        var q = recherche?.Trim() ?? string.Empty;
        IReadOnlyList<ClientSap> resultat = Clients
            .Where(c => q.Length == 0
                || c.Code.StartsWith(q, StringComparison.OrdinalIgnoreCase)
                || c.RaisonSociale.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(maximum)
            .ToList();
        return Task.FromResult(resultat);
    }

    public Task<ClientSap?> LireAsync(string code, CancellationToken annulation = default) =>
        Task.FromResult(Clients.FirstOrDefault(c => c.Code == code));

    public Task<IReadOnlyList<AdresseLivraisonSap>> AdressesAsync(string code, CancellationToken annulation = default) =>
        Task.FromResult<IReadOnlyList<AdresseLivraisonSap>>(Adresses.TryGetValue(code, out var a) ? a : []);

    public Task<IReadOnlyList<AgenceRetraitSap>> AgencesAsync(CancellationToken annulation = default) =>
        Task.FromResult<IReadOnlyList<AgenceRetraitSap>>(Agences);

    public Task<(int DelaiJours, IReadOnlyList<JourPlanningSap> Jours)> PlanningAsync(string produit, DateOnly aPartirDu, int nombreJours, CancellationToken annulation = default)
    {
        var delai = Delais.TryGetValue(produit, out var d) ? d : 10;
        var jours = new List<JourPlanningSap>();
        var curseur = aPartirDu.AddDays(delai);
        while (jours.Count < nombreJours)
        {
            if (curseur.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                jours.Add(Capacite(produit, curseur));
            }
            curseur = curseur.AddDays(1);
        }
        return Task.FromResult<(int, IReadOnlyList<JourPlanningSap>)>((delai, jours));
    }

    public Task<ResultatEnvoiSap> EnvoyerAsync(CommandeAEnvoyer commande, CancellationToken annulation = default)
    {
        var numero = "45" + Interlocked.Increment(ref _compteurSap).ToString("D7");
        return Task.FromResult(new ResultatEnvoiSap(true, numero, null));
    }

    // Même pseudo-aléa que la maquette (productionCapacityFor) pour un rendu reproductible.
    private static JourPlanningSap Capacite(string produit, DateOnly date)
    {
        var graine = (produit.Sum(c => c) + date.Day + (date.Month - 1) * 13) % 10;
        return graine switch
        {
            0 or 7 => new JourPlanningSap(date, EtatCapacite.Complet, "Complet"),
            1 or 4 or 8 => new JourPlanningSap(date, EtatCapacite.Limitee, "1 à 2 places"),
            _ => new JourPlanningSap(date, EtatCapacite.Disponible, "Disponible"),
        };
    }
}
