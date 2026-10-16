// Accès à l'API du backend (CfSaisie.Api). Le front n'appelle jamais directement le Middleware CPQ ni SAP.

export type TypeDossier = 'Commande' | 'Devis';
export type StatutDossier = 'EnCours' | 'Envoye' | 'EnAttente' | 'AValider' | 'Validee' | 'Erreur' | 'Abandonne';
export type OrigineDossier = 'CreationInterne' | 'SiteInternet';
export type ModeLivraison = 'Chez le client' | 'A saisir' | 'Retrait en agence';

export const libellesStatut: Record<StatutDossier, string> = {
  EnCours: 'En cours',
  Envoye: 'Envoyé',
  EnAttente: 'En attente',
  AValider: 'À valider',
  Validee: 'Validée',
  Erreur: 'Erreur',
  Abandonne: 'Abandonné',
};

export const libellesOrigine: Record<OrigineDossier, string> = {
  CreationInterne: 'Création interne',
  SiteInternet: 'Site internet',
};

/** Données de erp.Commandes (flux ERP), quand la commande y figure. */
export interface CommandeErp {
  numeroCommande: string;
  statutErp: string;
  debutProduction: string | null;
  finProduction: string | null;
  livraisonPrevue: string | null;
  livraisonReelle: string | null;
}

/** Ligne du suivi des commandes et devis. */
/** Résultat de la vérification faite à la fermeture du configurateur. */
export type IssueConfiguration = 'Enregistree' | 'Abandonnee' | 'ProduitNonChange' | 'Inchangee' | 'Indeterminee';

export interface FinConfiguration {
  dossier: Dossier;
  issue: IssueConfiguration;
}

export interface DossierSuivi {
  id: number;
  reference: string;
  type: TypeDossier;
  origine: OrigineDossier;
  statut: StatutDossier;
  dateCreation: string;
  creePar: string;
  codeClient: string;
  nomClient: string;
  referenceClient: string | null;
  numeroSap: string | null;
  nomProduit: string;
  nomFamille: string;
  configId: string | null;
  /** Date de fin de production choisie dans le portail. */
  dateFinProductionSaisie: string | null;
  erp: CommandeErp | null;
  /** Commande : devis dont elle est issue. */
  devisSourceReference: string | null;
  /** Devis : commande validée qui en est issue. */
  commandeIssueReference: string | null;
  /** Devis : commande en cours de transformation. */
  commandeEnCoursReference: string | null;
}

export interface Environnement {
  code: string;
  libelle: string;
}

/** Personne connectée (Microsoft Entra ID) ; son rôle vient de ses groupes. */
export interface Session {
  utilisateur: string;
  initiales: string;
  compte: string | null;
  role: 'Utilisateur' | 'Administrateur' | 'SuperAdministrateur';
  societe: string;
  environnements: Environnement[];
  environnementParDefaut: string;
  /** Langues actives (table Langues) : proposées dans le sélecteur et envoyées au configurateur. */
  langues: Langue[];
  langueParDefaut: string;
}

export interface Langue {
  code: string;
  libelle: string;
}

export interface Client {
  code: string;
  raisonSociale: string;
  pays: string;
  /** Code langue ERP (FR, EN…) ; null si non renseigné. */
  langue: string | null;
  estVic: boolean;
}

export interface AdresseLivraison {
  id: string;
  libelle: string;
  estPrincipale: boolean;
  nom: string;
  telephone: string;
  adresse: string;
  complementAdresse: string | null;
  codePostal: string;
  ville: string;
  pays: string;
}

export interface ClientDetail {
  client: Client;
  adresses: AdresseLivraison[];
}

export interface Agence {
  code: string;
  libelle: string;
  region: string | null;
}

export interface ProduitCatalogue {
  id: number;
  nom: string;
  description: string | null;
  image: string | null;
  configurateur: string;
}

export interface FamilleCatalogue {
  id: number;
  nom: string;
  image: string | null;
  produits: ProduitCatalogue[];
}

/** Calendrier : le XML de fin de configuration contient un code CCX ; Libre : date saisie librement. */
export type ModePlanning = 'Calendrier' | 'Libre' | 'ConfigurationNonEnregistree';

