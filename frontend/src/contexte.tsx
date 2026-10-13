import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react';
import { definirRole, type Session } from './api';

/** SuperAdministrateur : poste de développement uniquement (session.superAdministrateurDisponible), gère les configurateurs. */
export type Role = 'Utilisateur' | 'Administrateur' | 'SuperAdministrateur';

export const libellesRole: Record<Role, string> = {
  Utilisateur: 'Utilisateur',
  Administrateur: 'Administrateur',
  SuperAdministrateur: 'Super administrateur',
};

export const estAdministrateur = (r: Role) => r === 'Administrateur' || r === 'SuperAdministrateur';
export type Vue = 'dashboard' | 'users' | 'messages' | 'admin' | 'help';

export const accesParRole: Record<Role, Vue[]> = {
  SuperAdministrateur: ['dashboard', 'users', 'messages', 'admin', 'help'],
  Administrateur: ['dashboard', 'users', 'messages', 'admin', 'help'],
  Utilisateur: ['dashboard'],
};

// Rôle mémorisé dans le navigateur ; le super administrateur n'est repris que s'il est disponible.
function roleEnregistre(session: Session): Role {
  const r = lire('cf-role');
  if (r === 'SuperAdministrateur') return session.superAdministrateurDisponible ? r : 'Administrateur';
  return r === 'Administrateur' ? r : 'Utilisateur';
}

interface ContexteApp {
  session: Session;
  role: Role;
  definirRole: (r: Role) => void;
  environnement: string;
  definirEnvironnement: (e: string) => void;
  sombre: boolean;
  basculerTheme: () => void;
  vue: Vue;
  afficherVue: (v: Vue) => void;
  notifier: (message: string) => void;
}

const Contexte = createContext<ContexteApp | null>(null);

export function useApp() {
  const c = useContext(Contexte);
  if (!c) throw new Error('useApp doit être utilisé dans <FournisseurApp>.');
  return c;
}

// Les préférences d'affichage sont conservées dans le navigateur, comme dans la maquette.
function lire(cle: string) {
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
  const [role, setRole] = useState<Role>(() => roleEnregistre(session));
  const [environnement, setEnvironnement] = useState(() => {
    const enregistre = lire('cf-environment');
    return session.environnements.some((e) => e.code === enregistre) ? enregistre! : session.environnementParDefaut;
  });
  const [sombre, setSombre] = useState(() => lire('cf-theme') === 'dark');
  // L'écran affiché est reflété dans l'adresse (#admin, #help…) pour pouvoir la mettre en favori.
  const [vue, setVue] = useState<Vue>(() => {
    const demande = window.location.hash.slice(1) as Vue;
    return accesParRole[roleEnregistre(session)].includes(demande) ? demande : 'dashboard';
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
    definirRole(role);
    document.body.dataset.role = role;
  }, [role]);
  useEffect(() => {
    document.body.dataset.env = environnement === 'production' || environnement === 'local' ? environnement : 'test';
  }, [environnement]);
  useEffect(() => {
    document.body.classList.toggle('dark', sombre);
  }, [sombre]);

  const afficherVue = useCallback(
    (v: Vue) => {
      if (!accesParRole[role].includes(v)) {
        notifier(`Cet onglet n’est pas accessible avec le rôle ${libellesRole[role]}`);
        v = 'dashboard';
      }
      setVue(v);
      window.scrollTo(0, 0);
    },
    [role, notifier],
  );

  const changerRole = (r: Role) => {
    setRole(r);
    ecrire('cf-role', r);
    if (!accesParRole[r].includes(vue)) setVue('dashboard');
    notifier(`Vue adaptée au rôle : ${libellesRole[r]}`);
  };

  const changerEnvironnement = (e: string) => {
    setEnvironnement(e);
    ecrire('cf-environment', e);
    notifier(e === 'production' ? 'Mode PRODUCTION activé' : e === 'local' ? 'Mode LOCAL activé' : 'Mode TEST activé');
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
        definirRole: changerRole,
        environnement,
        definirEnvironnement: changerEnvironnement,
        sombre,
        basculerTheme,
        vue,
        afficherVue,
        notifier,
      }}
    >
      {children}
      <div className={`toast ${toast.visible ? 'show' : ''}`}>{toast.message}</div>
    </Contexte.Provider>
  );
}
