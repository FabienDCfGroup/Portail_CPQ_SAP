import { StrictMode, useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { api, connexionRefusee, seConnecter, type Session } from './api';
import { App } from './App';
import { EcranChargement } from './composants/Chargement';
import { FournisseurApp, lire } from './contexte';
import { traduire } from './i18n';
import './styles/maquette.css';
import './styles/portail.css';

// Thème sombre appliqué dès l'écran de chargement (le contexte de l'application n'est pas encore monté).
try {
  document.body.classList.toggle('dark', localStorage.getItem('cf-theme') === 'dark');
} catch {
  // Stockage indisponible : thème clair.
}

// Durée minimale de l'écran de chargement, pour qu'il ne se réduise pas à un flash quand l'API répond vite.
const DUREE_MINIMALE_MS = 1200;

// Avant le chargement de la session, la langue est celle mémorisée dans le navigateur (vérifiée ensuite contre les langues actives).
const t = (texte: string, valeurs?: Record<string, string>) => traduire(lire('cf-langue') ?? 'fr', texte, valeurs);

/** Connexion refusée (aucun groupe du portail) ou échouée côté Microsoft : pas de nouvelle tentative automatique. */
function AccesRefuse({ motif }: { motif: string }) {
  return (
    <div className="content" style={{ maxWidth: 640, margin: '12vh auto' }}>
      <div className="card" style={{ textAlign: 'center' }}>
        <h2>{t(motif === 'refuse' ? 'Accès refusé' : 'Connexion impossible')}</h2>
        <p className="muted">
          {t(
            motif === 'refuse'
              ? 'Votre compte Microsoft n’appartient à aucun groupe autorisé pour CF Saisie commandes. Demandez l’accès à votre administrateur.'
              : 'La connexion Microsoft a échoué. Réessayez ; si le problème persiste, contactez votre administrateur.',
          )}
        </p>
        <button className="primary" onClick={() => seConnecter(true)}>
          {t('Se connecter avec un autre compte')}
        </button>
      </div>
    </div>
  );
}

function Demarrage() {
  const [session, setSession] = useState<Session | null>(null);
  const [erreur, setErreur] = useState<string | null>(null);
  const [delaiEcoule, setDelaiEcoule] = useState(false);

  useEffect(() => {
    const minuteur = window.setTimeout(() => setDelaiEcoule(true), DUREE_MINIMALE_MS);
    return () => window.clearTimeout(minuteur);
  }, []);

  // Au lancement de « npm run dev », le navigateur s'ouvre avant que l'API ait fini de démarrer :
  // on réessaie pendant une minute avant d'afficher une erreur.
  const refus = connexionRefusee();

  useEffect(() => {
    if (refus) return;
    let essais = 0;
    let minuteur: number;
    const essayer = () =>
      api
        .session()
        .then(setSession)
        .catch((e: Error) => {
          if (++essais < 60) minuteur = window.setTimeout(essayer, 1000);
          else setErreur(e.message);
        });
    void essayer();
    return () => window.clearTimeout(minuteur);
  }, [refus]);

  if (refus) {
    return <AccesRefuse motif={refus} />;
  }
  if (erreur) {
    return (
      <div className="content">
        <div className="card">
          <h2>{t('API indisponible')}</h2>
          <p className="muted">
            {t("Impossible de joindre l'API du portail ({erreur}). Vérifiez qu'elle est démarrée :", { erreur })}{' '}
            <code>dotnet run --project backend/src/CfSaisie.Api</code>
          </p>
        </div>
      </div>
    );
  }
  if (!session || !delaiEcoule) {
    return <EcranChargement titre={t('CF Saisie commandes')} texte={t('Démarrage du portail')} />;
  }
  return (
    <FournisseurApp session={session}>
      <App />
    </FournisseurApp>
  );
}

// Retour de la connexion Microsoft : l'adresse déclarée dans Entra ID est la racine du portail (https://localhost:5173).
// Le code (ou l'erreur) renvoyé par Microsoft est transmis tel quel à l'API, qui termine la connexion.
const parametresRetour = new URLSearchParams(window.location.search);
const retourConnexion =
  window.location.pathname === '/' && parametresRetour.has('state') && (parametresRetour.has('code') || parametresRetour.has('error'));

if (retourConnexion) {
  window.location.replace(`/api/signin-oidc${window.location.search}`);
} else {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <Demarrage />
    </StrictMode>,
  );
}