export interface JourPlanning {
  /** Date de fin de production, AAAA-MM-JJ (sans fuseau). */
  date: string;
  etat: 'Disponible' | 'Limite' | 'Complet' | 'AvantDelai';
  libelle: string;
  selectionnable: boolean;
  /** Date du jour : à confirmer explicitement (comportement de l'écran AX). */
  confirmationRequise: boolean;
  placesRestantes: number | null;
  motif: string | null;
}

export interface PlanningDossier {
  mode: ModePlanning;
  message: string | null;
  codeCcx: string | null;
  libelleCcx: string | null;
  site: string | null;
  delaiMiniJoursOuvres: number | null;
  premiereDateAutorisee: string | null;
  /** Charge AX seulement : les fabrications déjà passées dans SAP ne sont pas encore comptées. */
  chargeIncomplete: boolean;
  origines: string[];
  jours: JourPlanning[];
}

export interface Lancement {
  succes: boolean;
  peutOuvrir: boolean;
  message: string | null;
  url: string | null;
  configId: string | null;
}

export interface SaisieDossier {
  type: TypeDossier;
  codeClient: string | null;
  referenceChantier: string | null;
  ticketSalesforce: string | null;
  modeLivraison: ModeLivraison | null;
  livraisonCodeClient: string | null;
  livraisonNomClient: string | null;
  livraisonLibelleAdresse: string | null;
  livraisonNom: string | null;
  livraisonTelephone: string | null;
  livraisonAdresse: string | null;
  livraisonCodePostal: string | null;
  livraisonVille: string | null;
  acces: string[];
  accesAutre: string | null;
  agenceRetrait: string | null;
  dateFinProduction: string | null;
  commentaireExterne: string | null;
  commentaireInterne: string | null;
}

export interface Dossier extends SaisieDossier {
  /** Répertoire d'export créé à la validation ({ConfigId}, avec {ConfigId}_portailSAP.xml et {ConfigId}.xml). */
  fichierXml?: string | null;
  id: number;
  reference: string;
  origine: OrigineDossier;
  statut: StatutDossier;
  codeClient: string;
  nomClient: string;
  produitId: number | null;
  nomProduit: string;
  nomFamille: string;
  configurateur: string | null;
  creePar: string;
  dateCreation: string;
  dateModification: string;
  configId: string | null;
  environnement: string | null;
  configurationEnregistree: boolean;
  numeroSap: string | null;
  devisSourceReference: string | null;
  commandeIssueReference: string | null;
  referenceCommandeClient: string | null;
  dateCommande: string | null;
  dateLivraisonSouhaitee: string | null;
}

export interface DossierLance {
  dossier: Dossier;
  lancement: Lancement;
}

export interface CommentaireModele {
  id: number;
  familleId: number;
  ordre: number;
  texte: string;
}

export interface CommentaireFamille {
  id: number;
  nom: string;
  ordre: number;
  modeles: CommentaireModele[];
}

export interface Message {
  id: number;
  audience: string;
  titre: string;
  corps: string;
  dateCreation: string;
}

export class ErreurApi extends Error {
  constructor(message: string, public readonly statut: number) {
    super(message);
  }
}

/**
 * Session absente ou expirée : redirection vers la connexion Microsoft, puis retour sur l'écran en cours.
 * Après un refus (aucun groupe du portail), on ne relance pas la connexion pour éviter une boucle.
 */
export function seConnecter(autreCompte = false) {
  const retour = encodeURIComponent(window.location.pathname + window.location.hash);
  window.location.href = `/api/connexion?retour=${retour}${autreCompte ? '&compte=autre' : ''}`;
}

export const connexionRefusee = () => new URLSearchParams(window.location.search).get('acces');

// Langue de l'interface, transmise à chaque appel : l'API la vérifie et l'envoie au configurateur.
let langueCourante = 'fr';
export function definirLangueApi(langue: string) {
  langueCourante = langue;
}

async function appeler<T>(methode: string, chemin: string, corps?: unknown): Promise<T> {
  const reponse = await fetch(`/api${chemin}`, {
    method: methode,
    headers: {
      Accept: 'application/json',
      'Accept-Language': langueCourante,
      ...(corps !== undefined ? { 'Content-Type': 'application/json' } : {}),
    },
    body: corps !== undefined ? JSON.stringify(corps) : undefined,
  });
  if (reponse.status === 401 && !connexionRefusee()) {
    seConnecter();
    // La page quitte le portail : on laisse l'appel en attente plutôt que d'afficher une erreur.
    return new Promise<T>(() => undefined);
  }
  if (!reponse.ok) {
    let message = `Erreur ${reponse.status}`;
    try {
      const probleme = await reponse.json();
      message = probleme.detail || probleme.title || message;
    } catch {
      // Réponse sans corps JSON : on garde le message générique.
    }
    throw new ErreurApi(message, reponse.status);
  }
  if (reponse.status === 204) return undefined as T;
  return (await reponse.json()) as T;
}

