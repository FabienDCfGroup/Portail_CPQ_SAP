import { useCallback, useEffect, useState } from 'react';
import { admin, api, type Client, type CommentaireFamille, type CommentaireModele, type Message } from '../api';
import { Modale } from '../composants/Modale';
import { useApp } from '../contexte';

type Onglet = 'bibliotheque' | 'tableau';

export function GestionMessages() {
  const { t } = useApp();
  const [onglet, setOnglet] = useState<Onglet>('bibliotheque');
  return (
    <section id="messages" className="view active">
      <div className="hero">
        <h1>{t('Gestion des messages')}</h1>
        <p>{t('Gérez la bibliothèque de commentaires utilisables dans les commandes et les messages affichés sur le tableau de bord.')}</p>
      </div>
      <div className="message-admin-tabs">
        <button className={onglet === 'bibliotheque' ? 'active' : ''} onClick={() => setOnglet('bibliotheque')}>
          {t('Bibliothèque de commentaires')}
        </button>
        <button className={onglet === 'tableau' ? 'active' : ''} onClick={() => setOnglet('tableau')}>
          {t('Messages tableau de bord')}
        </button>
      </div>
      {onglet === 'bibliotheque' ? <Bibliotheque /> : <MessagesTableauBord />}
    </section>
  );
}

