import { useEffect, useState } from 'react';
import { api, type ActionOuverture, type Client, type TypeDossier } from '../api';
import { Modale } from '../composants/Modale';
import { useApp } from '../contexte';

/**
 * Ouverture d'une configuration existante par son ConfigId, en modification ou en consultation
 * (équivalent des modes Modification / Consultation de l'ancien outil de lancement).
 */
export function OuvertureConfiguration({ surFermer }: { surFermer: () => void }) {
  const app = useApp();
  const [type, setType] = useState<TypeDossier>('Commande');
  const [action, setAction] = useState<ActionOuverture>('Modification');
  const [configId, setConfigId] = useState('');
  const [codeClient, setCodeClient] = useState('');
  const [clients, setClients] = useState<Client[]>([]);
  const [erreur, setErreur] = useState<string | null>(null);
  const [occupe, setOccupe] = useState(false);
  const envCourant = app.session.environnements.find((e) => e.code === app.environnement);

  // Suggestions de clients SAP pendant la saisie du code.
  useEffect(() => {
    const minuteur = window.setTimeout(() => {
      api.clients(codeClient.trim() || undefined).then(setClients).catch(() => undefined);
    }, 250);
    return () => window.clearTimeout(minuteur);
  }, [codeClient]);

  async function ouvrir() {
    const id = configId.trim();
    const code = codeClient.trim();
    if (!id || !code) {
      setErreur('Saisissez le ConfigId et le code client');
      return;
    }
    // Fenêtre ouverte au clic pour ne pas être bloquée par le navigateur, puis dirigée vers le configurateur.
    const fenetre = window.open('', 'CFConfigurator', 'width=1500,height=900,resizable=yes,scrollbars=yes');
    if (!fenetre) {
      setErreur('Autorisez les fenêtres popup pour ouvrir le configurateur.');
      return;
    }
    fenetre.document.write(
      `<p style="font-family:Segoe UI,Arial;text-align:center;margin-top:40vh">Ouverture du configurateur en ${action === 'Modification' ? 'modification' : 'consultation'}…</p>`,
    );
    setOccupe(true);
    setErreur(null);
    try {
      const lancement = await api.ouvrirConfiguration({ type, action, configId: id, codeClient: code, environnement: app.environnement });
      if (!lancement.peutOuvrir || !lancement.url) {
        fenetre.close();
        setErreur(lancement.message ?? 'Le Middleware n’a pas renvoyé d’URL de configurateur.');
        return;
      }
      fenetre.location.href = lancement.url;
      app.notifier(`${id} ouvert en ${action === 'Modification' ? 'modification' : 'consultation'} (${type.toLowerCase()})`);
      surFermer();
    } catch (e) {
      fenetre.close();
      setErreur((e as Error).message);
    } finally {
      setOccupe(false);
    }
  }

  return (
    <Modale
      ouverte
      titre="Ouvrir une configuration existante"
      sousTitre="Rouvrez une configuration de commande ou de devis à partir de son ConfigId, en modification ou en consultation."
      largeur={680}
      classeBoite={occupe ? 'busy' : ''}
      surFermer={surFermer}
    >
      <div className="form">
        <label>Type</label>
        <div className="choice-row">
          {(['Commande', 'Devis'] as TypeDossier[]).map((t) => (
            <label key={t} className="choice">
              <input type="radio" name="ouverture-type" checked={type === t} onChange={() => setType(t)} />
              {t}
            </label>
          ))}
        </div>
        <label>Mode</label>
        <div className="choice-row">
          {(['Modification', 'Consultation'] as ActionOuverture[]).map((a) => (
            <label key={a} className="choice">
              <input type="radio" name="ouverture-action" checked={action === a} onChange={() => setAction(a)} />
              {a}
            </label>
          ))}
        </div>
        <div className="catalog-form-grid">
          <div>
            <label>ConfigId *</label>
            <input className="config-id" placeholder="Ex. Volet_00000561" value={configId} onChange={(e) => setConfigId(e.target.value)} />
          </div>
          <div>
            <label>Code client *</label>
            <input list="ouverture-clients" placeholder="Ex. 102466" value={codeClient} onChange={(e) => setCodeClient(e.target.value)} />
            <datalist id="ouverture-clients">
              {clients.map((c) => (
                <option key={c.code} value={c.code}>
                  {c.raisonSociale}
                </option>
              ))}
            </datalist>
          </div>
        </div>
        <div className="info-ligne">
          Le configurateur est déduit du ConfigId. Environnement utilisé : <b>{envCourant?.libelle ?? app.environnement}</b>
        </div>
        {erreur && <div className="erreur-ligne">{erreur}</div>}
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 18 }}>
          <button className="secondary" onClick={surFermer}>
            Annuler
          </button>
          <button className="primary" disabled={occupe} onClick={() => void ouvrir()}>
            Ouvrir le configurateur
          </button>
        </div>
      </div>
    </Modale>
  );
}
