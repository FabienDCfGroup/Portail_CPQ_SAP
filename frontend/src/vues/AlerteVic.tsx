import { Modale } from '../composants/Modale';

/** Avertissement avant une action sur une commande d'un client en paiement VIC (paiement à la commande). */
export function AlerteVic({ surContinuer, surFermer }: { surContinuer: () => void; surFermer: () => void }) {
  return (
    <Modale ouverte titre="Attention" largeur={560} surFermer={surFermer}>
      <div style={{ padding: '8px 4px 4px', textAlign: 'center' }}>
        <div style={{ fontSize: 42, marginBottom: 8 }}>⚠️</div>
        <p style={{ fontSize: 18, fontWeight: 800, color: 'var(--navy)', margin: '0 0 18px' }}>Attention, ce client est en mode de paiement VIC</p>
        <div style={{ display: 'flex', justifyContent: 'center', gap: 10, flexWrap: 'wrap' }}>
          <button className="primary" onClick={surContinuer}>
            Continuer
          </button>
          <button className="secondary" onClick={surFermer}>
            Fermer
          </button>
        </div>
      </div>
    </Modale>
  );
}
