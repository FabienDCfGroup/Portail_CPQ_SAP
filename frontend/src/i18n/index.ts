// Traduction du portail. Le français est la langue de référence : le texte français sert de clé,
// et chaque fichier de ./textes fournit ses traductions (en, de, nl) pour les textes d'un écran.
// Un texte non traduit s'affiche en français (et est signalé en développement).

export type Valeurs = Record<string, string | number>;

/** Traductions d'un écran : texte français → texte traduit, par code langue. */
export type Traductions = Partial<Record<string, Record<string, string>>>;

export const LANGUE_SOURCE = 'fr';

const fragments = import.meta.glob<{ default: Traductions }>('./textes/*.ts', { eager: true });
const dictionnaires: Record<string, Record<string, string>> = {};
for (const fragment of Object.values(fragments)) {
  for (const [langue, textes] of Object.entries(fragment.default)) {
    Object.assign((dictionnaires[langue] ??= {}), textes);
  }
}

const signales = new Set<string>();

/** Traduit un texte français ; « {nom} » est remplacé par valeurs.nom. */
export function traduire(langue: string, texte: string, valeurs?: Valeurs): string {
  let modele = texte;
  if (langue !== LANGUE_SOURCE) {
    const traduit = dictionnaires[langue]?.[texte];
    if (traduit !== undefined) {
      modele = traduit;
    } else if (import.meta.env.DEV && !signales.has(`${langue}|${texte}`)) {
      signales.add(`${langue}|${texte}`);
      console.warn(`[i18n] Texte non traduit en « ${langue} » : ${texte}`);
    }
  }
  return valeurs ? modele.replace(/\{(\w+)\}/g, (m, cle: string) => (cle in valeurs ? String(valeurs[cle]) : m)) : modele;
}

const locales: Record<string, string> = { fr: 'fr-FR', en: 'en-GB', de: 'de-DE', nl: 'nl-NL', pt: 'pt-PT' };
let localeCourante = locales.fr;

/** Format des dates et nombres de la langue choisie (utilisé par outils.ts). */
export function definirLocale(langue: string) {
  localeCourante = locales[langue] ?? locales.fr;
}

export const locale = () => localeCourante;
