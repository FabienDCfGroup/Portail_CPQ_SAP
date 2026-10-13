import { useState } from 'react';
import logo from './assets/logo-cf.png';
// Version allégée (720p, sans audio, ~2 Mo) de CF_GROUP_Piscine_Animee_15s_FullHD_final.mp4 (31 Mo).
import videoFond from './assets/fond-piscine-720p.mp4';
import { accesParRole, estAdministrateur, libellesRole, useApp, type Role, type Vue } from './contexte';
import { AssistantDossier, type OuvertureAssistant } from './vues/AssistantDossier';
import { Creation } from './vues/Creation';
import { Utilisateurs } from './vues/Utilisateurs';
import { GestionMessages } from './vues/GestionMessages';
import { Administration } from './vues/Administration';
import { Aide } from './vues/Aide';

const menu: { vue: Vue; icone: string; libelle: string }[] = [
  { vue: 'dashboard', icone: '⌂', libelle: 'Création commande/devis' },
  { vue: 'users', icone: '♙', libelle: 'Utilisateurs & rôles' },
  { vue: 'messages', icone: '✉', libelle: 'Gestion des messages' },
  { vue: 'admin', icone: '⚙', libelle: 'Gestion clients & produits' },
  { vue: 'help', icone: '?', libelle: 'Aide' },
];

const titresLangue: Record<string, string> = {
  fr: 'CF Saisie commandes',
  en: 'CF Order Entry',
  nl: 'CF Orderinvoer',
  pt: 'CF Entrada de encomendas',
};

export function App() {
  const app = useApp();
  const { session, role, vue } = app;
  const [assistant, setAssistant] = useState<OuvertureAssistant | null>(null);
  const estAdmin = estAdministrateur(role);
  const roles: Role[] = session.superAdministrateurDisponible
    ? ['Utilisateur', 'Administrateur', 'SuperAdministrateur']
    : ['Utilisateur', 'Administrateur'];
  const envCourant = session.environnements.find((e) => e.code === app.environnement);

  const changerEnvironnement = (code: string) => {
    if (code === 'production') {
      const ok = window.confirm(
        'Passer en PRODUCTION ?\n\nLes configurateurs seront lancés sur le Middleware de production et créeront de vraies configurations.',
      );
      if (!ok) return;
    }
    app.definirEnvironnement(code);
  };

  const changerLangue = (langue: string) => {
    document.documentElement.lang = langue;
    document.title = titresLangue[langue] ?? titresLangue.fr;
    app.notifier(`Langue : ${langue.toUpperCase()}`);
  };

  return (
    <div className="app">
      {/* Fond animé décoratif, atténué par CSS (.fond-video) pour ne pas gêner la lecture. */}
      <video className="fond-video" src={videoFond} autoPlay muted loop playsInline aria-hidden="true" tabIndex={-1} />
      <aside className="sidebar">
        <div className="brand">
          <img src={logo} alt="CF Group" />
          <div className="brand-name">
            CF Saisie <span>commandes</span>
          </div>
        </div>
        <nav className="nav">
          {menu
            .filter((m) => accesParRole[role].includes(m.vue))
            .map((m) => (
              <button key={m.vue} data-view={m.vue} className={vue === m.vue ? 'active' : ''} onClick={() => app.afficherVue(m.vue)}>
                <span className="ico">{m.icone}</span>
                <span>{m.libelle}</span>
              </button>
            ))}
        </nav>
        <div className="role-note">
          <b>Mode {libellesRole[role]}</b>
          <br />
          <small>
            {role === 'SuperAdministrateur'
              ? 'Toutes les fonctions, dont la gestion des configurateurs.'
              : estAdmin
                ? 'Accès à tous les onglets et à toutes les fonctions.'
                : 'Accès uniquement à Création commande/devis.'}
          </small>
        </div>
      </aside>

      <main className="main">
        <div className="topbar">
          <div style={{ flex: 1 }} />
          <span className="env-badge">{envCourant?.libelle ?? app.environnement.toUpperCase()}</span>
          <select className="mode-select" value={app.environnement} onChange={(e) => changerEnvironnement(e.target.value)} title="Choisir le mode">
            {session.environnements.map((e) => (
              <option key={e.code} value={e.code}>
                {e.code === 'production' ? 'Production' : e.code === 'local' ? 'Local' : 'Test'}
              </option>
            ))}
          </select>
          <button className="theme-btn" onClick={app.basculerTheme} title={app.sombre ? 'Activer le mode clair' : 'Activer le mode sombre'}>
            {app.sombre ? '☀' : '◐'}
          </button>
          <select className="lang-select" defaultValue="fr" onChange={(e) => changerLangue(e.target.value)}>
            <option value="fr">FR</option>
            <option value="en">EN</option>
            <option value="nl">NL</option>
            <option value="pt">PT</option>
          </select>
          <select className="role-select" value={role} onChange={(e) => app.definirRole(e.target.value as Role)}>
            {roles.map((r) => (
              <option key={r} value={r}>
                {libellesRole[r]}
              </option>
            ))}
          </select>
          <div className="user">
            <div className="avatar">{session.initiales}</div>
            <div>
              <b>{session.utilisateur}</b>
              <div className="muted" style={{ fontSize: 12 }}>
                {session.societe}
              </div>
            </div>
          </div>
        </div>

        <div className="content">
          {vue === 'dashboard' && <Creation ouvrir={setAssistant} />}
          {vue === 'users' && <Utilisateurs />}
          {vue === 'messages' && <GestionMessages />}
          {vue === 'admin' && <Administration />}
          {vue === 'help' && <Aide />}
        </div>
      </main>

      {assistant && (
        <AssistantDossier
          ouverture={assistant}
          surFermer={() => setAssistant(null)}
          surValide={() => {
            setAssistant(null);
            app.afficherVue('dashboard');
          }}
        />
      )}
    </div>
  );
}
