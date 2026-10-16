using CfSaisie.Cpq;
using CfSaisie.Data;
using CfSaisie.Sap;
using Microsoft.EntityFrameworkCore;

namespace CfSaisie.Api.Services;

/// <summary>
/// Ouverture d'une configuration existante à partir de son ConfigId (modification ou consultation),
/// comme le champ ConfigId de l'ancien outil. Rien n'est enregistré dans la base du portail.
/// </summary>
public sealed class ServiceLancements(CfSaisieContext db, ISapClientService clients, LanceurConfigurateur lanceur, LangueInterface langue)
{
    public async Task<LancementDto> OuvrirAsync(OuvertureConfigurationDto demande, CancellationToken annulation)
    {
        if (demande.Action == ActionConfigurateur.Creation)
        {
            throw new RequeteInvalideException("La création d'une commande ou d'un devis passe par l'assistant de création.");
        }

        var configId = demande.ConfigId?.Trim();
        if (string.IsNullOrEmpty(configId)) throw new RequeteInvalideException("Saisissez le ConfigId de la configuration.");
        if (string.IsNullOrWhiteSpace(demande.CodeClient)) throw new RequeteInvalideException("Saisissez le code client.");
        var client = await clients.LireAsync(demande.CodeClient.Trim(), annulation)
            ?? throw new RequeteInvalideException($"Client {demande.CodeClient.Trim()} introuvable dans les données ERP.");

        var configurateurs = await db.Configurateurs.AsNoTracking().ToListAsync(annulation);
        var nom = ConfigIdCpq.Configurateur(configId, configurateurs.Select(c => c.Nom))
            ?? throw new RequeteInvalideException(
                $"Aucun configurateur ne correspond au ConfigId « {configId} » (format attendu : <Configurateur>_<numéro>, ex. Volet_00000561).");
        var configurateur = configurateurs.First(c => c.Nom == nom);
        // Une configuration reste consultable même si son configurateur a été désactivé dans le portail.
        if (!configurateur.Actif && demande.Action == ActionConfigurateur.Modification)
        {
            throw new RequeteInvalideException($"Le configurateur {configurateur.Nom} est inactif.");
        }

        var lancement = new DemandeLancement
        {
            Action = demande.Action,
            TypeDocument = demande.Type == TypeDossier.Commande ? TypeDocument.Commande : TypeDocument.Devis,
            Configurateur = new ConfigurateurCpq(configurateur.Nom, configurateur.ProductionSite),
            CodeClient = client.Code,
            ConfigId = configId,
            // Langue de l'interface du portail (la langue du client ERP n'est plus utilisée).
            Langue = await langue.PourConfigurateurAsync(annulation),
        };
        var resultat = await lanceur.LancerAsync(lancement, lanceur.Resoudre(demande.Environnement), configId, annulation);
        return new LancementDto(resultat.Succes, resultat.PeutOuvrir, resultat.Message, resultat.Url, resultat.ConfigId);
    }
}
