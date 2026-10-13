import { useEffect, useState } from 'react';
import { api, type CommentaireFamille } from '../api';
import { Modale } from '../composants/Modale';

/** « Insérer un message » : choix d'un commentaire prédéfini dans la bibliothèque, par famille. */
export function SelecteurCommentaire({ cible, surChoix, surFermer }: { cible: string; surChoix: (texte: string) => void; surFermer: () => void }) {
  const [familles, setFamilles] = useState<CommentaireFamille[]>([]);
  const [familleId, setFamilleId] = useState<number | null>(null);
  const [recherche, setRecherche] = useState('');

  useEffect(() => {
    api.commentaires().then((f) => {
      setFamilles(f);
      setFamilleId(f[0]?.id ?? null);
    });
  }, []);

  const famille = familles.find((f) => f.id === familleId);
  const messages = (famille?.modeles ?? []).filter((m) => !recherche.trim() || m.texte.toLowerCase().includes(recherche.trim().toLowerCase()));

  return (
    <Modale ouverte titre="Insérer un message" sousTitre="Choisissez une famille puis un message. Le texte restera modifiable après insertion." largeur={920} surFermer={surFermer}>
      <div className="comment-picker-layout">
        <div className="comment-picker-families">
          {familles.map((f) => (
            <button key={f.id} type="button" className={f.id === familleId ? 'active' : ''} onClick={() => setFamilleId(f.id)}>
              <span>{f.nom}</span>
              <small style={{ display: 'block', color: 'var(--muted)', fontWeight: 600, marginTop: 3 }}>{f.modeles.length} message(s)</small>
            </button>
          ))}
        </div>
        <div>
          <input placeholder="Rechercher un message..." value={recherche} onChange={(e) => setRecherche(e.target.value)} style={{ width: '100%', marginBottom: 10 }} />
          {messages.length === 0 && <div className="muted">Aucun message dans cette famille.</div>}
          {messages.map((m) => (
            <button key={m.id} type="button" className="comment-picker-message" onClick={() => surChoix(m.texte)}>
              {m.texte}
              <small>Cliquer pour insérer dans {cible}</small>
            </button>
          ))}
        </div>
      </div>
    </Modale>
  );
}
