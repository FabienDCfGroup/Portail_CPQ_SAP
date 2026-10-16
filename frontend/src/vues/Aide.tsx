import { useApp } from '../contexte';

export function Aide() {
  const { t } = useApp();
  return (
    <section id="help" className="view active">
      <div className="hero">
        <h1>{t('Aide')}</h1>
        <p>{t('CF Saisie commandes — saisie des commandes et devis, lancement des configurateurs CPQ.')}</p>
      </div>
      <div className="card" style={{ marginTop: 18 }}>
        <h3>{t('Parcours principal')}</h3>
        <p>
          {t(
            'Tableau de bord → Création d’une commande → rechercher le client → renseigner la référence chantier, le ticket Salesforce et la livraison → choisir la famille disponible → choisir le produit autorisé. Le configurateur du produit s’ouvre dans une nouvelle fenêtre ; fermez-la après avoir enregistré la configuration, puis choisissez la date de fin de production et validez.',
          )}
        </p>
        <h3>{t('Reprendre une commande ou un devis')}</h3>
        <p>
          {t(
            'Suivi des commandes → Ouvrir (dossier en cours) pour reprendre la saisie et la configuration, Consulter pour voir la configuration d’un dossier transmis, Transformer en commande pour un devis envoyé.',
          )}
        </p>
        <h3>{t('Rôles')}</h3>
        <p>
          {t(
            'Chaque personne se connecte avec son compte Microsoft ; son rôle dépend de son groupe Entra ID. Administrateur voit tous les onglets, dont Gestion clients & produits et le Journal des connexions. Utilisateur voit Création commande/devis et le Suivi des commandes. Super administrateur déclare en plus les configurateurs du Middleware (onglet Configurateurs), auxquels les administrateurs rattachent ensuite les produits.',
          )}
        </p>
        <h3>{t('Mode Production / Test')}</h3>
        <p>
          {t(
            'Le mode choisi dans la barre du haut détermine le Middleware CPQ utilisé pour les nouveaux lancements (recette en mode Test, production en mode Production). Un dossier repris depuis le suivi s’ouvre toujours dans l’environnement où il a été créé.',
          )}
        </p>
      </div>
    </section>
  );
}