function Bibliotheque() {
  const { notifier, t } = useApp();
  const [familles, setFamilles] = useState<CommentaireFamille[]>([]);
  const [recherche, setRecherche] = useState('');
  const [familleEditee, setFamilleEditee] = useState<Partial<CommentaireFamille> | null>(null);
  const [modeleEdite, setModeleEdite] = useState<Partial<CommentaireModele> | null>(null);

  const charger = useCallback(() => api.commentaires().then(setFamilles).catch((e: Error) => notifier(e.message)), [notifier]);
  useEffect(() => {
    void charger();
  }, [charger]);

  const executer = async (action: Promise<unknown>, message?: string) => {
    try {
      await action;
      if (message) notifier(message);
      await charger();
      return true;
    } catch (e) {
      notifier((e as Error).message);
      return false;
    }
  };

  const q = recherche.trim().toLowerCase();
  const lignes = familles.flatMap((f) => {
    if (!f.modeles.length) return !q || f.nom.toLowerCase().includes(q) ? [{ famille: f, modele: null as CommentaireModele | null }] : [];
    return f.modeles.filter((m) => !q || f.nom.toLowerCase().includes(q) || m.texte.toLowerCase().includes(q)).map((m) => ({ famille: f, modele: m }));
  });

  return (
    <div className="card">
      <div className="admin-section-title">
        <div>
          <h2>{t('Bibliothèque de commentaires')}</h2>
          <p className="muted" style={{ margin: 0 }}>
            {t('Classez les messages par famille et définissez leur ordre d’affichage.')}
          </p>
        </div>
      </div>
      <div className="comment-library-toolbar">
        <input placeholder={t('Rechercher une famille ou un message...')} value={recherche} onChange={(e) => setRecherche(e.target.value)} style={{ maxWidth: 420 }} />
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          <button className="secondary" onClick={() => setFamilleEditee({ nom: '', ordre: familles.length + 1 })}>
            {t('Créer une famille')}
          </button>
          <button className="primary" onClick={() => setModeleEdite({ familleId: familles[0]?.id, ordre: 1, texte: '' })}>
            {t('Créer un message')}
          </button>
        </div>
      </div>
      <div className="table-wrap comment-library-table">
        <table>
          <thead>
            <tr>
              <th>{t('Famille')}</th>
              <th>{t('Ordre famille')}</th>
              <th>{t('Message')}</th>
              <th>{t('Ordre message')}</th>
              <th>{t('Actions')}</th>
            </tr>
          </thead>
          <tbody>
            {lignes.length === 0 && (
              <tr>
                <td colSpan={5} className="muted">
                  {t('Aucun résultat.')}
                </td>
              </tr>
            )}
            {lignes.map(({ famille: f, modele: m }) => (
              <tr key={`${f.id}-${m?.id ?? 'vide'}`}>
                <td>
                  <span className="comment-family-badge">{f.nom}</span>
                </td>
                <td>{f.ordre}</td>
                <td className={m ? '' : 'muted'}>{m ? m.texte : t('Aucun message')}</td>
                <td>{m?.ordre ?? ''}</td>
                <td>
                  <div className="order-buttons">
                    {m ? (
                      <>
                        <button className="secondary" onClick={() => void executer(admin.deplacerModele(m.id, -1))}>
                          ↑
                        </button>
                        <button className="secondary" onClick={() => void executer(admin.deplacerModele(m.id, 1))}>
                          ↓
                        </button>
                        <button className="secondary" onClick={() => setModeleEdite(m)}>
                          {t('Modifier')}
                        </button>
                        <button
                          className="danger"
                          onClick={() => window.confirm(t('Supprimer ce message prédéfini ?')) && void executer(admin.supprimerModele(m.id))}
                        >
                          {t('Supprimer')}
                        </button>
                      </>
                    ) : (
                      <>
                        <button
                          className="secondary"
                          onClick={() => void executer(admin.deplacerFamilleCommentaire(f.id, -1))}
                        >
                          ↑
                        </button>
                        <button
                          className="secondary"
                          onClick={() => void executer(admin.deplacerFamilleCommentaire(f.id, 1))}
                        >
                          ↓
                        </button>
                        <button className="secondary" onClick={() => setFamilleEditee(f)}>
                          {t('Modifier')}
                        </button>
                        <button
                          className="danger"
                          onClick={() =>
                            window.confirm(t('Supprimer la famille « {nom} » ?', { nom: f.nom })) && void executer(admin.supprimerFamilleCommentaire(f.id))
                          }
                        >
                          {t('Supprimer')}
                        </button>
                      </>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {familleEditee && (
        <EditionFamilleCommentaire
          famille={familleEditee}
          surFermer={() => setFamilleEditee(null)}
          surEnregistrer={async (nom, ordre) => {
            const ok = await executer(
              familleEditee.id ? admin.modifierFamilleCommentaire(familleEditee.id, nom, ordre) : admin.creerFamilleCommentaire(nom, ordre),
              t('Famille enregistrée'),
            );
            if (ok) setFamilleEditee(null);
          }}
        />
      )}
      {modeleEdite && (
        <EditionModele
          modele={modeleEdite}
          familles={familles}
          surFermer={() => setModeleEdite(null)}
          surEnregistrer={async (familleId, ordre, texte) => {
            const ok = await executer(
              modeleEdite.id ? admin.modifierModele(modeleEdite.id, familleId, ordre, texte) : admin.creerModele(familleId, ordre, texte),
              t('Message enregistré'),
            );
            if (ok) setModeleEdite(null);
          }}
        />
      )}
    </div>
  );
}

function EditionFamilleCommentaire({
  famille,
  surFermer,
  surEnregistrer,
}: {
  famille: Partial<CommentaireFamille>;
  surFermer: () => void;
  surEnregistrer: (nom: string, ordre: number) => void;
}) {
  const { t } = useApp();
  const [nom, setNom] = useState(famille.nom ?? '');
  const [ordre, setOrdre] = useState(famille.ordre ?? 1);
  return (
    <Modale
      ouverte
      titre={t('Famille de commentaires')}
      sousTitre={t('Créez ou modifiez une famille et son ordre.')}
      largeur={560}
      surFermer={surFermer}
    >
      <div className="form">
        <label>{t('Nom de la famille')}</label>
        <input placeholder={t('Ex. Conformité')} value={nom} onChange={(e) => setNom(e.target.value)} />
        <label>{t('Ordre')}</label>
        <input type="number" min={1} value={ordre} onChange={(e) => setOrdre(Number(e.target.value) || 1)} />
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 14 }}>
          <button className="secondary" onClick={surFermer}>
            {t('Annuler')}
          </button>
          <button className="primary" onClick={() => surEnregistrer(nom, ordre)}>
            {t('Enregistrer')}
          </button>
        </div>
      </div>
    </Modale>
  );
}

function EditionModele({
  modele,
  familles,
  surFermer,
  surEnregistrer,
}: {
  modele: Partial<CommentaireModele>;
  familles: CommentaireFamille[];
  surFermer: () => void;
  surEnregistrer: (familleId: number, ordre: number, texte: string) => void;
}) {
  const { t } = useApp();
  const [familleId, setFamilleId] = useState(modele.familleId ?? familles[0]?.id ?? 0);
  const [ordre, setOrdre] = useState(modele.ordre ?? 1);
  const [texte, setTexte] = useState(modele.texte ?? '');
  return (
    <Modale
      ouverte
      titre={t('Message prédéfini')}
      sousTitre={t('Classez le message dans une famille et définissez sa priorité.')}
      largeur={680}
      surFermer={surFermer}
    >
      <div className="form">
        <label>{t('Famille')}</label>
        <select value={familleId} onChange={(e) => setFamilleId(Number(e.target.value))}>
          {familles.map((f) => (
            <option key={f.id} value={f.id}>
              {f.nom}
            </option>
          ))}
        </select>
        <label>{t('Ordre dans la famille')}</label>
        <input type="number" min={1} value={ordre} onChange={(e) => setOrdre(Number(e.target.value) || 1)} />
        <label>{t('Message')}</label>
        <textarea rows={5} placeholder={t('Texte du commentaire...')} value={texte} onChange={(e) => setTexte(e.target.value)} />
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 14 }}>
          <button className="secondary" onClick={surFermer}>
            {t('Annuler')}
          </button>
          <button className="primary" onClick={() => surEnregistrer(familleId, ordre, texte)}>
            {t('Enregistrer')}
          </button>
        </div>
      </div>
    </Modale>
  );
}

function MessagesTableauBord() {
  const { notifier, t } = useApp();
  const [messages, setMessages] = useState<Message[]>([]);
  const [clients, setClients] = useState<Client[]>([]);
  const [audience, setAudience] = useState('Tous');
  const [titre, setTitre] = useState('');
  const [corps, setCorps] = useState('');

  const charger = useCallback(() => admin.messages().then(setMessages).catch((e: Error) => notifier(e.message)), [notifier]);
  useEffect(() => {
    void charger();
    api.clients().then(setClients).catch(() => undefined);
  }, [charger]);

  async function publier() {
    if (!titre.trim() || !corps.trim()) return notifier(t('Complétez le titre et le message'));
    try {
      await admin.publierMessage(audience, titre, corps);
      setTitre('');
      setCorps('');
      notifier(t('Message publié'));
      await charger();
    } catch (e) {
      notifier((e as Error).message);
    }
  }

  async function supprimer(id: number) {
    await admin.supprimerMessage(id);
    notifier(t('Message supprimé'));
    await charger();
  }

  return (
    <div className="admin-grid">
      <div className="card form">
        <h2>{t('Nouveau message tableau de bord')}</h2>
        <label>{t('Audience')}</label>
        <select value={audience} onChange={(e) => setAudience(e.target.value)}>
          <option value="Tous">{t('Tous les clients')}</option>
          {clients.map((c) => (
            <option key={c.code} value={c.raisonSociale}>
              {c.raisonSociale}
            </option>
          ))}
        </select>
        <label>{t('Titre')}</label>
        <input placeholder={t('Ex. Information commerciale')} value={titre} onChange={(e) => setTitre(e.target.value)} />
        <label>{t('Message')}</label>
        <textarea placeholder={t('Saisissez le message à afficher...')} value={corps} onChange={(e) => setCorps(e.target.value)} />
        <button className="primary" onClick={() => void publier()}>
          {t('Publier le message')}
        </button>
      </div>
      <div className="card">
        <h2>{t('Messages publiés')}</h2>
        <div style={{ marginTop: 14 }}>
          {messages.map((m) => (
            <div key={m.id} className={`announcement ${m.audience === 'Tous' ? '' : 'client'}`}>
              <b>{m.titre}</b> <span className="tag">{m.audience === 'Tous' ? t('Tous') : m.audience}</span>
              <div style={{ marginTop: 5 }}>{m.corps}</div>
              <div style={{ marginTop: 8 }}>
                <button className="danger" onClick={() => void supprimer(m.id)}>
                  {t('Supprimer')}
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
