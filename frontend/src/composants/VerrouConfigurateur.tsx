import type { Valeurs } from '../i18n';

/**
 * Voile plein écran affiché tant que la fenêtre du configurateur est ouverte : le portail est bloqué
 * jusqu'à la fermeture du configurateur (fin de configuration ou abandon).
 */
export function VerrouConfigurateur({ fenetre, libelle, t }: { fenetre: Window; libelle: string; t: (texte: string, valeurs?: Valeurs) => string }) {
  function fermer() {
    // Le configurateur traite la fermeture de sa fenêtre comme un abandon de la configuration en cours.
    if (window.confirm(t('Fermer le configurateur ? La configuration en cours non enregistrée sera perdue.'))) {
      fenetre.close();
    }
  }

  return (
    <div className="verrou-configurateur" role="dialog" aria-modal="true" aria-labelledby="verrou-titre">
      <div className="verrou-boite">
        <div className="verrou-icone" aria-hidden="true">
          ⚙
        </div>
        <h2 id="verrou-titre">{t('Configurateur ouvert')}</h2>
        <p>
          <b>{libelle}</b>
        </p>
        <p className="muted">
          {t('Terminez la configuration (Enregistrer ou Valider), puis fermez la fenêtre du configurateur pour revenir au portail.')}
        </p>
        <div className="verrou-actions">
          <button className="primary" onClick={() => fenetre.focus()}>
            {t('Afficher le configurateur')}
          </button>
          <button className="secondary" onClick={fermer}>
            {t('Fermer le configurateur')}
          </button>
        </div>
      </div>
    </div>
  );
}
