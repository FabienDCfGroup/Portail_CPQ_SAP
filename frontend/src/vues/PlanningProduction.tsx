import { useEffect, useState } from 'react';
import { api, type Planning } from '../api';
import { dateLongue } from '../outils';

const classeEtat = { Disponible: 'available', Limitee: 'limited', Complet: 'unavailable' } as const;

/** Planning de disponibilité fabrication : 21 jours ouvrés à partir du délai du produit (données SAP). */
export function PlanningProduction({ produitId, valeur, surChoix }: { produitId: number; valeur: string | null; surChoix: (d: string) => void }) {
  const [planning, setPlanning] = useState<Planning | null>(null);
  const [erreur, setErreur] = useState<string | null>(null);

  useEffect(() => {
    setPlanning(null);
    api.planning(produitId).then(setPlanning).catch((e: Error) => setErreur(e.message));
  }, [produitId]);

  return (
    <div className="production-planning">
      <div className="production-planning-head">
        <div>
          <h4>Planning de disponibilité fabrication</h4>
          <p>
            {planning
              ? `${planning.produit} · délai indicatif ${planning.delaiJours} jours · choisissez un créneau disponible`
              : 'Sélectionnez une date disponible pour ce produit.'}
          </p>
        </div>
        <div className="production-legend">
          <span>
            <i className="production-dot" style={{ background: '#27ae60' }} />
            Disponible
          </span>
          <span>
            <i className="production-dot" style={{ background: '#f5a623' }} />
            Places limitées
          </span>
          <span>
            <i className="production-dot" style={{ background: '#aab4bf' }} />
            Complet
          </span>
        </div>
      </div>
      {erreur && <div className="erreur-ligne">{erreur}</div>}
      <div className="production-calendar">
        {planning?.jours.map((j) => {
          const d = new Date(`${j.date}T00:00:00`);
          const complet = j.etat === 'Complet';
          return (
            <button
              key={j.date}
              type="button"
              className={`production-slot ${classeEtat[j.etat]} ${valeur === j.date ? 'selected' : ''}`}
              disabled={complet}
              onClick={() => !complet && surChoix(j.date)}
            >
              <div className="day">{d.toLocaleDateString('fr-FR', { weekday: 'short' })}</div>
              <div className="date">{d.toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit' })}</div>
              <div className="capacity">{j.libelle}</div>
            </button>
          );
        })}
      </div>
      <div className="production-selected">{valeur ? `Date de fin de prod sélectionnée : ${dateLongue(valeur)}` : 'Aucune date sélectionnée'}</div>
    </div>
  );
}
