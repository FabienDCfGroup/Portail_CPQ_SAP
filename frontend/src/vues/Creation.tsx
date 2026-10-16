import { useEffect, useState } from 'react';
import { api, type Message } from '../api';
import { estAdministrateur, useApp } from '../contexte';
import type { OuvertureAssistant } from './AssistantDossier';

export function Creation({ ouvrir }: { ouvrir: (o: OuvertureAssistant) => void }) {
  const app = useApp();
  const { t } = app;
  const [messages, setMessages] = useState<Message[] | null>(null);

  useEffect(() => {
    api.messagesTableauBord().then(setMessages).catch((e: Error) => app.notifier(e.message));
  }, [app.notifier]);

  return (
    <section id="dashboard" className="view active">
      <div className="card announcements">
        <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
          <div>
            <h2>{t('Informations & messages')}</h2>
            <p className="muted" style={{ margin: 0 }}>
              {t('Zone d’information pilotée par l’administrateur.')}
            </p>
          </div>
          {estAdministrateur(app.role) && (
            <button className="secondary admin-only" onClick={() => app.afficherVue('messages')}>
              {t('Gérer les messages')}
            </button>
          )}
        </div>
        <div>
          {messages?.length === 0 && (
            <div className="muted" style={{ marginTop: 10 }}>
              {t('Aucun message à afficher.')}
            </div>
          )}
          {messages?.map((m) => (
            <div key={m.id} className={`announcement ${m.audience === 'Tous' ? '' : 'client'}`}>
              <span className="tag">{m.audience === 'Tous' ? t('Tous') : m.audience}</span>
              <b>{m.titre}</b>
              <div style={{ marginTop: 5 }}>{m.corps}</div>
            </div>
          ))}
        </div>
      </div>
      <div className="dashboard-grid">
        <div className="card action-card order-card">
          <h2>{t('Création d’une commande')}</h2>
          <p>
            {t(
              'Identifiez le client par son code ou sa raison sociale. CF Saisie commandes n’affiche ensuite que les familles et produits autorisés pour ce client.',
            )}
          </p>
          <button className="primary" onClick={() => ouvrir({ type: 'Commande' })}>
            {t('Démarrer la création d’une commande →')}
          </button>
        </div>
        <div className="card action-card quote-card">
          <h2>{t('Création d’un devis')}</h2>
          <p>{t('Préparez un devis à partir du client, des familles et des produits auxquels il est autorisé à accéder.')}</p>
          <button className="primary" onClick={() => ouvrir({ type: 'Devis' })}>
            {t('Démarrer la création d’un devis →')}
          </button>
        </div>
      </div>
    </section>
  );
}
