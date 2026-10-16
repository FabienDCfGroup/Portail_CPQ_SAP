import { useEffect, useState } from 'react';
import { admin, type Connexion } from '../api';
import { libellesRole, useApp, type Role } from '../contexte';
import { dateHeure } from '../outils';

const libellesResultat: Record<Connexion['resultat'], string> = {
  Succes: 'Connexion réussie',
  RefuseSansGroupe: 'Refusée : aucun groupe du portail',
  Echec: 'Échec de l’authentification',
};

const classeResultat = (r: Connexion['resultat']) => (r === 'Succes' ? 'green' : r === 'RefuseSansGroupe' ? 'orange' : 'red');

/**
 * Journal des connexions (administrateurs) : qui s'est connecté, quand et avec quel rôle.
 * Les utilisateurs et leurs rôles sont gérés dans Microsoft Entra ID, par les groupes du portail.
 */
export function JournalConnexions() {
  const { notifier, t } = useApp();
  const [connexions, setConnexions] = useState<Connexion[] | null>(null);
  const [recherche, setRecherche] = useState('');
  const [resultat, setResultat] = useState('');
  const [du, setDu] = useState('');
  const [au, setAu] = useState('');

  // Filtres appliqués côté serveur (le journal grossit à chaque connexion), après une courte pause de frappe.
  useEffect(() => {
    const minuteur = window.setTimeout(() => {
      admin
        .connexions({ q: recherche.trim() || undefined, resultat: resultat || undefined, du: du || undefined, au: au || undefined })
        .then(setConnexions)
        .catch((e: Error) => notifier(e.message));
    }, 300);
    return () => window.clearTimeout(minuteur);
  }, [recherche, resultat, du, au, notifier]);

  return (
    <section id="connexions" className="view active">
      <div className="hero">
        <h1>{t('Journal des connexions')}</h1>
        <p>{t('Les utilisateurs se connectent avec leur compte Microsoft. Leur rôle dépend du groupe Entra ID auquel ils appartiennent.')}</p>
      </div>
      <div className="card" style={{ marginTop: 18 }}>
        <h2>{t('Règles d’accès')}</h2>
        <div className="permissions">
          <div className="perm">
            <b>{t('Super administrateur')}</b>
            <span className="muted">{t('Groupe Super administrateur : droits de l’administrateur, plus la déclaration des configurateurs du Middleware.')}</span>
          </div>
          <div className="perm">
            <b>{t('Administrateur')}</b>
            <span className="muted">{t('Groupe Administrateur : tous les onglets, catalogue, droits clients, messages et journal des connexions.')}</span>
          </div>
          <div className="perm">
            <b>{t('Utilisateur')}</b>
            <span className="muted">{t('Groupe Utilisateur : Création commande/devis et Suivi des commandes.')}</span>
          </div>
          <div className="perm">
            <b>{t('Aucun de ces groupes')}</b>
            <span className="muted">{t('Accès refusé ; la tentative est tracée dans le journal.')}</span>
          </div>
        </div>
      </div>
      <div className="toolbar" style={{ marginTop: 18 }}>
        <input placeholder={t('Nom ou compte...')} value={recherche} onChange={(e) => setRecherche(e.target.value)} />
        <select value={resultat} onChange={(e) => setResultat(e.target.value)}>
          <option value="">{t('Tous les résultats')}</option>
          {(Object.keys(libellesResultat) as Connexion['resultat'][]).map((r) => (
            <option key={r} value={r}>
              {t(libellesResultat[r])}
            </option>
          ))}
        </select>
        <input type="date" value={du} onChange={(e) => setDu(e.target.value)} title={t('Du')} />
        <input type="date" value={au} onChange={(e) => setAu(e.target.value)} title={t('Au')} />
      </div>
      <div className="card table-wrap">
        <table>
          <thead>
            <tr>
              <th>{t('Date')}</th>
              <th>{t('Nom')}</th>
              <th>{t('Compte')}</th>
              <th>{t('Rôle')}</th>
              <th>{t('Résultat')}</th>
              <th>{t('Adresse IP')}</th>
            </tr>
          </thead>
          <tbody>
            {(connexions ?? []).map((c) => (
              <tr key={c.id}>
                <td>{dateHeure(c.date)}</td>
                <td>{c.nom ?? '—'}</td>
                <td>{c.compte ?? '—'}</td>
                <td>{c.role ? t(libellesRole[c.role as Role] ?? c.role) : '—'}</td>
                <td>
                  <span className={`pill ${classeResultat(c.resultat)}`} title={c.message ?? undefined}>
                    {t(libellesResultat[c.resultat])}
                  </span>
                </td>
                <td title={c.navigateur ?? undefined}>{c.adresseIp ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
        {connexions?.length === 0 && (
          <p className="muted" style={{ textAlign: 'center' }}>
            {t('Aucune connexion ne correspond aux filtres.')}
          </p>
        )}
        {connexions && connexions.length >= 500 && (
          <p className="muted" style={{ textAlign: 'center' }}>
            {t('Seules les 500 connexions les plus récentes sont affichées : affinez les filtres.')}
          </p>
        )}
      </div>
    </section>
  );
}
