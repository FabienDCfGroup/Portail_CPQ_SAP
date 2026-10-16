import { useState } from 'react';
import logo from './assets/logo-cf.png';
// Version allégée (720p, sans audio, ~2 Mo) de CF_GROUP_Piscine_Animee_15s_FullHD_final.mp4 (31 Mo).
import videoFond from './assets/fond-piscine-720p.mp4';
import { traduire } from './i18n';
import { accesParRole, estAdministrateur, libellesRole, useApp, type Vue } from './contexte';
import { AssistantDossier, type OuvertureAssistant } from './vues/AssistantDossier';
import { Creation } from './vues/Creation';
import { Suivi } from './vues/Suivi';
import { JournalConnexions } from './vues/JournalConnexions';
import { GestionMessages } from './vues/GestionMessages';
import { Administration } from './vues/Administration';
import { Aide } from './vues/Aide';

const menu: { vue: Vue; icone: string; libelle: string }[] = [
  { vue: 'dashboard', icone: '⌂', libelle: 'Création commande/devis' },
  { vue: 'tracking', icone: '▧', libelle: 'Suivi des commandes' },
  { vue: 'connexions', icone: '♙', libelle: 'Journal des connexions' },
  { vue: 'messages', icone: '✉', libelle: 'Gestion des messages' },
  { vue: 'admin', icone: '⚙', libelle: 'Gestion clients & produits' },
  { vue: 'help', icone: '?', libelle: 'Aide' },
];

export function App() {
  const app = useApp();
  const { session, role, vue, t } = app;
  const [assistant, setAssistant] = useState<OuvertureAssistant | null>(null);
  // Incrémenté à chaque fermeture de l'assistant pour rafraîchir le suivi.
  const [versionDossiers, setVersionDossiers] = useState(0);
  const estAdmin = estAdministrateur(role);
  const envCourant = session.environnements.find((e) => e.code === app.environnement);

  const changerEnvironnement = (code: string) => {
    if (code === 'production') {
      const ok = window.confirm(
        t('Passer en PRODUCTION ?\n\nLes configurateurs seront lancés sur le Middleware de production et créeront de vraies configurations.'),
      );
      if (!ok) return;
    }
    app.definirEnvironnement(code);
  };

  // La langue choisie traduit le portail et est envoyée aux configurateurs lancés ensuite.
  const changerLangue = (langue: string) => {
    app.definirLangue(langue);
    app.notifier(traduire(langue, 'Langue : {langue}', { langue: session.langues.find((l) => l.code === langue)?.libelle ?? langue }));
  };

  return (
    <div className="app">
      {/* Fond animé décoratif, atténué par CSS (.fond-video) pour ne pas gêner la lecture. */}
      <video className="fond-video" src={videoFond} autoPlay muted loop playsInline aria-hidden="true" tabIndex={-1} />
      <aside className="sidebar">
        <div className="brand">
          <img src={logo} alt="CF Group" />
          <div className="brand-name">
            {t('CF Saisie')} <span>{t('commandes')}</span>
          </div>
        </div>
        <nav className="nav">
          {menu
            .filter((m) => accesParRole[role].includes(m.vue))
            .map((m) => (
              <button key={m.vue} data-view={m.vue} className={vue === m.vue ? 'active' : ''} onClick={() => app.afficherVue(m.vue)}>
                <span className="ico">{m.icone}</span>
                <span>{t(m.libelle)}</span>
              </button>
            ))}
        </nav>
        <div className="role-note">
          <b>{t('Mode {role}', { role: t(libellesRole[role]) })}</b>
          <br />
          <small>
            {t(
              role === 'SuperAdministrateur'
                ? 'Toutes les fonctions, dont la gestion des configurateurs.'
                : estAdmin
                  ? 'Accès à tous les onglets et à toutes les fonctions.'
                  : 'Accès à Création commande/devis et au Suivi des commandes.',
            )}
          </small>
        </div>
      </aside>

      <main className="main">
        <div className="topbar">
          <div style={{ flex: 1 }} />
          <span className="env-badge">{envCourant ? t(envCourant.libelle) : app.environnement.toUpperCase()}</span>
          <select className="mode-select" value={app.environnement} onChange={(e) => changerEnvironnement(e.target.value)} title={t('Choisir le mode')}>
            {session.environnements.map((e) => (
              <option key={e.code} value={e.code}>
                {t(e.code === 'production' ? 'Production' : e.code === 'local' ? 'Local' : 'Test')}
              </option>
            ))}
          </select>
          <button className="theme-btn" onClick={app.basculerTheme} title={t(app.sombre ? 'Activer le mode clair' : 'Activer le mode sombre')}>
            {app.sombre ? '☀' : '◐'}
          </button>
          {/* Langues actives de la table Langues : une langue désactivée en base disparaît du sélecteur. */}
          <select className="lang-select" value={app.langue} onChange={(e) => changerLangue(e.target.value)} title={t('Langue')}>
            {session.langues.map((l) => (
              <option key={l.code} value={l.code} title={l.libelle}>
                {l.code.toUpperCase()}
              </option>
            ))}
          </select>
          {/* Personne connectée (Entra ID) : le rôle vient de ses groupes et ne se change pas ici. */}
          <div className="user" title={session.compte ?? undefined}>
            <div className="avatar">{session.initiales}</div>
            <div>
              <b>{session.utilisateur}</b>
              <div className="muted" style={{ fontSize: 12 }}>
                {t(libellesRole[role])} · {session.societe}
              </div>
            </div>
          </div>
          <a className="secondary bouton-deconnexion" href="/api/deconnexion" title={t('Se déconnecter')}>
            {t('Déconnexion')}
          </a>
        </div>

        <div className="content">
          {vue === 'dashboard' && <Creation ouvrir={setAssistant} />}
          {vue === 'tracking' && <Suivi ouvrir={setAssistant} version={versionDossiers} />}
          {vue === 'connexions' && <JournalConnexions />}
          {vue === 'messages' && <GestionMessages />}
          {vue === 'admin' && <Administration />}
          {vue === 'help' && <Aide />}
        </div>
      </main>

      {assistant && (
        <AssistantDossier
          ouverture={assistant}
          surFermer={() => {
            setAssistant(null);
            setVersionDossiers((v) => v + 1);
          }}
          surValide={() => {
            setAssistant(null);
            setVersionDossiers((v) => v + 1);
            // Un dossier rouvert depuis le suivi y revient après validation ; une création revient à l'accueil.
            app.afficherVue(vue === 'tracking' ? 'tracking' : 'dashboard');
          }}
        />
      )}
    </div>
  );
}
