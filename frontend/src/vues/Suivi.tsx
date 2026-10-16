import { useEffect, useMemo, useState } from 'react';
import { api, libellesOrigine, libellesStatut, type DossierSuivi, type StatutDossier } from '../api';
import { afficherAttente } from '../composants/Chargement';
import { useApp } from '../contexte';
import { locale } from '../i18n';
import { dateCourte, dateHeure, nomCourt } from '../outils';
import type { OuvertureAssistant } from './AssistantDossier';

const classeStatut = (s: StatutDossier) =>
  s === 'Erreur' || s === 'Abandonne' ? 'red' : s === 'EnAttente' || s === 'Envoye' ? 'orange' : s === 'EnCours' ? 'blue' : 'green';

/**
 * Suivi des commandes et devis (maquette) : dossiers du portail, complétés par les données de l'ERP
 * (statut, production, livraison) dès que le flux ERP a transmis la commande.
 */
export function Suivi({ ouvrir, version }: { ouvrir: (o: OuvertureAssistant) => void; version: number }) {
  const { notifier, t, suivreConfigurateur } = useApp();
  const [dossiers, setDossiers] = useState<DossierSuivi[] | null>(null);
  const [recherche, setRecherche] = useState('');
  const [type, setType] = useState('');
  const [statut, setStatut] = useState('');
  const [famille, setFamille] = useState('');

  useEffect(() => {
    api.suivi().then(setDossiers).catch((e: Error) => notifier(e.message));
  }, [version, notifier]);

  const familles = useMemo(
    () => [...new Set((dossiers ?? []).map((d) => d.nomFamille).filter(Boolean))].sort((a, b) => a.localeCompare(b, locale())),
    [dossiers],
  );

  const lignes = (dossiers ?? []).filter((d) => {
    const q = recherche.trim().toLowerCase();
    const texte = [d.reference, d.nomClient, d.codeClient, d.nomProduit, d.nomFamille, d.creePar, d.referenceClient, d.numeroSap, d.configId, libellesStatut[d.statut], d.erp?.statutErp]
      .join(' ')
      .toLowerCase();
    // Les dossiers abandonnés (configurateur fermé avant la fin) sont gardés pour trace, visibles seulement avec leur filtre.
    const statutVisible = statut ? d.statut === statut : d.statut !== 'Abandonne';
    return (!q || texte.includes(q)) && (!type || d.type === type) && statutVisible && (!famille || d.nomFamille === famille);
  });

  /** Dossier transmis : ouverture de sa configuration en consultation (lecture seule). */
  async function consulter(d: DossierSuivi) {
    const fenetre = window.open('', 'CFConfigurator', 'width=1500,height=900,resizable=yes,scrollbars=yes');
    if (!fenetre) return notifier(t('Autorisez les fenêtres popup pour ouvrir le configurateur.'));
    afficherAttente(fenetre, t('{configId} — consultation', { configId: d.configId ?? '' }), t('Le Middleware CPQ prépare la session'));
    try {
      const { lancement } = await api.consulterDossier(d.id);
      if (!lancement.peutOuvrir || !lancement.url) {
        fenetre.close();
        notifier(lancement.message ?? t('Consultation impossible'));
        return;
      }
      fenetre.location.href = lancement.url;
      void suivreConfigurateur(fenetre, t('{configId} — consultation', { configId: d.configId ?? '' }));
    } catch (e) {
      fenetre.close();
      notifier((e as Error).message);
    }
  }

  /** Devis transmis : création (ou reprise) de la commande issue du devis, puis ouverture dans l'assistant. */
  async function transformer(d: DossierSuivi) {
    try {
      const commande = await api.transformerEnCommande(d.id);
      notifier(t('Commande {commande} créée à partir du devis {devis} : ouvrez la configuration en commande puis validez-la.', { commande: commande.reference, devis: d.reference }));
      ouvrir({ dossierId: commande.id });
    } catch (e) {
      notifier((e as Error).message);
    }
  }

  function action(d: DossierSuivi) {
    if (d.statut === 'EnCours') {
      return (
        <button className="primary" onClick={() => ouvrir({ dossierId: d.id })}>
          {t('Ouvrir')}
        </button>
      );
    }
    if (d.statut === 'AValider') {
      return (
        <button className="primary" onClick={() => ouvrir({ dossierId: d.id })}>
          {t('Ouvrir pour validation')}
        </button>
      );
    }
    if (d.statut === 'Abandonne') return null;
    const consultation = d.configId ? (
      <button className="secondary" onClick={() => void consulter(d)} title={t('Ouvrir la configuration en consultation')}>
        {t('Consulter')}
      </button>
    ) : null;
    if (d.type === 'Devis' && d.commandeIssueReference) {
      return (
        <div className="actions-suivi">
          {consultation}
          <span className="muted" title={t('Commande issue du devis')}>→ {d.commandeIssueReference}</span>
        </div>
      );
    }
    if (d.type === 'Devis' && d.configId && (d.statut === 'Envoye' || d.statut === 'Validee')) {
      return (
        <div className="actions-suivi">
          {consultation}
          <button
            className="primary"
            onClick={() => void transformer(d)}
            title={d.commandeEnCoursReference ? t('Reprendre la commande {commande}, en cours de transformation', { commande: d.commandeEnCoursReference }) : undefined}
          >
            {d.commandeEnCoursReference ? t('Reprendre la transformation') : t('Transformer en commande')}
          </button>
        </div>
      );
    }
    return consultation;
  }

  return (
    <section id="tracking" className="view active">
      <div className="announcement" style={{ marginBottom: 14 }}>
        <b>{t('Origine des commandes')}</b>
        <div style={{ marginTop: 5 }}>
          {/* Les repères {creationInterne} et {siteInternet} de la phrase traduite sont remplacés par leurs libellés en gras. */}
          {t('Les commandes peuvent être créées dans CF Saisie commandes ({creationInterne}) ou intégrées depuis un autre processus, par exemple le {siteInternet}.')
            .split(/\{(\w+)\}/)
            .map((morceau, i) =>
              i % 2 ? <b key={i}>{morceau === 'creationInterne' ? t('Création interne') : t('Site internet')}</b> : morceau,
            )}{' '}
          {t('Les colonnes ERP (statut, production, livraison) se remplissent dès que l’ERP a transmis la commande.')}
        </div>
      </div>
      <div className="hero">
        <h1>{t('Suivi des commandes')}</h1>
        <p>{t('Filtrez les commandes et devis, rouvrez un dossier en cours ou consultez la configuration d’un dossier transmis.')}</p>
      </div>
      <div className="toolbar">
        <input placeholder={t('Référence, client, produit, n° SAP...')} value={recherche} onChange={(e) => setRecherche(e.target.value)} />
        <select value={type} onChange={(e) => setType(e.target.value)}>
          <option value="">{t('Commande et/ou devis')}</option>
          <option value="Commande">{t('Commande')}</option>
          <option value="Devis">{t('Devis')}</option>
        </select>
        <select value={statut} onChange={(e) => setStatut(e.target.value)}>
          <option value="">{t('Tous les statuts')}</option>
          {(Object.keys(libellesStatut) as StatutDossier[]).map((s) => (
            <option key={s} value={s}>
              {t(libellesStatut[s])}
            </option>
          ))}
        </select>
        <select value={famille} onChange={(e) => setFamille(e.target.value)}>
          <option value="">{t('Toutes les familles')}</option>
          {familles.map((f) => (
            <option key={f}>{f}</option>
          ))}
        </select>
      </div>
      <div className="card table-wrap">
        <table>
          <thead>
            <tr>
              <th>{t('Type')}</th>
              <th>{t('Origine')}</th>
              <th>{t('Date de création')}</th>
              <th>{t('Créé par')}</th>
              <th>{t('Code client')}</th>
              <th>{t('Nom client')}</th>
              <th>{t('Référence client')}</th>
              <th>{t('N° commande SAP')}</th>
              <th>{t('Statut')}</th>
              <th>{t('Statut ERP')}</th>
              <th>{t('Fin de prod')}</th>
              <th>{t('Livraison prévue')}</th>
              <th>{t('Livraison réelle')}</th>
              <th>{t('Action')}</th>
            </tr>
          </thead>
          <tbody>
            {lignes.map((d) => {
              const web = d.origine === 'SiteInternet';
              // Fin de production : celle de l'ERP quand il l'a transmise, sinon celle choisie dans le portail.
              const finProd = d.erp?.finProduction ?? d.dateFinProductionSaisie;
              return (
                <tr key={d.id}>
                  <td>
                    <span className={`pill ${d.type === 'Devis' ? 'orange' : 'blue'}`} title={`${d.reference} · ${d.nomProduit}${d.configId ? ` · ${d.configId}` : ''}${d.devisSourceReference ? ` · ${t('issue du devis {devis}', { devis: d.devisSourceReference })}` : ''}`}>
                      {t(d.type)}
                    </span>
                  </td>
                  <td>
                    <span className={`pill ${web ? 'orange' : 'blue'}`}>{t(libellesOrigine[d.origine])}</span>
                  </td>
                  <td>{dateHeure(d.dateCreation)}</td>
                  <td>{nomCourt(d.creePar)}</td>
                  <td>
                    <b>{d.codeClient || '—'}</b>
                  </td>
                  <td>{d.nomClient || '—'}</td>
                  <td>{d.referenceClient ?? '—'}</td>
                  <td>{d.numeroSap ?? '—'}</td>
                  <td>
                    <span className={`pill ${classeStatut(d.statut)}`}>{t(libellesStatut[d.statut])}</span>
                  </td>
                  <td>{d.erp?.statutErp ?? '—'}</td>
                  <td title={finProd ? (d.erp?.finProduction ? t('Date ERP') : t('Date choisie dans le portail')) : undefined}>{dateCourte(finProd)}</td>
                  <td>{dateCourte(d.erp?.livraisonPrevue)}</td>
                  <td>{dateCourte(d.erp?.livraisonReelle)}</td>
                  <td>{action(d)}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
        {dossiers && lignes.length === 0 && (
          <p className="muted" style={{ textAlign: 'center' }}>
            {dossiers.length === 0 ? t('Aucune commande ni aucun devis pour le moment.') : t('Aucun dossier ne correspond aux filtres.')}
          </p>
        )}
        {!dossiers && <p className="muted" style={{ textAlign: 'center' }}>{t('Chargement du suivi…')}</p>}
      </div>
    </section>
  );
}
