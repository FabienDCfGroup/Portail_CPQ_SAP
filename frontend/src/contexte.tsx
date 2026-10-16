import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react';
import { definirLangueApi, type Session } from './api';
import { VerrouConfigurateur } from './composants/VerrouConfigurateur';
import { definirLocale, traduire, type Valeurs } from './i18n';

/** Rôle attribué à la connexion par les groupes Entra ID ; SuperAdministrateur gère en plus les configurateurs. */
export type Role = 'Utilisateur' | 'Administrateur' | 'SuperAdministrateur';

export const libellesRole: Record<Role, string> = {
  Utilisateur: 'Utilisateur',
  Administrateur: 'Administrateur',
  SuperAdministrateur: 'Super administrateur',
};

export const estAdministrateur = (r: Role) => r === 'Administrateur' || r === 'SuperAdministrateur';
export type Vue = 'dashboard' | 'tracking' | 'connexions' | 'messages' | 'admin' | 'help';

export const accesParRole: Record<Role, Vue[]> = {
  SuperAdministrateur: ['dashboard', 'tracking', 'connexions', 'messages', 'admin', 'help'],
  Administrateur: ['dashboard', 'tracking', 'connexions', 'messages', 'admin', 'help'],
  Utilisateur: ['dashboard', 'tracking'],
};

/** Langue mémorisée, si elle est encore active ; sinon la langue par défaut. */
export function langueEnregistree(session: Session): string {
  const l = lire('cf-langue');
  return session.langues.some((x) => x.code === l) ? l! : session.langueParDefaut;
}

interface ContexteApp {
  session: Session;
  /** Rôle de la personne connectée (groupes Entra ID), non modifiable dans le portail. */
  role: Role;
  environnement: string;
  definirEnvironnement: (e: string) => void;
  sombre: boolean;
  basculerTheme: () => void;
  vue: Vue;
  afficherVue: (v: Vue) => void;
  notifier: (message: string) => void;
  /** Langue de l'interface, envoyée aussi au configurateur. */
  langue: string;
  definirLangue: (langue: string) => void;
  /** Traduit un texte français dans la langue de l'interface. */
  t: (texte: string, valeurs?: Valeurs) => string;
  /**
   * Bloque le portail tant que la fenêtre du configurateur est ouverte ; la promesse est résolue à sa fermeture
   * (configuration terminée ou abandonnée).
   */
  suivreConfigurateur: (fenetre: Window, libelle: string) => Promise<void>;
}

const Contexte = createContext<ContexteApp | null>(null);

export function useApp() {
  const c = useContext(Contexte);
  if (!c) throw new Error('useApp doit être utilisé dans <FournisseurApp>.');
  return c;
}

// Les préférences d'affichage sont conservées dans le navigateur, comme dans la maquette.
export function lire(cle: string) {
  try {
    return localStorage.getItem(cle);
  } catch {
    return null;
  }
}
function ecrire(cle: string, valeur: string) {
  try {
    localStorage.setItem(cle, valeur);
  } catch {
    // Stockage indisponible (navigation privée) : la préférence n'est simplement pas conservée.
  }
}

