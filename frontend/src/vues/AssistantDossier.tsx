import { useEffect, useMemo, useRef, useState } from 'react';
import {
  api,
  type Agence,
  type Client,
  type ClientDetail,
  type Dossier,
  type FamilleCatalogue,
  type ModeLivraison,
  type ProduitCatalogue,
  type SaisieDossier,
  type TypeDossier,
} from '../api';
import { pageAttenteConfigurateur } from '../composants/Chargement';
import { Modale } from '../composants/Modale';
import { useApp } from '../contexte';
import { imageProduit, urlCss, vide } from '../outils';
import { AlerteVic } from './AlerteVic';
import { PlanningProduction } from './PlanningProduction';
import { SelecteurCommentaire } from './SelecteurCommentaire';

/** Ouverture de l'assistant : nouvelle commande / nouveau devis, ou réouverture d'un dossier existant. */
export type OuvertureAssistant = { type: TypeDossier } | { dossierId: number };

interface Champs {
  referenceChantier: string;
  ticketSalesforce: string;
  modeLivraison: ModeLivraison | '';
  agenceRetrait: string;
  livraisonNom: string;
  livraisonTelephone: string;
  livraisonAdresse: string;
  livraisonCodePostal: string;
  livraisonVille: string;
  acces: string[];
  accesAutre: string;
  dateFinProduction: string | null;
  commentaireExterne: string;
  commentaireInterne: string;
}

const champsVierges: Champs = {
  referenceChantier: '',
  ticketSalesforce: '',
  modeLivraison: '',
  agenceRetrait: '',
  livraisonNom: '',
  livraisonTelephone: '',
  livraisonAdresse: '',
  livraisonCodePostal: '',
  livraisonVille: '',
  acces: [],
  accesAutre: '',
  dateFinProduction: null,
  commentaireExterne: '',
  commentaireInterne: '',
};

const modes: ModeLivraison[] = ['Chez le client', 'A saisir', 'Retrait en agence'];
const typesAcces = ['Semi-remorque', 'Porteur', 'VL', 'Autre'];

/** Retrouve un client à partir du texte saisi (code, raison sociale ou « code — raison sociale »), comme la maquette. */
function trouverClient(clients: Client[], saisie: string) {
  const q = saisie.trim().toLowerCase();
  if (!q) return undefined;
  return (
    clients.find((c) => [c.code, c.raisonSociale, `${c.code} — ${c.raisonSociale}`, `${c.raisonSociale} — ${c.code}`].some((v) => v.toLowerCase() === q)) ??
    clients.find((c) => q.includes(c.code.toLowerCase()) || q.includes(c.raisonSociale.toLowerCase()))
  );
}


