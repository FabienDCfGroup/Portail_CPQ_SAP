import { StrictMode, useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { api, type Session } from './api';
import { App } from './App';
import { FournisseurApp } from './contexte';
import './styles/maquette.css';
import './styles/portail.css';

function Demarrage() {
  const [session, setSession] = useState<Session | null>(null);
  const [erreur, setErreur] = useState<string | null>(null);

  // Au lancement de « npm run dev », le navigateur s'ouvre avant que l'API ait fini de démarrer :
  // on réessaie pendant une minute avant d'afficher une erreur.
  useEffect(() => {
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
  }, []);

  if (erreur) {
    return (
      <div className="content">
        <div className="card">
          <h2>API indisponible</h2>
          <p className="muted">
            Impossible de joindre l'API du portail ({erreur}). Vérifiez qu'elle est démarrée :{' '}
            <code>dotnet run --project backend/src/CfSaisie.Api</code>
          </p>
        </div>
      </div>
    );
  }
  if (!session) {
    return (
      <div className="content" style={{ textAlign: 'center', marginTop: '30vh' }}>
        <h2>CF Saisie commandes</h2>
        <p className="muted">Démarrage du serveur…</p>
      </div>
    );
  }
  return (
    <FournisseurApp session={session}>
      <App />
    </FournisseurApp>
  );
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Demarrage />
  </StrictMode>,
);
