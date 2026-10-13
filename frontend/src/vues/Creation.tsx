import { useEffect, useState } from 'react';
import { api, type Message } from '../api';
import { estAdministrateur, useApp } from '../contexte';
import type { OuvertureAssistant } from './AssistantDossier';
import { OuvertureConfiguration } from './OuvertureConfiguration';

export function Creation({ ouvrir }: { ouvrir: (o: OuvertureAssistant) => void }) {
  const app = useApp();
  const [messages, setMessages] = useState<Message[] | null>(null);
  const [ouverture, setOuverture] = useState(false);

  useEffect(() => {
    api.messagesTableauBord().then(setMessages).catch((e: Error) => app.notifier(e.message));
  }, [app.notifier]);

  return (
    <section id="dashboard" className="view active">
      <div className="card announcements">
        <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
          <div>
            <h2>Informations & messages</h2>
            <p className="muted" style={{ margin: 0 }}>
              Zone d'information pilotée par l’administrateur.
            </p>
          </div>
          {estAdministrateur(app.role) && (
            <button className="secondary admin-only" onClick={() => app.afficherVue('messages')}>
              Gérer les messages
            </button>
          )}
        </div>
        <div>
          {messages?.length === 0 && (
            <div className="muted" style={{ marginTop: 10 }}>
              Aucun message à afficher.
            </div>
          )}
          {messages?.map((m) => (
            <div key={m.id} className={`announcement ${m.audience === 'Tous' ? '' : 'client'}`}>
              <span className="tag">{m.audience}</span>
              <b>{m.titre}</b>
              <div style={{ marginTop: 5 }}>{m.corps}</div>
            </div>
          ))}
        </div>
      </div>
      <div className="dashboard-grid">
        <div className="card action-card order-card">
          <h2>Création d'une commande</h2>
          <p>
            Identifiez le client par son code ou sa raison sociale. CF Saisie commandes n’affiche ensuite que les familles et produits autorisés pour
            ce client.
          </p>
          <button className="primary" onClick={() => ouvrir({ type: 'Commande' })}>
            Démarrer la création d'une commande →
          </button>
        </div>
        <div className="card action-card quote-card">
          <h2>Création d'un devis</h2>
          <p>Préparez un devis à partir du client, des familles et des produits auxquels il est autorisé à accéder.</p>
          <button className="primary" onClick={() => ouvrir({ type: 'Devis' })}>
            Démarrer la création d'un devis →
          </button>
        </div>
        <div className="card action-card open-card">
          <h2>Ouvrir une configuration existante</h2>
          <p>Rouvrez une commande ou un devis dans son configurateur, en modification ou en consultation, à partir de son ConfigId.</p>
          <button className="primary" onClick={() => setOuverture(true)}>
            Ouvrir une configuration →
          </button>
        </div>
      </div>
      {ouverture && <OuvertureConfiguration surFermer={() => setOuverture(false)} />}
    </section>
  );
}
