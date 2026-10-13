// Accès à l'API du backend (CfSaisie.Api). Le front n'appelle jamais directement le Middleware CPQ ni SAP.

export type TypeDossier = 'Commande' | 'Devis';
export type StatutDossier = 'EnCours' | 'Envoye' | 'EnAttente' | 'AValider' | 'Validee' | 'Erreur';
export type OrigineDossier = 'CreationInterne' | 'SiteInternet';
export type ModeLivraison = 'Chez le client' | 'A saisir' | 'Retrait en agence';
export type ActionOuverture = 'Modification' | 'Consultation';

export interface Environnement {
  code: string;
  libelle: string;
}

export interface Session {
  utilisateur: string;
  initiales: string;
  societe: string;
  environnements: Environnement[];
  environnementParDefaut: string;
  /** Vrai uniquement sur le poste de développement : le rôle Super administrateur est alors proposé. */
  superAdministrateurDisponible: boolean;
}

export interface Client {
  code: string;
  raisonSociale: string;
  pays: string;
  langue: string;
  estVic: boolean;
}

export interface AdresseLivraison {
  id: string;
  libelle: string;
  estPrincipale: boolean;
  nom: string;
  telephone: string;
  adresse: string;
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

export interface JourPlanning {
  date: string;
  etat: 'Disponible' | 'Limitee' | 'Complet';
  libelle: string;
}

export interface Planning {
  produit: string;
  delaiJours: number;
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

/** Configuration existante rouverte par son ConfigId ; le configurateur en est déduit côté serveur. */
export interface OuvertureConfiguration {
  type: TypeDossier;
  action: ActionOuverture;
  configId: string;
  codeClient: string;
  environnement: string;
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

// Rôle simulé (tant que l'authentification réelle n'est pas branchée), transmis à l'API pour les écrans d'administration.
let roleCourant = 'Utilisateur';
export function definirRole(role: string) {
  roleCourant = role;
}

async function appeler<T>(methode: string, chemin: string, corps?: unknown): Promise<T> {
  const reponse = await fetch(`/api${chemin}`, {
    method: methode,
    headers: {
      Accept: 'application/json',
      'X-Role': roleCourant,
      ...(corps !== undefined ? { 'Content-Type': 'application/json' } : {}),
    },
    body: corps !== undefined ? JSON.stringify(corps) : undefined,
  });
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
  planning: (produitId: number) => appeler<Planning>('GET', `/produits/${produitId}/planning`),
  commentaires: () => appeler<CommentaireFamille[]>('GET', '/commentaires'),

  dossier: (id: number) => appeler<Dossier>('GET', `/dossiers/${id}`),
  creerDossier: (saisie: SaisieDossier, produitId: number, environnement: string) =>
    appeler<DossierLance>('POST', '/dossiers', { saisie, produitId, environnement }),
  enregistrerDossier: (id: number, saisie: SaisieDossier) => appeler<Dossier>('PUT', `/dossiers/${id}`, saisie),
  relancerConfigurateur: (id: number, produitId: number | null, environnement: string) =>
    appeler<DossierLance>('POST', `/dossiers/${id}/configurateur`, { produitId, environnement }),
  configurationEnregistree: (id: number) => appeler<Dossier>('POST', `/dossiers/${id}/configuration-enregistree`),
  validerDossier: (id: number, saisie: SaisieDossier) => appeler<Dossier>('POST', `/dossiers/${id}/valider`, saisie),
  ouvrirConfiguration: (demande: OuvertureConfiguration) => appeler<Lancement>('POST', '/lancements', demande),
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

export interface Utilisateur {
  id: number;
  nom: string;
  role: string;
  statut: string;
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

  accreditations: () => appeler<AccreditationClient[]>('GET', '/admin/accreditations'),
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

  utilisateurs: () => appeler<Utilisateur[]>('GET', '/admin/utilisateurs'),
};

