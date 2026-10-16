import { useCallback, useEffect, useState } from 'react';
import { api, type JourPlanning, type PlanningDossier } from '../api';
import { useApp } from '../contexte';
import { locale } from '../i18n';
import { dateLongue } from '../outils';

const classeEtat: Record<JourPlanning['etat'], string> = { Disponible: 'available', Limite: 'limited', Complet: 'unavailable', AvantDelai: 'unavailable' };

// Nombre de jours ouvrés affichés à la fois, comme la maquette.
const PAGE = 21;

// Dates AAAA-MM-JJ sans fuseau : jamais converties en UTC (un décalage d'un jour serait une erreur coûteuse).
const dateLocale = (iso: string) => new Date(`${iso}T00:00:00`);
const aujourdhui = () => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
};

/**
 * Date de fin de production. Le code CCX lu dans le XML de fin de configuration décide de l'affichage :
 * calendrier de l'API Planning CCX s'il existe, date libre sinon.
 */
export function PlanningProduction({ dossierId, valeur, surChoix }: { dossierId: number; valeur: string | null; surChoix: (d: string | null) => void }) {
  const { t } = useApp();
  const [planning, setPlanning] = useState<PlanningDossier | null>(null);
  const [erreur, setErreur] = useState<string | null>(null);
  const [chargement, setChargement] = useState(true);
  const [affiches, setAffiches] = useState(PAGE);

  // Pas de cache : la charge évolue au fil des prises de commande, le planning est relu à chaque ouverture.
  const charger = useCallback(async () => {
    setChargement(true);
    setErreur(null);
    try {
      setPlanning(await api.planningDossier(dossierId));
      setAffiches(PAGE);
    } catch (e) {
      setErreur((e as Error).message);
    } finally {
      setChargement(false);
    }
  }, [dossierId]);

  useEffect(() => {
    void charger();
  }, [charger]);

  const actualiser = (
    <button type="button" className="secondary" disabled={chargement} onClick={() => void charger()}>
      {chargement ? t('Lecture…') : t('Actualiser')}
    </button>
  );

  if (!planning) {
    return (
      <div className="production-planning">
        {erreur ? <div className="erreur-ligne">{erreur}</div> : <p className="muted">{t('Lecture de la configuration et du planning de fabrication…')}</p>}
        {erreur && <div style={{ textAlign: 'center', marginTop: 10 }}>{actualiser}</div>}
      </div>
    );
  }

  if (planning.mode === 'ConfigurationNonEnregistree') {
    return (
      <div className="production-planning">
        <div className="info-ligne">{planning.message}</div>
        <div style={{ textAlign: 'center', marginTop: 10 }}>{actualiser}</div>
      </div>
    );
  }

  if (planning.mode === 'Libre') {
    return (
      <div className="production-planning">
        {planning.message && <div className="info-ligne" style={{ marginBottom: 10 }}>{planning.message}</div>}
        <p className="muted" style={{ marginTop: 0 }}>{t('Pas de planning de fabrication pour ce produit : saisissez la date de fin de production.')}</p>
        <input type="date" min={aujourdhui()} value={valeur ?? ''} onChange={(e) => surChoix(e.target.value || null)} />
        {/* « min » n'empêche pas de taper une date passée au clavier : elle est signalée ici et refusée par l'API. */}
        {valeur && valeur < aujourdhui() && <div className="erreur-ligne">{t("La date de fin de production ne peut pas être antérieure à aujourd'hui.")}</div>}
        {valeur && valeur >= aujourdhui() && <div className="production-selected">{t('Date de fin de prod saisie : {date}', { date: dateLongue(valeur) })}</div>}
        {erreur && <div className="erreur-ligne">{erreur}</div>}
      </div>
    );
  }

  // Les jours avant le délai minimum ne sont pas proposés ; la première date autorisée est indiquée dans l'en-tête.
  const jours = planning.jours.filter((j) => j.etat !== 'AvantDelai');
  const valeurDisponible = !valeur || jours.some((j) => j.date === valeur && j.selectionnable);

  function choisir(j: JourPlanning) {
    if (!j.selectionnable) return;
    if (j.confirmationRequise && !window.confirm(t('Fin de production le jour même ({date}) : confirmez-vous ce choix ?', { date: dateLongue(j.date) }))) return;
    surChoix(j.date);
  }

  return (
    <div className="production-planning">
      <div className="production-planning-head">
        <div>
          <h4>{t('Planning de disponibilité fabrication')}</h4>
          <p>
            {t('Ligne {ligne} ({code}) · site {site}', { ligne: planning.libelleCcx ?? planning.codeCcx ?? '', code: planning.codeCcx ?? '', site: planning.site ?? '' })}
            {planning.delaiMiniJoursOuvres ? t(' · délai minimum {jours} jours ouvrés', { jours: planning.delaiMiniJoursOuvres }) : ''}
            {planning.premiereDateAutorisee ? t(' · au plus tôt le {date}', { date: dateLocale(planning.premiereDateAutorisee).toLocaleDateString(locale()) }) : ''}
          </p>
        </div>
        <div className="production-legend">
          <span>
            <i className="production-dot" style={{ background: '#27ae60' }} />
            {t('Disponible')}
          </span>
          <span>
            <i className="production-dot" style={{ background: '#f5a623' }} />
            {t('Places limitées')}
          </span>
          <span>
            <i className="production-dot" style={{ background: '#aab4bf' }} />
            {t('Complet')}
          </span>
          {actualiser}
        </div>
      </div>
      {planning.chargeIncomplete && (
        <div className="info-ligne" style={{ marginBottom: 10 }}>
          {t('Charge calculée sur {origines} uniquement : les fabrications déjà passées dans SAP ne sont pas encore comptées.', { origines: planning.origines.join(', ') || 'AX' })}
        </div>
      )}
      {erreur && <div className="erreur-ligne">{erreur}</div>}
      {jours.length === 0 ? (
        // Choix validé : pas de date libre dans ce cas, la commande reste bloquée tant que l'atelier n'a pas de capacité.
        <div className="erreur-ligne">
          {planning.jours.length === 0
            ? t("La ligne {ligne} ({code}) n'a aucune capacité de fabrication paramétrée sur l'horizon du planning : aucune date ne peut être choisie et la validation est impossible. Contactez l'atelier ou l'ADV.", { ligne: planning.libelleCcx ?? planning.codeCcx ?? '', code: planning.codeCcx ?? '' })
            : t("Aucune date de fabrication possible pour la ligne {ligne} ({code}) avant la fin de l'horizon du planning : aucune date ne peut être choisie et la validation est impossible. Contactez l'atelier ou l'ADV.", { ligne: planning.libelleCcx ?? planning.codeCcx ?? '', code: planning.codeCcx ?? '' })}
        </div>
      ) : (
        <div className="production-calendar">
          {jours.slice(0, affiches).map((j) => {
            const d = dateLocale(j.date);
            return (
              <button
                key={j.date}
                type="button"
                className={`production-slot ${classeEtat[j.etat]} ${valeur === j.date ? 'selected' : ''}`}
                disabled={!j.selectionnable}
                title={j.motif ?? (j.confirmationRequise ? t('Date du jour : confirmation demandée') : undefined)}
                onClick={() => choisir(j)}
              >
                <div className="day">{d.toLocaleDateString(locale(), { weekday: 'short' })}</div>
                <div className="date">{d.toLocaleDateString(locale(), { day: '2-digit', month: '2-digit' })}</div>
                <div className="capacity">{j.libelle}</div>
              </button>
            );
          })}
        </div>
      )}
      {affiches < jours.length && (
        <div style={{ textAlign: 'center', marginTop: 10 }}>
          <button type="button" className="secondary" onClick={() => setAffiches((n) => n + PAGE)}>
            {t('Voir les jours suivants')}
          </button>
        </div>
      )}
      <div className="production-selected">
        {valeur ? t('Date de fin de prod sélectionnée : {date}', { date: dateLongue(valeur) }) : t('Aucune date sélectionnée')}
        {!valeurDisponible && <div className="erreur-ligne">{t("Cette date n'est plus disponible : choisissez-en une autre.")}</div>}
      </div>
    </div>
  );
}