const qs = (parametres: Record<string, string | undefined>) => {
  const p = new URLSearchParams();
  Object.entries(parametres).forEach(([k, v]) => v && p.set(k, v));
  const s = p.toString();
  return s ? `?${s}` : '';
};

export const api = {
  session: () => appeler<Session>('GET', '/session'),
  messagesTableauBord: (client?: string) => appeler<Message[]>('GET', `/messages/tableau-bord${qs({ client })}`),
  clients: (q?: string) => appeler<Client[]>('GET', `/clients${qs({ q })}`),
  client: (code: string) => appeler<ClientDetail>('GET', `/clients/${encodeURIComponent(code)}`),
  catalogue: (code: string) => appeler<FamilleCatalogue[]>('GET', `/clients/${encodeURIComponent(code)}/catalogue`),
  agences: () => appeler<Agence[]>('GET', '/agences'),
  commentaires: () => appeler<CommentaireFamille[]>('GET', '/commentaires'),

  dossier: (id: number) => appeler<Dossier>('GET', `/dossiers/${id}`),
  creerDossier: (saisie: SaisieDossier, produitId: number, environnement: string) =>
    appeler<DossierLance>('POST', '/dossiers', { saisie, produitId, environnement }),
  enregistrerDossier: (id: number, saisie: SaisieDossier) => appeler<Dossier>('PUT', `/dossiers/${id}`, saisie),
  relancerConfigurateur: (id: number, produitId: number | null, environnement: string) =>
    appeler<DossierLance>('POST', `/dossiers/${id}/configurateur`, { produitId, environnement }),
  finConfiguration: (id: number, nouvelle?: { configId: string; produitId: number; environnement: string }) =>
    appeler<FinConfiguration>('POST', `/dossiers/${id}/fin-configuration`, nouvelle ?? {}),
  planningDossier: (id: number) => appeler<PlanningDossier>('GET', `/dossiers/${id}/planning`),
  suivi: () => appeler<DossierSuivi[]>('GET', '/dossiers'),
  transformerEnCommande: (id: number) => appeler<Dossier>('POST', `/dossiers/${id}/transformer-en-commande`),
  consulterDossier: (id: number) => appeler<DossierLance>('POST', `/dossiers/${id}/consulter`),
  validerDossier: (id: number, saisie: SaisieDossier) => appeler<Dossier>('POST', `/dossiers/${id}/valider`, saisie),
};

// ----- Administration (rôle Administrateur) -----

export interface ConfigurateurAdmin {
  id: number;
  nom: string;
  productionSite: string | null;
  actif: boolean;
  nombreProduits: number;
}

export interface ConfigurateurSaisie {
  nom: string;
  productionSite: string | null;
  actif: boolean;
}

export interface FamilleAdmin {
  id: number;
  nom: string;
  image: string | null;
  actif: boolean;
  nombreProduits: number;
}

export interface FamilleSaisie {
  nom: string;
  image: string | null;
  actif: boolean;
}

export interface ProduitAdmin {
  id: number;
  nom: string;
  description: string | null;
  image: string | null;
  lienConfigurateur: string | null;
  configurateurId: number;
  configurateur: string;
  familleId: number | null;
  famille: string | null;
}

export interface ProduitSaisie {
  nom: string;
  configurateurId: number;
  lienConfigurateur: string | null;
  description: string | null;
  image: string | null;
}

export interface AccreditationClient {
  codeClient: string;
  raisonSociale: string;
  produits: number[];
}

/** Ligne du journal des connexions. Résultat : Succes, RefuseSansGroupe ou Echec. */
export interface Connexion {
  id: number;
  date: string;
  resultat: 'Succes' | 'RefuseSansGroupe' | 'Echec';
  compte: string | null;
  nom: string | null;
  role: string | null;
  message: string | null;
  adresseIp: string | null;
  navigateur: string | null;
}