export function AssistantDossier({
  ouverture,
  surFermer,
  surValide,
}: {
  ouverture: OuvertureAssistant;
  surFermer: (modifie: boolean) => void;
  surValide: () => void;
}) {
  const app = useApp();
  const { notifier } = app;

  const [dossier, setDossier] = useState<Dossier | null>(null);
  const [type, setType] = useState<TypeDossier>('type' in ouverture ? ouverture.type : 'Commande');
  const [chargement, setChargement] = useState('dossierId' in ouverture);
  const [clients, setClients] = useState<Client[]>([]);
  const [agences, setAgences] = useState<Agence[]>([]);
  const [rechercheClient, setRechercheClient] = useState('');
  const [client, setClient] = useState<ClientDetail | null>(null);
  const [champs, setChamps] = useState<Champs>(champsVierges);
  const [clientAdresse, setClientAdresse] = useState<ClientDetail | null>(null);
  const [rechercheClientAdresse, setRechercheClientAdresse] = useState('');
  const [adresseId, setAdresseId] = useState('');
  const [catalogue, setCatalogue] = useState<FamilleCatalogue[] | null>(null);
  const [familleId, setFamilleId] = useState<number | null>(null);
  const [produit, setProduit] = useState<ProduitCatalogue | null>(null);
  const [produitRestaure, setProduitRestaure] = useState(false);
  const [finalisation, setFinalisation] = useState(false);
  const [occupe, setOccupe] = useState(false);
  const [erreur, setErreur] = useState<string | null>(null);
  const [actionVic, setActionVic] = useState<(() => void) | null>(null);
  const [cibleCommentaire, setCibleCommentaire] = useState<'commentaireExterne' | 'commentaireInterne' | null>(null);
  const modifie = useRef(false);
  const surveillance = useRef<number>();
  const zoneProduits = useRef<HTMLDivElement>(null);
  const zoneFinalisation = useRef<HTMLDivElement>(null);

  const estDevis = type === 'Devis';
  const estVic = !!client?.client.estVic;
  const maj = (partiel: Partial<Champs>) => setChamps((c) => ({ ...c, ...partiel }));

  // Chargement initial : clients et agences (SAP), puis le dossier en cas de réouverture.
  useEffect(() => {
    api.clients().then(setClients).catch((e: Error) => setErreur(e.message));
    api.agences().then(setAgences).catch(() => undefined);
    if ('dossierId' in ouverture) {
      void restaurer(ouverture.dossierId);
    }
    return () => window.clearInterval(surveillance.current);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function restaurer(id: number) {
    try {
      const d = await api.dossier(id);
      setDossier(d);
      setType(d.type);
      setChamps({
        referenceChantier: d.referenceChantier ?? '',
        ticketSalesforce: d.ticketSalesforce ?? '',
        modeLivraison: (d.modeLivraison as ModeLivraison) ?? '',
        agenceRetrait: d.agenceRetrait ?? '',
        livraisonNom: d.livraisonNom ?? '',
        livraisonTelephone: d.livraisonTelephone ?? '',
        livraisonAdresse: d.livraisonAdresse ?? '',
        livraisonCodePostal: d.livraisonCodePostal ?? '',
        livraisonVille: d.livraisonVille ?? '',
        acces: d.acces ?? [],
        accesAutre: d.accesAutre ?? '',
        dateFinProduction: d.dateFinProduction,
        commentaireExterne: d.commentaireExterne ?? '',
        commentaireInterne: d.commentaireInterne ?? '',
      });
      const detail = await api.client(d.codeClient);
      setClient(detail);
      setRechercheClient(`${detail.client.code} — ${detail.client.raisonSociale}`);
      if (d.modeLivraison === 'Chez le client') {
        const source = d.livraisonCodeClient && d.livraisonCodeClient !== d.codeClient ? await api.client(d.livraisonCodeClient) : detail;
        setClientAdresse(source);
        setRechercheClientAdresse(`${source.client.code} — ${source.client.raisonSociale}`);
        setAdresseId(source.adresses.find((a) => a.libelle === d.livraisonLibelleAdresse)?.id ?? source.adresses[0]?.id ?? '');
      }
      const cat = await api.catalogue(d.codeClient);
      setCatalogue(cat);
      const famille = cat.find((f) => f.nom === d.nomFamille) ?? cat.find((f) => f.produits.some((p) => p.id === d.produitId));
      setFamilleId(famille?.id ?? null);
      const p = cat.flatMap((f) => f.produits).find((x) => x.id === d.produitId);
      if (p || d.nomProduit) {
        setProduit(p ?? { id: d.produitId ?? 0, nom: d.nomProduit, description: 'Dernier choix enregistré dans le dossier.', image: null, configurateur: d.configurateur ?? '' });
        setProduitRestaure(true);
        setFinalisation(true);
      }
      notifier(`${d.reference} rouvert dans son dernier état`);
    } catch (e) {
      setErreur((e as Error).message);
    } finally {
      setChargement(false);
    }
  }

  // ----- Client -----
  async function choisirClient(saisie: string) {
    setRechercheClient(saisie);
    const c = trouverClient(clients, saisie);
    if (!c) {
      if (client) {
        setClient(null);
        setCatalogue(null);
        setFamilleId(null);
      }
      return;
    }
    setRechercheClient(`${c.code} — ${c.raisonSociale}`);
    if (client?.client.code === c.code) return;
    try {
      const detail = await api.client(c.code);
      setClient(detail);
      setFamilleId(null);
      setProduitRestaure(false);
      setCatalogue(await api.catalogue(c.code));
      if (champs.modeLivraison === 'Chez le client') appliquerClientAdresse(detail, true);
      if (detail.client.estVic) notifier('Client en mode de paiement VIC');
    } catch (e) {
      setErreur((e as Error).message);
    }
  }

  // ----- Livraison / retrait -----
  function appliquerClientAdresse(detail: ClientDetail | null, principale: boolean, id?: string) {
    setClientAdresse(detail);
    setRechercheClientAdresse(detail ? `${detail.client.code} — ${detail.client.raisonSociale}` : '');
    if (!detail) return;
    const adresse =
      detail.adresses.find((a) => a.id === id) ?? (principale ? detail.adresses.find((a) => a.estPrincipale) : undefined) ?? detail.adresses[0];
    setAdresseId(adresse?.id ?? '');
    if (adresse) {
      maj({
        livraisonNom: adresse.nom || detail.client.raisonSociale,
        livraisonTelephone: adresse.telephone,
        livraisonAdresse: adresse.adresse,
        livraisonCodePostal: adresse.codePostal,
        livraisonVille: adresse.ville,
      });
    }
  }

  function changerMode(mode: ModeLivraison) {
    const viderAdresse = { livraisonNom: '', livraisonTelephone: '', livraisonAdresse: '', livraisonCodePostal: '', livraisonVille: '', acces: [], accesAutre: '' };
    if (mode === 'Chez le client') {
      maj({ modeLivraison: mode, agenceRetrait: '' });
      appliquerClientAdresse(client, true);
    } else if (mode === 'A saisir') {
      setClientAdresse(null);
      setRechercheClientAdresse('');
      setAdresseId('');
      maj({ modeLivraison: mode, agenceRetrait: '', ...viderAdresse });
    } else {
      setClientAdresse(null);
      maj({ modeLivraison: mode });
    }
  }

  async function choisirClientAdresse(saisie: string) {
    setRechercheClientAdresse(saisie);
    const c = trouverClient(clients, saisie);
    if (c && c.code !== clientAdresse?.client.code) {
      appliquerClientAdresse(await api.client(c.code), true);
    }
  }

  /** Informations obligatoires encore manquantes avant de pouvoir choisir un produit. */
  const manquants = useMemo(() => {
    const liste: string[] = [];
    if (!client) liste.push('le client');
    if (!champs.referenceChantier.trim()) liste.push('la référence chantier');
    if (!champs.ticketSalesforce.trim()) liste.push('le numéro de ticket Salesforce');
    if (champs.modeLivraison === 'Chez le client' || champs.modeLivraison === 'A saisir') {
      const adresse: [string, string][] = [
        [champs.livraisonNom, 'nom'],
        [champs.livraisonTelephone, 'téléphone'],
        [champs.livraisonAdresse, 'adresse'],
        [champs.livraisonCodePostal, 'code postal'],
        [champs.livraisonVille, 'ville'],
      ];
      const vides = adresse.filter(([v]) => !v.trim()).map(([, nom]) => nom);
      if (vides.length) liste.push(`l'adresse de livraison (${vides.join(', ')})`);
    } else if (champs.modeLivraison === 'Retrait en agence') {
      if (!champs.agenceRetrait) liste.push("l'agence de retrait");
    } else {
      liste.push('le mode de livraison ou de retrait');
    }
    return liste;
  }, [client, champs]);
  const prerequisOk = manquants.length === 0;

  function saisieDto(): SaisieDossier {
    const retrait = champs.modeLivraison === 'Retrait en agence';
    const chezClient = champs.modeLivraison === 'Chez le client';
    return {
      type,
      codeClient: client?.client.code ?? null,
      referenceChantier: vide(champs.referenceChantier),
      ticketSalesforce: vide(champs.ticketSalesforce),
      modeLivraison: champs.modeLivraison || null,
      livraisonCodeClient: chezClient ? clientAdresse?.client.code ?? null : null,
      livraisonNomClient: chezClient ? clientAdresse?.client.raisonSociale ?? null : null,
      livraisonLibelleAdresse: chezClient ? clientAdresse?.adresses.find((a) => a.id === adresseId)?.libelle ?? null : null,
      livraisonNom: retrait ? null : vide(champs.livraisonNom),
      livraisonTelephone: retrait ? null : vide(champs.livraisonTelephone),
      livraisonAdresse: retrait ? null : vide(champs.livraisonAdresse),
      livraisonCodePostal: retrait ? null : vide(champs.livraisonCodePostal),
      livraisonVille: retrait ? null : vide(champs.livraisonVille),
      acces: retrait ? [] : champs.acces,
      accesAutre: retrait ? null : vide(champs.accesAutre),
      agenceRetrait: retrait ? vide(champs.agenceRetrait) : null,
      dateFinProduction: champs.dateFinProduction,
      commentaireExterne: vide(champs.commentaireExterne),
      commentaireInterne: vide(champs.commentaireInterne),
    };
  }

  // ----- Configurateur -----
  /** Ouvre le configurateur : création pour un nouveau produit, modification de la configuration existante sinon. */
  async function lancerConfigurateur(p: ProduitCatalogue, changementProduit: boolean) {
    setErreur(null);
    // La fenêtre est ouverte immédiatement (au clic) pour ne pas être bloquée par le navigateur, puis redirigée vers l'URL du Middleware.
    const fenetre = window.open('', 'CFConfigurator', 'width=1500,height=900,resizable=yes,scrollbars=yes');
    if (!fenetre) {
      notifier('Autorisez les fenêtres popup pour ouvrir le configurateur.');
      return;
    }
    fenetre.document.write(pageAttenteConfigurateur(`Ouverture du configurateur ${p.configurateur || p.nom}`));
    fenetre.document.close();
    setOccupe(true);
    try {
      let resultat;
      if (dossier) {
        await api.enregistrerDossier(dossier.id, saisieDto());
        resultat = await api.relancerConfigurateur(dossier.id, changementProduit ? p.id : null, app.environnement);
      } else {
        resultat = await api.creerDossier(saisieDto(), p.id, app.environnement);
      }
      const { lancement } = resultat;
      if (!lancement.peutOuvrir || !lancement.url) {
        fenetre.close();
        setErreur(lancement.succes ? `Message du Middleware : ${lancement.message}` : `Le lancement a échoué : ${lancement.message ?? 'réponse sans message'}`);
        if (!lancement.succes && !dossier) {
          notifier('Si le lancement est OK avec le profil « AX », vérifiez les droits dans « MyUsers ».');
        }
        return;
      }
      modifie.current = true;
      setDossier(resultat.dossier);
      setProduit(p);
      setProduitRestaure(false);
      setFinalisation(false);
      fenetre.location.href = lancement.url;
      notifier(`Configurateur ouvert (${lancement.configId}). Fermez-le après avoir enregistré la configuration.`);
      surveiller(fenetre, resultat.dossier.id);
    } catch (e) {
      fenetre.close();
      setErreur((e as Error).message);
    } finally {
      setOccupe(false);
    }
  }

  /** Comme la maquette : la fermeture de la fenêtre du configurateur ouvre la finalisation. */
  function surveiller(fenetre: Window, idDossier: number) {
    window.clearInterval(surveillance.current);
    surveillance.current = window.setInterval(async () => {
      if (!fenetre.closed) return;
      window.clearInterval(surveillance.current);
      try {
        setDossier(await api.configurationEnregistree(idDossier));
      } catch {
        // Sans effet sur la saisie : la finalisation reste possible.
      }
      setFinalisation(true);
      notifier(estDevis ? 'Configuration enregistrée. Vous pouvez finaliser le devis.' : 'Configuration enregistrée. Vous pouvez finaliser la commande.');
      requestAnimationFrame(() => zoneFinalisation.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' }));
    }, 600);
  }

  function choisirFamille(id: number) {
    setFamilleId(id);
    setProduitRestaure(false);
    requestAnimationFrame(() => zoneProduits.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' }));
  }

  // ----- Finalisation -----
  const avecAlerteVic = (action: () => void) => (!estDevis && estVic ? setActionVic(() => action) : action());

  async function valider() {
    if (!dossier || !produit) return notifier('Choisissez d’abord un produit / configurateur');
    if (!champs.dateFinProduction) return notifier('Renseignez la date de fin de prod');
    setOccupe(true);
    try {
      const d = await api.validerDossier(dossier.id, saisieDto());
      notifier(
        `${estDevis ? 'Devis' : 'Commande'} ${d.reference} enregistré${estDevis ? '' : 'e'}${d.configId ? ` — configuration ${d.configId}` : ''}${d.numeroSap ? ` — n° SAP ${d.numeroSap}` : ''}`,
      );
      surValide();
    } catch (e) {
      setErreur((e as Error).message);
    } finally {
      setOccupe(false);
    }
  }

  function modifierDansSap() {
    notifier('Ouverture de la modification SAP simulée');
    window.setTimeout(() => window.alert(`Prototype : ici, ${estDevis ? 'le devis' : 'la commande'} sera ouvert${estDevis ? '' : 'e'} dans SAP pour modification.`), 150);
  }

  async function fermer() {
    window.clearInterval(surveillance.current);
    // Les saisies d'un dossier « En cours » sont conservées.
    if (dossier && prerequisOk) {
      try {
        await api.enregistrerDossier(dossier.id, saisieDto());
        modifie.current = true;
      } catch {
        // Saisie incomplète : le dossier garde son dernier état enregistré.
      }
    }
    surFermer(modifie.current);
  }

  // ----- Affichage -----
  const integreWeb = dossier?.origine === 'SiteInternet';
  const titre = dossier
    ? integreWeb
      ? `Validation de la commande intégrée ${dossier.reference}`
      : `${estDevis ? 'Modification du devis' : 'Modification de la commande'} ${dossier.reference}`
    : estDevis
      ? "Création d'un devis"
      : "Création d'une commande";
  const sousTitre = dossier
    ? integreWeb
      ? 'Commande intégrée automatiquement depuis le site internet. Vérifiez les données puis validez la commande.'
      : 'Tous les champs, choix et données enregistrés sont réaffichés dans leur dernier état.'
    : `Renseignez le client, la référence chantier, le ticket Salesforce et les informations de livraison / retrait, puis choisissez le produit à ${estDevis ? 'chiffrer' : 'commander'}.`;

  const famille = catalogue?.find((f) => f.id === familleId) ?? null;
  const afficherProduits = prerequisOk && famille !== null;

  return (
    <>
      <Modale
        id="orderModal"
        ouverte
        titre={titre}
        sousTitre={sousTitre}
        largeur={1120}
        classeBoite={`${estVic ? 'vic-client' : ''} ${occupe ? 'busy' : ''}`}
        surFermer={fermer}
        enTete={
          <div className="vic-logo" aria-label="Client VIC">
            <span className="vic-badge">VIC</span>
            <span className="vic-text">
              <strong>VIC</strong>
              <small>Paiement à la commande</small>
            </span>
          </div>
        }
      >
        {chargement ? (
          <p className="muted" style={{ textAlign: 'center' }}>
            Chargement du dossier…
          </p>
        ) : (
          <div className="form">
            <label>Recherche, choisir un client</label>
            <div className="client-search">
              <input
                list="clientOptions"
                placeholder="Rechercher par code client ou raison sociale..."
                value={rechercheClient}
                onChange={(e) => void choisirClient(e.target.value)}
              />
              <datalist id="clientOptions">
                {clients.flatMap((c) => [
                  <option key={`${c.code}-c`} value={c.code}>{`${c.raisonSociale} — ${c.code}`}</option>,
                  <option key={`${c.code}-n`} value={c.raisonSociale}>{`${c.raisonSociale} — ${c.code}`}</option>,
                ])}
              </datalist>
            </div>

            <div className="form-grid-2">
              <div>
                <label>Référence chantier</label>
                <input type="text" placeholder="Saisir la référence chantier" value={champs.referenceChantier} onChange={(e) => maj({ referenceChantier: e.target.value })} />
              </div>
              <div>
                <label>Numéro de ticket Salesforce</label>
                <input type="text" placeholder="Ex. 01234567" value={champs.ticketSalesforce} onChange={(e) => maj({ ticketSalesforce: e.target.value })} />
              </div>
            </div>

            <div className="delivery-box">
              <h3>Livraison / Retrait en agence</h3>
              <div className="choice-row" style={{ marginBottom: 14 }}>
                {modes.map((m) => (
                  <label key={m} className="choice">
                    <input type="radio" name="modeLivraison" checked={champs.modeLivraison === m} onChange={() => changerMode(m)} /> {m}
                  </label>
                ))}
              </div>

              {champs.modeLivraison === 'Retrait en agence' && (
                <div style={{ borderTop: '1px solid var(--line)', paddingTop: 12, marginBottom: 14 }}>
                  <label>Choix de l'agence de retrait</label>
                  <div className="choice-row agency-row">
                    {agences.map((a) => (
                      <label key={a.code} className="choice">
                        <input type="radio" name="agence" checked={champs.agenceRetrait === a.libelle} onChange={() => maj({ agenceRetrait: a.libelle })} /> {a.libelle}
                      </label>
                    ))}
                  </div>
                </div>
              )}

              {(champs.modeLivraison === 'Chez le client' || champs.modeLivraison === 'A saisir') && (
                <div>
                  <h3 style={{ marginTop: 4 }}>Adresse de livraison</h3>
                  {champs.modeLivraison === 'Chez le client' && (
                    <div>
                      <div className="form-grid-2">
                        <div>
                          <label>Client de l'adresse</label>
                          <input
                            list="addressClientOptions"
                            placeholder="Client sélectionné ou rechercher un autre client..."
                            value={rechercheClientAdresse}
                            onChange={(e) => void choisirClientAdresse(e.target.value)}
                          />
                          <datalist id="addressClientOptions">
                            {clients.map((c) => (
                              <option key={c.code} value={`${c.code} — ${c.raisonSociale}`}>{`${c.raisonSociale} — ${c.code}`}</option>
                            ))}
                          </datalist>
                        </div>
                        <div>
                          <label>Adresse enregistrée</label>
                          <select value={adresseId} onChange={(e) => appliquerClientAdresse(clientAdresse, false, e.target.value)}>
                            {!clientAdresse?.adresses.length && <option value="">Aucune adresse enregistrée</option>}
                            {clientAdresse?.adresses.map((a) => (
                              <option key={a.id} value={a.id}>
                                {a.libelle}
                              </option>
                            ))}
                          </select>
                        </div>
                      </div>
                      <p className="muted" style={{ marginTop: -4 }}>
                        Par défaut, l'adresse principale du client sélectionné est utilisée. Vous pouvez choisir une adresse secondaire ou l'adresse d'un autre client.
                      </p>
                    </div>
                  )}
                  <div className="form-grid-2">
                    <div>
                      <label>Nom</label>
                      <input type="text" placeholder="Nom du destinataire" value={champs.livraisonNom} onChange={(e) => maj({ livraisonNom: e.target.value })} />
                    </div>
                    <div>
                      <label>Tél. (personne à contacter)</label>
                      <input type="tel" placeholder="Téléphone du contact" value={champs.livraisonTelephone} onChange={(e) => maj({ livraisonTelephone: e.target.value })} />
                    </div>
                  </div>
                  <label>Adresse</label>
                  <input type="text" placeholder="Adresse de livraison" value={champs.livraisonAdresse} onChange={(e) => maj({ livraisonAdresse: e.target.value })} />
                  <div className="form-grid-2">
                    <div>
                      <label>Code postal</label>
                      <input type="text" inputMode="numeric" placeholder="Code postal" value={champs.livraisonCodePostal} onChange={(e) => maj({ livraisonCodePostal: e.target.value })} />
                    </div>
                    <div>
                      <label>Ville</label>
                      <input type="text" placeholder="Ville" value={champs.livraisonVille} onChange={(e) => maj({ livraisonVille: e.target.value })} />
                    </div>
                  </div>
                  <label>Accès</label>
                  <div className="choice-row">
                    {typesAcces.map((a) => (
                      <label key={a} className="choice">
                        <input
                          type="checkbox"
                          checked={champs.acces.includes(a)}
                          onChange={(e) => maj({ acces: e.target.checked ? [...champs.acces, a] : champs.acces.filter((x) => x !== a) })}
                        />{' '}
                        {a}
                      </label>
                    ))}
                  </div>
                  <input type="text" placeholder="Préciser l'autre type d'accès" value={champs.accesAutre} onChange={(e) => maj({ accesAutre: e.target.value })} />
                  <p className="muted delivery-note">Renseigner les informations nécessaires à l’accessibilité pour l’affrètement.</p>
                </div>
              )}
            </div>

            {!prerequisOk && (
              <div className="info-ligne" style={{ marginTop: 18 }}>
                Pour afficher les produits disponibles, complétez : <b>{manquants.join(', ')}</b>.
              </div>
            )}

            {prerequisOk && (
              <div id="familyArea">
                <h3 style={{ marginTop: 20 }}>Choisir un produit</h3>
                <p className="muted">Seules les familles et les produits autorisés pour ce client sont affichés.</p>
                <div className="family-grid">
                  {catalogue?.length === 0 && <div className="muted">Aucun produit n’est autorisé pour ce client.</div>}
                  {catalogue?.map((f) => (
                    <button key={f.id} type="button" className={`family ${familleId === f.id ? 'active' : ''}`} onClick={() => choisirFamille(f.id)}>
                      <div className="family-icon" style={{ backgroundImage: urlCss(imageProduit(f.produits[0]?.nom ?? f.nom, f.nom, f.image ?? f.produits[0]?.image)) }} />
                      <b>{f.nom}</b>
                      <div className="count">{f.produits.length} produit(s) disponible(s)</div>
                    </button>
                  ))}
                </div>

                {afficherProduits && (
                  <div id="productArea" ref={zoneProduits}>
                    <h3 style={{ marginTop: 20 }}>Choisir le produit</h3>
                    <div className="product-grid">
                      {produitRestaure && produit ? (
                        <article className="product" style={{ borderColor: '#1479f8', boxShadow: '0 0 0 2px rgba(20,121,248,.12)' }}>
                          <div className="thumb" style={{ backgroundImage: urlCss(imageProduit(produit.nom, famille.nom, produit.image)) }} />
                          <h4>{produit.nom}</h4>
                          <div className="config-name">✓ Produit déjà sélectionné · {produit.configurateur || 'Configurateur'}</div>
                          <p>{produit.description ?? ''}</p>
                          <button className="secondary" onClick={() => void lancerConfigurateur(produit, false)}>
                            Modifier la configuration
                          </button>
                        </article>
                      ) : (
                        famille.produits.map((p) => (
                          <article key={p.id} className="product">
                            <div className="thumb" style={{ backgroundImage: urlCss(imageProduit(p.nom, famille.nom, p.image)) }} />
                            <h4>{p.nom}</h4>
                            <div className="config-name" title={p.configurateur}>
                              ⚙ {p.configurateur}
                            </div>
                            <p>{p.description ?? ''}</p>
                            <button className="primary" disabled={occupe} onClick={() => void lancerConfigurateur(p, p.id !== dossier?.produitId)}>
                              Choisir →
                            </button>
                          </article>
                        ))
                      )}
                      {!produitRestaure && famille.produits.length === 0 && (
                        <div className="muted" style={{ gridColumn: '1/-1' }}>
                          Aucun produit disponible dans cette famille pour ce client.
                        </div>
                      )}
                    </div>
                  </div>
                )}
              </div>
            )}

            {erreur && <div className="erreur-ligne">{erreur}</div>}
            {occupe && <div className="info-ligne">Communication avec le Middleware CPQ en cours…</div>}

            {finalisation && produit && dossier && (
              <div className="delivery-box" ref={zoneFinalisation} style={{ marginTop: 22 }}>
                <h3 style={{ marginTop: 0, textAlign: 'center' }}>{estDevis ? 'Finaliser mon devis' : 'Finaliser ma commande'}</h3>
                <p className="muted" style={{ textAlign: 'center' }}>
                  Produit : {produit.nom} · Configurateur : {produit.configurateur || 'Non défini'}
                  {dossier.configId && (
                    <>
                      {' '}
                      · Configuration <span className="config-id">{dossier.configId}</span>
                      {dossier.environnement === 'production' ? ' (production)' : ' (test)'}
                    </>
                  )}
                </p>
                <div className="finalize-order-fields">
                  <div className="finalize-order-field">
                    <label>Date de fin de prod</label>
                    {produit.id > 0 ? (
                      <PlanningProduction produitId={produit.id} valeur={champs.dateFinProduction} surChoix={(d) => maj({ dateFinProduction: d })} />
                    ) : (
                      <p className="muted">Produit absent du catalogue : planning indisponible.</p>
                    )}
                  </div>
                  {(['commentaireExterne', 'commentaireInterne'] as const).map((cle) => (
                    <div key={cle} className="finalize-order-field">
                      <div className="comment-field-head">
                        <label>{cle === 'commentaireExterne' ? 'Commentaire externe' : 'Commentaire interne'}</label>
                        <button type="button" className="secondary comment-template-btn" onClick={() => setCibleCommentaire(cle)}>
                          Choisir un message
                        </button>
                      </div>
                      <textarea
                        rows={3}
                        placeholder={`Saisir ou insérer un commentaire ${cle === 'commentaireExterne' ? 'externe' : 'interne'}`}
                        value={champs[cle]}
                        onChange={(e) => maj({ [cle]: e.target.value })}
                      />
                    </div>
                  ))}
                </div>
                <div style={{ display: 'flex', justifyContent: 'center', gap: 10, flexWrap: 'wrap', marginTop: 18 }}>
                  <button className="primary" disabled={occupe} onClick={() => avecAlerteVic(() => void valider())}>
                    {estDevis ? 'VALIDER DEVIS →' : 'VALIDER COMMANDE →'}
                  </button>
                  <button className="secondary" onClick={() => avecAlerteVic(modifierDansSap)}>
                    MODIFIER DANS SAP
                  </button>
                  <button className="secondary" onClick={() => void fermer()}>
                    FERMER
                  </button>
                </div>
              </div>
            )}
          </div>
        )}
      </Modale>

      {cibleCommentaire && (
        <SelecteurCommentaire
          cible={cibleCommentaire === 'commentaireExterne' ? 'Commentaire externe' : 'Commentaire interne'}
          surFermer={() => setCibleCommentaire(null)}
          surChoix={(texte) => {
            const actuel = champs[cibleCommentaire].trim();
            maj({ [cibleCommentaire]: actuel ? `${actuel}\n${texte}` : texte });
            setCibleCommentaire(null);
            notifier('Message inséré — le texte reste modifiable');
          }}
        />
      )}

      {actionVic && (
        <AlerteVic
          surContinuer={() => {
            const action = actionVic;
            setActionVic(null);
            action();
          }}
          surFermer={() => {
            setActionVic(null);
            notifier('Action annulée — la commande reste identique');
          }}
        />
      )}
    </>
  );
}
