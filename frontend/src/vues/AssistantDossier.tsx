import { useEffect, useMemo, useRef, useState } from 'react';
import {
  api,
  type Agence,
  type Client,
  type ClientDetail,
  type Dossier,
  type FamilleCatalogue,
  type IssueConfiguration,
  type ModeLivraison,
  type ProduitCatalogue,
  type SaisieDossier,
  type TypeDossier,
} from '../api';
import { afficherAttente } from '../composants/Chargement';
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

/**
 * Retrouve un client à partir du texte saisi : code, raison sociale ou « code — raison sociale » exacts, ou texte commençant par le code.
 * Pas de correspondance partielle sur la raison sociale : avec des milliers de clients ERP, « ALEX PISCINES » ne doit pas désigner « ALEX ».
 */
function trouverClient(clients: Client[], saisie: string) {
  const q = saisie.trim().toLowerCase();
  if (!q) return undefined;
  return (
    clients.find((c) => [c.code, c.raisonSociale, `${c.code} — ${c.raisonSociale}`, `${c.raisonSociale} — ${c.code}`].some((v) => v.toLowerCase() === q)) ??
    clients.find((c) => q.startsWith(`${c.code.toLowerCase()} `))
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
  const { notifier, t } = app;

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
  const derniereRecherche = useRef(0);
  const zoneProduits = useRef<HTMLDivElement>(null);
  const zoneFinalisation = useRef<HTMLDivElement>(null);

  const estDevis = type === 'Devis';
  // Commande issue d'un devis dont la configuration n'a pas encore été rouverte et validée en commande.
  const aTransformer = !!dossier?.devisSourceReference && !dossier.configurationEnregistree;
  const estVic = !!client?.client.estVic;
  const maj = (partiel: Partial<Champs>) => setChamps((c) => ({ ...c, ...partiel }));

  // Chargement initial : clients et agences (SAP), puis le dossier en cas de réouverture.
  useEffect(() => {
    api.clients().then(setClients).catch((e: Error) => setErreur(e.message));
    api.agences().then(setAgences).catch(() => undefined);
    if ('dossierId' in ouverture) {
      void restaurer(ouverture.dossierId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function restaurer(id: number) {
    try {
      let d = await api.dossier(id);
      // Portail fermé pendant la configuration : la fin de configuration est vérifiée à la réouverture.
      // (Une commande issue d'un devis pas encore rouverte en commande est une transformation à poursuivre, pas un abandon.)
      if (d.statut === 'EnCours' && d.configId && !d.configurationEnregistree && !d.devisSourceReference) {
        const fin = await api.finConfiguration(id);
        if (fin.issue === 'Abandonnee') {
          window.alert(t('Configuration non terminée : {reference} est abandonné.', { reference: d.reference }));
          surFermer(true);
          return;
        }
        d = fin.dossier;
      }
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
        setProduit(p ?? { id: d.produitId ?? 0, nom: d.nomProduit, description: t('Dernier choix enregistré dans le dossier.'), image: null, configurateur: d.configurateur ?? '' });
        setProduitRestaure(true);
        // Commande issue d'un devis : la finalisation attend que la configuration ait été rouverte et validée en commande.
        setFinalisation(!d.devisSourceReference || d.configurationEnregistree);
      }
      notifier(t('{reference} rouvert dans son dernier état', { reference: d.reference }));
    } catch (e) {
      setErreur((e as Error).message);
    } finally {
      setChargement(false);
    }
  }

  // ----- Client -----
  /**
   * Plusieurs milliers de clients ERP : la recherche se fait côté serveur à chaque saisie, ce qui rafraîchit les suggestions,
   * puis le client est reconnu parmi elles. Seule la réponse à la dernière saisie est prise en compte.
   */
  async function rechercherClient(saisie: string): Promise<Client | undefined> {
    const connu = trouverClient(clients, saisie);
    if (connu) return connu;
    const terme = saisie.split('—')[0].trim();
    if (terme.length < 2) return undefined;
    const numero = ++derniereRecherche.current;
    const resultats = await api.clients(terme);
    if (numero !== derniereRecherche.current) return undefined;
    setClients(resultats);
    return trouverClient(resultats, saisie);
  }

  async function choisirClient(saisie: string) {
    setRechercheClient(saisie);
    let c: Client | undefined;
    try {
      c = await rechercherClient(saisie);
    } catch (e) {
      setErreur((e as Error).message);
      return;
    }
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
      if (detail.client.estVic) notifier(t('Client en mode de paiement VIC'));
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
        // Le dossier n'a qu'un champ d'adresse : le complément ERP y est ajouté.
        livraisonAdresse: [adresse.adresse, adresse.complementAdresse].filter(Boolean).join(', '),
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
    try {
      const c = await rechercherClient(saisie);
      if (c && c.code !== clientAdresse?.client.code) {
        appliquerClientAdresse(await api.client(c.code), true);
      }
    } catch (e) {
      setErreur((e as Error).message);
    }
  }

  /** Informations obligatoires encore manquantes avant de pouvoir choisir un produit. */
  const manquants = useMemo(() => {
    const liste: string[] = [];
    if (!client) liste.push(t('le client'));
    if (!champs.referenceChantier.trim()) liste.push(t('la référence chantier'));
    if (!champs.ticketSalesforce.trim()) liste.push(t('le numéro de ticket Salesforce'));
    if (champs.modeLivraison === 'Chez le client' || champs.modeLivraison === 'A saisir') {
      const adresse: [string, string][] = [
        [champs.livraisonNom, t('nom')],
        [champs.livraisonTelephone, t('téléphone')],
        [champs.livraisonAdresse, t('adresse')],
        [champs.livraisonCodePostal, t('code postal')],
        [champs.livraisonVille, t('ville')],
      ];
      const vides = adresse.filter(([v]) => !v.trim()).map(([, nom]) => nom);
      if (vides.length) liste.push(t("l'adresse de livraison ({champs})", { champs: vides.join(', ') }));
    } else if (champs.modeLivraison === 'Retrait en agence') {
      if (!champs.agenceRetrait) liste.push(t("l'agence de retrait"));
    } else {
      liste.push(t('le mode de livraison ou de retrait'));
    }
    return liste;
  }, [client, champs, t]);
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
      notifier(t('Autorisez les fenêtres popup pour ouvrir le configurateur.'));
      return;
    }
    afficherAttente(fenetre, t('Ouverture du configurateur {configurateur}', { configurateur: p.configurateur || p.nom }), t('Le Middleware CPQ prépare la session'));
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
        setErreur(
          lancement.succes
            ? t('Message du Middleware : {message}', { message: lancement.message ?? '' })
            : t('Le lancement a échoué : {message}', { message: lancement.message ?? t('réponse sans message') }),
        );
        if (!lancement.succes && !dossier) {
          notifier(t('Si le lancement est OK avec le profil « AX », vérifiez les droits dans « MyUsers ».'));
        }
        return;
      }
      modifie.current = true;
      const precedent = produit;
      setDossier(resultat.dossier);
      setProduit(p);
      setProduitRestaure(false);
      setFinalisation(false);
      fenetre.location.href = lancement.url;
      // Changement de produit sur un dossier qui a déjà une configuration : à confirmer à la fermeture du configurateur.
      const nouvelle =
        lancement.configId && dossier?.configId && lancement.configId !== dossier.configId
          ? { configId: lancement.configId, produitId: p.id, environnement: app.environnement }
          : undefined;
      void surveiller(fenetre, resultat.dossier.id, lancement.configId ?? p.nom, nouvelle, precedent);
    } catch (e) {
      fenetre.close();
      setErreur((e as Error).message);
    } finally {
      setOccupe(false);
    }
  }

  /**
   * Le portail est bloqué tant que le configurateur est ouvert. À sa fermeture, l'API vérifie auprès du Middleware que la
   * configuration a été terminée : sinon le dossier est abandonné (ou garde son produit précédent en cas de changement).
   */
  async function surveiller(
    fenetre: Window,
    idDossier: number,
    libelle: string,
    nouvelle: { configId: string; produitId: number; environnement: string } | undefined,
    precedent: ProduitCatalogue | null,
  ) {
    await app.suivreConfigurateur(fenetre, libelle);
    setOccupe(true);
    try {
      const { dossier: d, issue } = await api.finConfiguration(idDossier, nouvelle);
      setDossier(d);
      await traiterFin(d, issue, precedent);
    } catch (e) {
      setErreur((e as Error).message);
    } finally {
      setOccupe(false);
    }
  }

  async function traiterFin(d: Dossier, issue: IssueConfiguration, precedent: ProduitCatalogue | null) {
    if (issue === 'Abandonnee') {
      window.alert(t('Configuration non terminée : {reference} est abandonné.', { reference: d.reference }));
      modifie.current = true;
      surFermer(true);
      return;
    }
    if (issue === 'ProduitNonChange') {
      // La nouvelle configuration n'a pas été enregistrée : le dossier garde son produit et sa configuration.
      setProduit(precedent);
      setProduitRestaure(!!precedent);
      setFinalisation(d.configurationEnregistree);
      notifier(t('Nouvelle configuration non terminée : le dossier garde son produit précédent.'));
      return;
    }
    if (issue === 'Indeterminee') {
      notifier(t('Le Middleware n’a pas pu confirmer l’enregistrement de la configuration : vérifiez-la avant de finaliser.'));
    } else if (d.devisSourceReference && !d.configurationEnregistree) {
      return;
    } else {
      notifier(estDevis ? t('Configuration enregistrée. Vous pouvez finaliser le devis.') : t('Configuration enregistrée. Vous pouvez finaliser la commande.'));
    }
    setFinalisation(true);
    requestAnimationFrame(() => zoneFinalisation.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' }));
  }

  function choisirFamille(id: number) {
    setFamilleId(id);
    setProduitRestaure(false);
    requestAnimationFrame(() => zoneProduits.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' }));
  }

  // ----- Finalisation -----
  const avecAlerteVic = (action: () => void) => (!estDevis && estVic ? setActionVic(() => action) : action());

  async function valider() {
    if (!dossier || !produit) return notifier(t('Choisissez d’abord un produit / configurateur'));
    if (!champs.dateFinProduction) return notifier(t('Renseignez la date de fin de prod'));
    setOccupe(true);
    try {
      const d = await api.validerDossier(dossier.id, saisieDto());
      notifier(
        (estDevis ? t('Devis {reference} enregistré', { reference: d.reference }) : t('Commande {reference} enregistrée', { reference: d.reference })) +
          (d.configId ? t(' — configuration {configId}', { configId: d.configId }) : '') +
          (d.numeroSap ? t(' — n° SAP {numeroSap}', { numeroSap: d.numeroSap }) : '') +
          (d.fichierXml ? t(' — fichiers SAP dans {repertoire}', { repertoire: d.fichierXml }) : ''),
      );
      surValide();
    } catch (e) {
      setErreur((e as Error).message);
    } finally {
      setOccupe(false);
    }
  }

  async function fermer() {
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
      ? t('Validation de la commande intégrée {reference}', { reference: dossier.reference })
      : estDevis
        ? t('Modification du devis {reference}', { reference: dossier.reference })
        : t('Modification de la commande {reference}', { reference: dossier.reference })
    : estDevis
      ? t("Création d'un devis")
      : t("Création d'une commande");
  const sousTitre = dossier
    ? integreWeb
      ? t('Commande intégrée automatiquement depuis le site internet. Vérifiez les données puis validez la commande.')
      : t('Tous les champs, choix et données enregistrés sont réaffichés dans leur dernier état.')
    : estDevis
      ? t('Renseignez le client, la référence chantier, le ticket Salesforce et les informations de livraison / retrait, puis choisissez le produit à chiffrer.')
      : t('Renseignez le client, la référence chantier, le ticket Salesforce et les informations de livraison / retrait, puis choisissez le produit à commander.');

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
          <div className="vic-logo" aria-label={t('Client VIC')}>
            <span className="vic-badge">VIC</span>
            <span className="vic-text">
              <strong>VIC</strong>
              <small>{t('Paiement à la commande')}</small>
            </span>
          </div>
        }
      >
        {chargement ? (
          <p className="muted" style={{ textAlign: 'center' }}>
            {t('Chargement du dossier…')}
          </p>
        ) : (
          <div className="form">
            <label>{t('Recherche, choisir un client')}</label>
            <div className="client-search">
              <input
                list="clientOptions"
                placeholder={t('Rechercher par code client ou raison sociale...')}
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
                <label>{t('Référence chantier')}</label>
                <input type="text" placeholder={t('Saisir la référence chantier')} value={champs.referenceChantier} onChange={(e) => maj({ referenceChantier: e.target.value })} />
              </div>
              <div>
                <label>{t('Numéro de ticket Salesforce')}</label>
                <input type="text" placeholder={t('Ex. 01234567')} value={champs.ticketSalesforce} onChange={(e) => maj({ ticketSalesforce: e.target.value })} />
              </div>
            </div>

            <div className="delivery-box">
              <h3>{t('Livraison / Retrait en agence')}</h3>
              <div className="choice-row" style={{ marginBottom: 14 }}>
                {modes.map((m) => (
                  <label key={m} className="choice">
                    <input type="radio" name="modeLivraison" checked={champs.modeLivraison === m} onChange={() => changerMode(m)} /> {t(m)}
                  </label>
                ))}
              </div>

              {champs.modeLivraison === 'Retrait en agence' && (
                <div style={{ borderTop: '1px solid var(--line)', paddingTop: 12, marginBottom: 14 }}>
                  <label>{t("Choix de l'agence de retrait")}</label>
                  <div className="choice-row agency-row">
                    {agences.map((a) => (
                      <label key={a.code} className="choice" title={a.region ?? undefined}>
                        <input type="radio" name="agence" checked={champs.agenceRetrait === a.libelle} onChange={() => maj({ agenceRetrait: a.libelle })} /> {a.libelle}
                      </label>
                    ))}
                  </div>
                  {agences.length === 0 && <div className="info-ligne">{t("Aucune agence de retrait n'est encore transmise par l'ERP.")}</div>}
                </div>
              )}

              {(champs.modeLivraison === 'Chez le client' || champs.modeLivraison === 'A saisir') && (
                <div>
                  <h3 style={{ marginTop: 4 }}>{t('Adresse de livraison')}</h3>
                  {champs.modeLivraison === 'Chez le client' && (
                    <div>
                      <div className="form-grid-2">
                        <div>
                          <label>{t("Client de l'adresse")}</label>
                          <input
                            list="addressClientOptions"
                            placeholder={t('Client sélectionné ou rechercher un autre client...')}
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
                          <label>{t('Adresse enregistrée')}</label>
                          <select value={adresseId} onChange={(e) => appliquerClientAdresse(clientAdresse, false, e.target.value)}>
                            {!clientAdresse?.adresses.length && <option value="">{t('Aucune adresse enregistrée')}</option>}
                            {clientAdresse?.adresses.map((a) => (
                              <option key={a.id} value={a.id}>
                                {a.libelle}
                              </option>
                            ))}
                          </select>
                        </div>
                      </div>
                      <p className="muted" style={{ marginTop: -4 }}>
                        {t("Par défaut, l'adresse principale du client sélectionné est utilisée. Vous pouvez choisir une adresse secondaire ou l'adresse d'un autre client.")}
                      </p>
                    </div>
                  )}
                  <div className="form-grid-2">
                    <div>
                      <label>{t('Nom')}</label>
                      <input type="text" placeholder={t('Nom du destinataire')} value={champs.livraisonNom} onChange={(e) => maj({ livraisonNom: e.target.value })} />
                    </div>
                    <div>
                      <label>{t('Tél. (personne à contacter)')}</label>
                      <input type="tel" placeholder={t('Téléphone du contact')} value={champs.livraisonTelephone} onChange={(e) => maj({ livraisonTelephone: e.target.value })} />
                    </div>
                  </div>
                  <label>{t('Adresse')}</label>
                  <input type="text" placeholder={t('Adresse de livraison')} value={champs.livraisonAdresse} onChange={(e) => maj({ livraisonAdresse: e.target.value })} />
                  <div className="form-grid-2">
                    <div>
                      <label>{t('Code postal')}</label>
                      <input type="text" inputMode="numeric" placeholder={t('Code postal')} value={champs.livraisonCodePostal} onChange={(e) => maj({ livraisonCodePostal: e.target.value })} />
                    </div>
                    <div>
                      <label>{t('Ville')}</label>
                      <input type="text" placeholder={t('Ville')} value={champs.livraisonVille} onChange={(e) => maj({ livraisonVille: e.target.value })} />
                    </div>
                  </div>
                  <label>{t('Accès')}</label>
                  <div className="choice-row">
                    {typesAcces.map((a) => (
                      <label key={a} className="choice">
                        <input
                          type="checkbox"
                          checked={champs.acces.includes(a)}
                          onChange={(e) => maj({ acces: e.target.checked ? [...champs.acces, a] : champs.acces.filter((x) => x !== a) })}
                        />{' '}
                        {t(a)}
                      </label>
                    ))}
                  </div>
                  <input type="text" placeholder={t("Préciser l'autre type d'accès")} value={champs.accesAutre} onChange={(e) => maj({ accesAutre: e.target.value })} />
                  <p className="muted delivery-note">{t('Renseigner les informations nécessaires à l’accessibilité pour l’affrètement.')}</p>
                </div>
              )}
            </div>

            {!prerequisOk && (
              <div className="info-ligne" style={{ marginTop: 18 }}>
                {t('Pour afficher les produits disponibles, complétez :')} <b>{manquants.join(', ')}</b>.
              </div>
            )}

            {prerequisOk && (
              <div id="familyArea">
                <h3 style={{ marginTop: 20 }}>{t('Choisir un produit')}</h3>
                <p className="muted">{t('Seules les familles et les produits autorisés pour ce client sont affichés.')}</p>
                <div className="family-grid">
                  {catalogue?.length === 0 && <div className="muted">{t('Aucun produit n’est autorisé pour ce client.')}</div>}
                  {catalogue?.map((f) => (
                    <button key={f.id} type="button" className={`family ${familleId === f.id ? 'active' : ''}`} onClick={() => choisirFamille(f.id)}>
                      <div className="family-icon" style={{ backgroundImage: urlCss(imageProduit(f.produits[0]?.nom ?? f.nom, f.nom, f.image ?? f.produits[0]?.image)) }} />
                      <b>{f.nom}</b>
                      <div className="count">{t('{nombre} produit(s) disponible(s)', { nombre: f.produits.length })}</div>
                    </button>
                  ))}
                </div>

                {afficherProduits && (
                  <div id="productArea" ref={zoneProduits}>
                    <h3 style={{ marginTop: 20 }}>{t('Choisir le produit')}</h3>
                    <div className="product-grid">
                      {produitRestaure && produit ? (
                        <article className="product" style={{ borderColor: '#1479f8', boxShadow: '0 0 0 2px rgba(20,121,248,.12)' }}>
                          <div className="thumb" style={{ backgroundImage: urlCss(imageProduit(produit.nom, famille.nom, produit.image)) }} />
                          <h4>{produit.nom}</h4>
                          <div className="config-name">{t('✓ Produit déjà sélectionné · {configurateur}', { configurateur: produit.configurateur || t('Configurateur') })}</div>
                          <p>{produit.description ?? ''}</p>
                          {aTransformer ? (
                            <button className="primary" disabled={occupe} onClick={() => void lancerConfigurateur(produit, false)}>
                              {t('Ouvrir la configuration en commande')}
                            </button>
                          ) : (
                            <button className="secondary" onClick={() => void lancerConfigurateur(produit, false)}>
                              {t('Modifier la configuration')}
                            </button>
                          )}
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
                              {t('Choisir →')}
                            </button>
                          </article>
                        ))
                      )}
                      {!produitRestaure && famille.produits.length === 0 && (
                        <div className="muted" style={{ gridColumn: '1/-1' }}>
                          {t('Aucun produit disponible dans cette famille pour ce client.')}
                        </div>
                      )}
                    </div>
                  </div>
                )}
              </div>
            )}

            {aTransformer && (
              <div className="info-ligne">
                {t('Commande issue du devis {devis} : ouvrez la configuration en commande, validez-la dans le configurateur, puis finalisez la commande.', {
                  devis: dossier?.devisSourceReference ?? '',
                })}
              </div>
            )}
            {erreur && <div className="erreur-ligne">{erreur}</div>}
            {occupe && <div className="info-ligne">{t('Communication avec le Middleware CPQ en cours…')}</div>}

            {finalisation && produit && dossier && (
              <div className="delivery-box" ref={zoneFinalisation} style={{ marginTop: 22 }}>
                <h3 style={{ marginTop: 0, textAlign: 'center' }}>{estDevis ? t('Finaliser mon devis') : t('Finaliser ma commande')}</h3>
                <p className="muted" style={{ textAlign: 'center' }}>
                  {t('Produit : {produit} · Configurateur : {configurateur}', { produit: produit.nom, configurateur: produit.configurateur || t('Non défini') })}
                  {dossier.configId && (
                    <>
                      {' '}
                      {t('· Configuration')} <span className="config-id">{dossier.configId}</span>
                      {` (${dossier.environnement ?? 'test'})`}
                    </>
                  )}
                </p>
                <div className="finalize-order-fields">
                  <div className="finalize-order-field">
                    <label>{t('Date de fin de prod')}</label>
                    <PlanningProduction dossierId={dossier.id} valeur={champs.dateFinProduction} surChoix={(d) => maj({ dateFinProduction: d })} />
                  </div>
                  {(['commentaireExterne', 'commentaireInterne'] as const).map((cle) => (
                    <div key={cle} className="finalize-order-field">
                      <div className="comment-field-head">
                        <label>{cle === 'commentaireExterne' ? t('Commentaire externe') : t('Commentaire interne')}</label>
                        <button type="button" className="secondary comment-template-btn" onClick={() => setCibleCommentaire(cle)}>
                          {t('Choisir un message')}
                        </button>
                      </div>
                      <textarea
                        rows={3}
                        placeholder={cle === 'commentaireExterne' ? t('Saisir ou insérer un commentaire externe') : t('Saisir ou insérer un commentaire interne')}
                        value={champs[cle]}
                        onChange={(e) => maj({ [cle]: e.target.value })}
                      />
                    </div>
                  ))}
                </div>
                <div style={{ display: 'flex', justifyContent: 'center', gap: 10, flexWrap: 'wrap', marginTop: 18 }}>
                  <button className="primary" disabled={occupe} onClick={() => avecAlerteVic(() => void valider())}>
                    {estDevis ? t('VALIDER DEVIS →') : t('VALIDER COMMANDE →')}
                  </button>
                  <button className="secondary" onClick={() => void fermer()}>
                    {t('FERMER')}
                  </button>
                </div>
              </div>
            )}
          </div>
        )}
      </Modale>

      {cibleCommentaire && (
        <SelecteurCommentaire
          cible={cibleCommentaire === 'commentaireExterne' ? t('Commentaire externe') : t('Commentaire interne')}
          surFermer={() => setCibleCommentaire(null)}
          surChoix={(texte) => {
            const actuel = champs[cibleCommentaire].trim();
            maj({ [cibleCommentaire]: actuel ? `${actuel}\n${texte}` : texte });
            setCibleCommentaire(null);
            notifier(t('Message inséré — le texte reste modifiable'));
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
            notifier(t('Action annulée — la commande reste identique'));
          }}
        />
      )}
    </>
  );
}