export function FournisseurApp({ session, children }: { session: Session; children: ReactNode }) {
  const role: Role = session.role;
  const [environnement, setEnvironnement] = useState(() => {
    const enregistre = lire('cf-environment');
    return session.environnements.some((e) => e.code === enregistre) ? enregistre! : session.environnementParDefaut;
  });
  const [sombre, setSombre] = useState(() => lire('cf-theme') === 'dark');
  const [langue, setLangue] = useState(() => langueEnregistree(session));
  const t = useCallback((texte: string, valeurs?: Valeurs) => traduire(langue, texte, valeurs), [langue]);
  // L'écran affiché est reflété dans l'adresse (#admin, #help…) pour pouvoir la mettre en favori.
  const [vue, setVue] = useState<Vue>(() => {
    const demande = window.location.hash.slice(1) as Vue;
    return accesParRole[session.role].includes(demande) ? demande : 'dashboard';
  });
  useEffect(() => {
    window.history.replaceState(null, '', `#${vue}`);
  }, [vue]);
  const [toast, setToast] = useState<{ message: string; visible: boolean }>({ message: '', visible: false });
  const minuteur = useRef<number>();

  const notifier = useCallback((message: string) => {
    setToast({ message, visible: true });
    window.clearTimeout(minuteur.current);
    minuteur.current = window.setTimeout(() => setToast((t) => ({ ...t, visible: false })), 2300);
  }, []);

  useEffect(() => {
    document.body.dataset.role = role;
  }, [role]);
  useEffect(() => {
    document.body.dataset.env = environnement === 'production' || environnement === 'local' ? environnement : 'test';
  }, [environnement]);
  useEffect(() => {
    document.body.classList.toggle('dark', sombre);
  }, [sombre]);
  useEffect(() => {
    definirLangueApi(langue);
    definirLocale(langue);
    document.documentElement.lang = langue;
    document.title = traduire(langue, 'CF Saisie commandes');
  }, [langue]);

  // Fenêtre du configurateur ouverte : le portail est bloqué par un voile jusqu'à sa fermeture.
  const [configurateur, setConfigurateur] = useState<{ fenetre: Window; libelle: string } | null>(null);
  const finConfigurateur = useRef<(() => void) | null>(null);

  const suivreConfigurateur = useCallback((fenetre: Window, libelle: string) => {
    finConfigurateur.current?.();
    setConfigurateur({ fenetre, libelle });
    return new Promise<void>((resolve) => {
      const surveillance = window.setInterval(() => {
        if (!fenetre.closed) return;
        terminer();
      }, 500);
      function terminer() {
        window.clearInterval(surveillance);
        finConfigurateur.current = null;
        setConfigurateur((c) => (c?.fenetre === fenetre ? null : c));
        resolve();
      }
      finConfigurateur.current = terminer;
    });
  }, []);

  // Quitter ou recharger le portail pendant la configuration : le navigateur demande confirmation.
  useEffect(() => {
    if (!configurateur) return;
    const avertir = (e: BeforeUnloadEvent) => {
      e.preventDefault();
      e.returnValue = '';
    };
    window.addEventListener('beforeunload', avertir);
    return () => window.removeEventListener('beforeunload', avertir);
  }, [configurateur]);

  const afficherVue = useCallback(
    (v: Vue) => {
      if (!accesParRole[role].includes(v)) {
        notifier(t('Cet onglet n’est pas accessible avec le rôle {role}', { role: t(libellesRole[role]) }));
        v = 'dashboard';
      }
      setVue(v);
      window.scrollTo(0, 0);
    },
    [role, notifier, t],
  );

  const changerEnvironnement = (e: string) => {
    setEnvironnement(e);
    ecrire('cf-environment', e);
    notifier(t(e === 'production' ? 'Mode PRODUCTION activé' : e === 'local' ? 'Mode LOCAL activé' : 'Mode TEST activé'));
  };

  const changerLangue = (l: string) => {
    setLangue(l);
    ecrire('cf-langue', l);
  };

  const basculerTheme = () => {
    setSombre((s) => {
      ecrire('cf-theme', s ? 'light' : 'dark');
      return !s;
    });
  };

  return (
    <Contexte.Provider
      value={{
        session,
        role,
        environnement,
        definirEnvironnement: changerEnvironnement,
        sombre,
        basculerTheme,
        vue,
        afficherVue,
        notifier,
        langue,
        definirLangue: changerLangue,
        t,
        suivreConfigurateur,
      }}
    >
      {children}
      {configurateur && <VerrouConfigurateur fenetre={configurateur.fenetre} libelle={configurateur.libelle} t={t} />}
      <div className={`toast ${toast.visible ? 'show' : ''}`}>{toast.message}</div>
    </Contexte.Provider>
  );
}
