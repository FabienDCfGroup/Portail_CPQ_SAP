import { useCallback, useEffect, useState } from 'react';
import { admin, api, type Client, type CommentaireFamille, type CommentaireModele, type Message } from '../api';
import { Modale } from '../composants/Modale';
import { useApp } from '../contexte';

type Onglet = 'bibliotheque' | 'tableau';

export function GestionMessages() {
  const [onglet, setOnglet] = useState<Onglet>('bibliotheque');
  return (
    <section id="messages" className="view active">
      <div className="hero">
        <h1>Gestion des messages</h1>
        <p>Gérez la bibliothèque de commentaires utilisables dans les commandes et les messages affichés sur le tableau de bord.</p>
      </div>
      <div className="message-admin-tabs">
        <button className={onglet === 'bibliotheque' ? 'active' : ''} onClick={() => setOnglet('bibliotheque')}>
          Bibliothèque de commentaires
        </button>
        <button className={onglet === 'tableau' ? 'active' : ''} onClick={() => setOnglet('tableau')}>
          Messages tableau de bord
        </button>
      </div>
      {onglet === 'bibliotheque' ? <Bibliotheque /> : <MessagesTableauBord />}
    </section>
  );
}

function Bibliotheque() {
  const { notifier } = useApp();
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
          <h2>Bibliothèque de commentaires</h2>
          <p className="muted" style={{ margin: 0 }}>
            Classez les messages par famille et définissez leur ordre d'affichage.
          </p>
        </div>
      </div>
      <div className="comment-library-toolbar">
        <input placeholder="Rechercher une famille ou un message..." value={recherche} onChange={(e) => setRecherche(e.target.value)} style={{ maxWidth: 420 }} />
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          <button className="secondary" onClick={() => setFamilleEditee({ nom: '', ordre: familles.length + 1 })}>
            Créer une famille
          </button>
          <button className="primary" onClick={() => setModeleEdite({ familleId: familles[0]?.id, ordre: 1, texte: '' })}>
            Créer un message
          </button>
        </div>
      </div>
      <div className="table-wrap comment-library-table">
        <table>
          <thead>
            <tr>
              <th>Famille</th>
              <th>Ordre famille</th>
              <th>Message</th>
              <th>Ordre message</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {lignes.length === 0 && (
              <tr>
                <td colSpan={5} className="muted">
                  Aucun résultat.
                </td>
              </tr>
            )}
            {lignes.map(({ famille: f, modele: m }) => (
              <tr key={`${f.id}-${m?.id ?? 'vide'}`}>
                <td>
                  <span className="comment-family-badge">{f.nom}</span>
                </td>
                <td>{f.ordre}</td>
                <td className={m ? '' : 'muted'}>{m ? m.texte : 'Aucun message'}</td>
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
                          Modifier
                        </button>
                        <button
                          className="danger"
                          onClick={() => window.confirm('Supprimer ce message prédéfini ?') && void executer(admin.supprimerModele(m.id))}
                        >
                          Supprimer
                        </button>
                      </>
                    ) : (
                      <>
                        <button className="secondary" onClick={() => void executer(admin.deplacerFamilleCommentaire(f.id, -1))}>
                          ↑
                        </button>
                        <button className="secondary" onClick={() => void executer(admin.deplacerFamilleCommentaire(f.id, 1))}>
                          ↓
                        </button>
                        <button className="secondary" onClick={() => setFamilleEditee(f)}>
                          Modifier
                        </button>
                        <button
                          className="danger"
                          onClick={() => window.confirm(`Supprimer la famille "${f.nom}" ?`) && void executer(admin.supprimerFamilleCommentaire(f.id))}
                        >
                          Supprimer
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
              'Famille enregistrée',
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
              'Message enregistré',
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
  const [nom, setNom] = useState(famille.nom ?? '');
  const [ordre, setOrdre] = useState(famille.ordre ?? 1);
  return (
    <Modale ouverte titre="Famille de commentaires" sousTitre="Créez ou modifiez une famille et son ordre." largeur={560} surFermer={surFermer}>
      <div className="form">
        <label>Nom de la famille</label>
        <input placeholder="Ex. Conformité" value={nom} onChange={(e) => setNom(e.target.value)} />
        <label>Ordre</label>
        <input type="number" min={1} value={ordre} onChange={(e) => setOrdre(Number(e.target.value) || 1)} />
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 14 }}>
          <button className="secondary" onClick={surFermer}>
            Annuler
          </button>
          <button className="primary" onClick={() => surEnregistrer(nom, ordre)}>
            Enregistrer
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
  const [familleId, setFamilleId] = useState(modele.familleId ?? familles[0]?.id ?? 0);
  const [ordre, setOrdre] = useState(modele.ordre ?? 1);
  const [texte, setTexte] = useState(modele.texte ?? '');
  return (
    <Modale ouverte titre="Message prédéfini" sousTitre="Classez le message dans une famille et définissez sa priorité." largeur={680} surFermer={surFermer}>
      <div className="form">
        <label>Famille</label>
        <select value={familleId} onChange={(e) => setFamilleId(Number(e.target.value))}>
          {familles.map((f) => (
            <option key={f.id} value={f.id}>
              {f.nom}
            </option>
          ))}
        </select>
        <label>Ordre dans la famille</label>
        <input type="number" min={1} value={ordre} onChange={(e) => setOrdre(Number(e.target.value) || 1)} />
        <label>Message</label>
        <textarea rows={5} placeholder="Texte du commentaire..." value={texte} onChange={(e) => setTexte(e.target.value)} />
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 14 }}>
          <button className="secondary" onClick={surFermer}>
            Annuler
          </button>
          <button className="primary" onClick={() => surEnregistrer(familleId, ordre, texte)}>
            Enregistrer
          </button>
        </div>
      </div>
    </Modale>
  );
}

function MessagesTableauBord() {
  const { notifier } = useApp();
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
    if (!titre.trim() || !corps.trim()) return notifier('Complétez le titre et le message');
    try {
      await admin.publierMessage(audience, titre, corps);
      setTitre('');
      setCorps('');
      notifier('Message publié');
      await charger();
    } catch (e) {
      notifier((e as Error).message);
    }
  }

  async function supprimer(id: number) {
    await admin.supprimerMessage(id);
    notifier('Message supprimé');
    await charger();
  }

  return (
    <div className="admin-grid">
      <div className="card form">
        <h2>Nouveau message tableau de bord</h2>
        <label>Audience</label>
        <select value={audience} onChange={(e) => setAudience(e.target.value)}>
          <option value="Tous">Tous les clients</option>
          {clients.map((c) => (
            <option key={c.code} value={c.raisonSociale}>
              {c.raisonSociale}
            </option>
          ))}
        </select>
        <label>Titre</label>
        <input placeholder="Ex. Information commerciale" value={titre} onChange={(e) => setTitre(e.target.value)} />
        <label>Message</label>
        <textarea placeholder="Saisissez le message à afficher..." value={corps} onChange={(e) => setCorps(e.target.value)} />
        <button className="primary" onClick={() => void publier()}>
          Publier le message
        </button>
      </div>
      <div className="card">
        <h2>Messages publiés</h2>
        <div style={{ marginTop: 14 }}>
          {messages.map((m) => (
            <div key={m.id} className={`announcement ${m.audience === 'Tous' ? '' : 'client'}`}>
              <b>{m.titre}</b> <span className="tag">{m.audience}</span>
              <div style={{ marginTop: 5 }}>{m.corps}</div>
              <div style={{ marginTop: 8 }}>
                <button className="danger" onClick={() => void supprimer(m.id)}>
                  Supprimer
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
