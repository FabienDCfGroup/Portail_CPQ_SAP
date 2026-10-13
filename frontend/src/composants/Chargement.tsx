import logo from '../assets/logo-cf.png';

// Écran d'attente au logo CF Group : le logo, dans une pastille blanche, flotte ; des ondes s'en éloignent
// comme à la surface de l'eau, et un reflet le traverse. Le même style sert à l'application (démarrage) et à la fenêtre du configurateur,
// qui n'a pas accès aux feuilles de style du portail : il est donc fourni en texte.
const style = `
.chargement{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;text-align:center;
  font-family:Inter,Segoe UI,Arial,sans-serif;color:#0b1f53;background:radial-gradient(circle at 50% 42%,#ffffff 0,#eaf6ff 45%,#d6ecfb 100%)}
body.dark .chargement{color:#eaf2ff;background:radial-gradient(circle at 50% 42%,#1b2c45 0,#122036 50%,#0d1524 100%)}
.chargement-scene{position:relative;width:300px;height:300px;margin:0 auto 10px;display:grid;place-items:center}
.chargement-onde{position:absolute;inset:50% auto auto 50%;width:170px;height:170px;margin:-85px 0 0 -85px;border-radius:50%;
  border:2px solid rgba(20,121,248,.5);opacity:0;animation:chargement-onde 2.7s cubic-bezier(.2,.6,.35,1) infinite}
body.dark .chargement-onde{border-color:rgba(88,184,255,.55)}
.chargement-onde:nth-child(2){animation-delay:.9s}
.chargement-onde:nth-child(3){animation-delay:1.8s}
/* Le logo a un fond blanc opaque : il est présenté dans une pastille blanche, que le reflet traverse. */
.chargement-logo{position:relative;width:170px;height:170px;border-radius:50%;overflow:hidden;background:#fff;
  display:grid;place-items:center;box-shadow:0 14px 34px rgba(11,31,83,.20),inset 0 0 0 1px rgba(20,121,248,.12);
  animation:chargement-flotte 3s ease-in-out infinite}
body.dark .chargement-logo{box-shadow:0 14px 34px rgba(0,0,0,.45),0 0 0 4px rgba(88,184,255,.18)}
.chargement-logo img{display:block;width:128px;height:auto}
.chargement-reflet{position:absolute;inset:0;pointer-events:none;
  background:linear-gradient(105deg,transparent 35%,rgba(160,215,255,.55) 50%,transparent 65%) no-repeat;background-size:260% 100%;
  animation:chargement-reflet 2.4s ease-in-out infinite}
.chargement-titre{margin:0;font-size:20px;font-weight:800}
.chargement-texte{margin:6px 0 0;color:#70809b;font-size:14px}
body.dark .chargement-texte{color:#9aaac1}
.chargement-points::after{content:'';display:inline-block;width:1.2em;text-align:left;animation:chargement-points 1.4s steps(4) infinite}
@keyframes chargement-onde{0%{transform:scale(.95);opacity:.9}100%{transform:scale(1.75);opacity:0}}
@keyframes chargement-flotte{0%,100%{transform:translateY(0) scale(1)}50%{transform:translateY(-7px) scale(1.04)}}
@keyframes chargement-reflet{0%{background-position:130% 0}60%,100%{background-position:-30% 0}}
@keyframes chargement-points{0%{content:''}25%{content:'.'}50%{content:'..'}75%{content:'...'}}
@media (prefers-reduced-motion:reduce){.chargement *,.chargement *::after{animation:none!important}.chargement-onde{opacity:.25;transform:scale(1.3)}}
`;

const scene = (urlLogo: string) =>
  `<div class="chargement-scene" aria-hidden="true">
    <span class="chargement-onde"></span><span class="chargement-onde"></span><span class="chargement-onde"></span>
    <div class="chargement-logo"><img src="${urlLogo}" alt=""><div class="chargement-reflet"></div></div>
  </div>`;

const echapper = (v: string) => v.replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]!);

/** Écran d'attente plein écran du portail (démarrage de l'application). */
export function EcranChargement({ titre, texte }: { titre: string; texte?: string }) {
  return (
    <div className="chargement" role="status" aria-live="polite">
      <style>{style}</style>
      <div>
        <div dangerouslySetInnerHTML={{ __html: scene(logo) }} />
        <p className="chargement-titre">{titre}</p>
        {texte && (
          <p className="chargement-texte">
            <span className="chargement-points">{texte}</span>
          </p>
        )}
      </div>
    </div>
  );
}

/**
 * Page d'attente écrite dans la fenêtre du configurateur, le temps que le Middleware réponde.
 * L'URL du logo est rendue absolue : la fenêtre (about:blank) ne résout pas les chemins du portail de la même façon.
 */
export function pageAttenteConfigurateur(titre: string, texte = 'Le Middleware CPQ prépare la session') {
  const urlLogo = new URL(logo, window.location.href).href;
  const sombre = document.body.classList.contains('dark') ? ' class="dark"' : '';
  return `<!doctype html><html lang="fr"><head><meta charset="utf-8"><title>CF Configurator</title><style>body{margin:0}${style}</style></head>
<body${sombre}><div class="chargement" role="status"><div>${scene(urlLogo)}
<p class="chargement-titre">${echapper(titre)}</p>
<p class="chargement-texte"><span class="chargement-points">${echapper(texte)}</span></p></div></div></body></html>`;
}
