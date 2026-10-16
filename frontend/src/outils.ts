// Fonctions d'affichage reprises de la maquette.
import { locale } from './i18n';

const palettes: Record<string, [string, string]> = {
  Liner: ['#8fe1ff', '#1479f8'],
  'Volet hors-sol': ['#d7e5ef', '#7a9ab4'],
  'Volet immergé': ['#aeeaff', '#2f8dcc'],
  "Couverture d'hivernage": ['#6e8793', '#243947'],
};

function echapperXml(v: string) {
  return v.replace(/[&<>"']/g, (m) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[m]!);
}

/** Visuel par défaut d'un produit ou d'une famille (dégradé + nom), comme makeProductImage de la maquette. */
export function imageProduit(nom: string, famille: string, image?: string | null): string {
  if (image) return image;
  const [c1, c2] = palettes[famille] ?? ['#9bdfff', '#1479f8'];
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="360"><defs><linearGradient id="g" x1="0" x2="1"><stop stop-color="${c1}"/><stop offset="1" stop-color="${c2}"/></linearGradient></defs><rect width="640" height="360" fill="url(#g)"/><path d="M0 245 Q150 180 320 250 T640 235 V360 H0Z" fill="rgba(255,255,255,.30)"/><circle cx="525" cy="72" r="55" fill="rgba(255,255,255,.22)"/><text x="34" y="245" font-family="Arial" font-size="26" fill="white" opacity=".86">${echapperXml(famille)}</text><text x="34" y="294" font-family="Arial" font-size="38" font-weight="700" fill="white">${echapperXml(nom)}</text></svg>`;
  return `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(svg)}`;
}

export const urlCss = (url: string) => `url("${url.replace(/"/g, '%22')}")`;

export const dateLongue = (isoJour: string) =>
  new Date(`${isoJour}T00:00:00`).toLocaleDateString(locale(), { weekday: 'long', day: '2-digit', month: 'long', year: 'numeric' });

/** « Thomas Martin » → « T.Martin » (colonne « Créé par » du suivi, comme la maquette). */
export function nomCourt(nomComplet: string) {
  const parties = nomComplet.trim().split(/\s+/).filter(Boolean);
  if (!parties.length) return '—';
  if (parties.length === 1) return parties[0];
  return `${parties[0].charAt(0).toUpperCase()}.${parties.slice(1).join(' ')}`;
}

export const dateHeure = (iso: string) =>
  new Date(iso).toLocaleString(locale(), { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });

/** Date AAAA-MM-JJ (sans fuseau) → JJ/MM/AAAA ; « — » si absente. */
export const dateCourte = (isoJour: string | null | undefined) =>
  isoJour ? new Date(`${isoJour}T00:00:00`).toLocaleDateString(locale()) : '—';

export const vide = (v: string | null | undefined) => (v && v.trim() ? v.trim() : null);
