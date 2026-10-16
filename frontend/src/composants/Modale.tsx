import type { CSSProperties, ReactNode } from 'react';
import { useApp } from '../contexte';

interface Props {
  id?: string;
  ouverte: boolean;
  titre: ReactNode;
  sousTitre?: ReactNode;
  largeur?: number;
  classeBoite?: string;
  surFermer: () => void;
  enTete?: ReactNode;
  children: ReactNode;
}

/** Fenêtre modale avec le style de la maquette (.modal / .modal-box). */
export function Modale({ id, ouverte, titre, sousTitre, largeur = 980, classeBoite, surFermer, enTete, children }: Props) {
  const { t } = useApp();
  const style: CSSProperties = { width: `min(${largeur}px,96vw)` };
  return (
    <div id={id} className={`modal ${ouverte ? 'open' : ''}`}>
      {ouverte && (
        <div className={`modal-box ${classeBoite ?? ''}`} style={style}>
          <div className="modal-head">
            <div>
              <h2 style={{ margin: 0 }}>{titre}</h2>
              {sousTitre && <p className="muted">{sousTitre}</p>}
              {enTete}
            </div>
            <button className="close" onClick={surFermer} aria-label={t('Fermer')}>
              ×
            </button>
          </div>
          {children}
        </div>
      )}
    </div>
  );
}