export const admin = {
  configurateurs: () => appeler<ConfigurateurAdmin[]>('GET', '/admin/configurateurs'),
  // Écriture réservée au rôle Super administrateur.
  creerConfigurateur: (c: ConfigurateurSaisie) => appeler<ConfigurateurAdmin[]>('POST', '/admin/configurateurs', c),
  modifierConfigurateur: (id: number, c: ConfigurateurSaisie) => appeler<ConfigurateurAdmin[]>('PUT', `/admin/configurateurs/${id}`, c),
  basculerConfigurateur: (id: number) => appeler<ConfigurateurAdmin[]>('POST', `/admin/configurateurs/${id}/basculer`),
  supprimerConfigurateur: (id: number) => appeler<ConfigurateurAdmin[]>('DELETE', `/admin/configurateurs/${id}`),

  familles: () => appeler<FamilleAdmin[]>('GET', '/admin/familles'),
  creerFamille: (f: FamilleSaisie) => appeler<FamilleAdmin[]>('POST', '/admin/familles', f),
  modifierFamille: (id: number, f: FamilleSaisie) => appeler<FamilleAdmin[]>('PUT', `/admin/familles/${id}`, f),
  basculerFamille: (id: number) => appeler<FamilleAdmin[]>('POST', `/admin/familles/${id}/basculer`),
  supprimerFamille: (id: number) => appeler<FamilleAdmin[]>('DELETE', `/admin/familles/${id}`),

  produits: () => appeler<ProduitAdmin[]>('GET', '/admin/produits'),
  creerProduit: (p: ProduitSaisie) => appeler<ProduitAdmin[]>('POST', '/admin/produits', p),
  modifierProduit: (id: number, p: ProduitSaisie) => appeler<ProduitAdmin[]>('PUT', `/admin/produits/${id}`, p),
  supprimerProduit: (id: number) => appeler<ProduitAdmin[]>('DELETE', `/admin/produits/${id}`),
  associer: (id: number, familleId: number | null) => appeler<ProduitAdmin[]>('PUT', `/admin/produits/${id}/famille`, { familleId }),

  /** Sans recherche : clients déjà accrédités ; avec recherche : clients ERP correspondants (50 au plus). */
  accreditations: (q?: string) => appeler<AccreditationClient[]>('GET', `/admin/accreditations${qs({ q })}`),
  enregistrerAccreditations: (code: string, produits: number[]) =>
    appeler<AccreditationClient[]>('PUT', `/admin/accreditations/${encodeURIComponent(code)}`, { produits }),

  creerFamilleCommentaire: (nom: string, ordre: number) => appeler<void>('POST', '/admin/commentaires/familles', { nom, ordre }),
  modifierFamilleCommentaire: (id: number, nom: string, ordre: number) => appeler<void>('PUT', `/admin/commentaires/familles/${id}`, { nom, ordre }),
  supprimerFamilleCommentaire: (id: number) => appeler<void>('DELETE', `/admin/commentaires/familles/${id}`),
  deplacerFamilleCommentaire: (id: number, sens: number) => appeler<void>('POST', `/admin/commentaires/familles/${id}/deplacer`, { sens }),
  creerModele: (familleId: number, ordre: number, texte: string) => appeler<void>('POST', '/admin/commentaires/modeles', { familleId, ordre, texte }),
  modifierModele: (id: number, familleId: number, ordre: number, texte: string) =>
    appeler<void>('PUT', `/admin/commentaires/modeles/${id}`, { familleId, ordre, texte }),
  supprimerModele: (id: number) => appeler<void>('DELETE', `/admin/commentaires/modeles/${id}`),
  deplacerModele: (id: number, sens: number) => appeler<void>('POST', `/admin/commentaires/modeles/${id}/deplacer`, { sens }),

  messages: () => appeler<Message[]>('GET', '/admin/messages'),
  publierMessage: (audience: string, titre: string, corps: string) => appeler<void>('POST', '/admin/messages', { audience, titre, corps }),
  supprimerMessage: (id: number) => appeler<void>('DELETE', `/admin/messages/${id}`),

  connexions: (filtres: { q?: string; resultat?: string; du?: string; au?: string }) => appeler<Connexion[]>('GET', `/admin/connexions${qs(filtres)}`),
};

