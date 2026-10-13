import { useCallback, useEffect, useState, type ReactNode } from 'react';
import {
  admin,
  type AccreditationClient,
  type ConfigurateurAdmin,
  type ConfigurateurSaisie,
  type FamilleAdmin,
  type FamilleSaisie,
  type ProduitAdmin,
  type ProduitSaisie,
} from '../api';
import { Modale } from '../composants/Modale';
import { useApp } from '../contexte';
import { imageProduit, urlCss } from '../outils';

type Onglet = 'configurateurs' | 'familles' | 'produits' | 'associations' | 'accreditations';

const onglets: { cle: Onglet; libelle: string; superAdministrateur?: boolean }[] = [
  { cle: 'configurateurs', libelle: 'Configurateurs', superAdministrateur: true },
  { cle: 'familles', libelle: 'Familles de produits' },
  { cle: 'produits', libelle: 'Création de produits' },
  { cle: 'associations', libelle: 'Association famille / produit' },
  { cle: 'accreditations', libelle: 'Accréditation clients / produits' },
];

export function Administration() {
  const { notifier, role } = useApp();
  const superAdministrateur = role === 'SuperAdministrateur';
  const [onglet, setOnglet] = useState<Onglet>('familles');
  const [familles, setFamilles] = useState<FamilleAdmin[]>([]);
  const [produits, setProduits] = useState<ProduitAdmin[]>([]);
  const [configurateurs, setConfigurateurs] = useState<ConfigurateurAdmin[]>([]);
  const [accreditations, setAccreditations] = useState<AccreditationClient[]>([]);

  const charger = useCallback(async () => {
    try {
      const [f, p, c, a] = await Promise.all([admin.familles(), admin.produits(), admin.configurateurs(), admin.accreditations()]);
      setFamilles(f);
      setProduits(p);
      setConfigurateurs(c);
      setAccreditations(a);
    } catch (e) {
      notifier((e as Error).message);
    }
  }, [notifier]);

  useEffect(() => {
    void charger();
  }, [charger]);

  /** Exécute une modification, recharge le catalogue et affiche un message. */
  const executer = async (action: () => Promise<unknown>, message: string) => {
    try {
      await action();
      notifier(message);
      await charger();
      return true;
    } catch (e) {
      notifier((e as Error).message);
      return false;
    }
  };

  const props = { familles, produits, configurateurs, accreditations, executer };

  return (
    <section id="admin" className="view active">
      <div className="hero">
        <h1>Gestion clients & produits</h1>
        <p>Gérez séparément les familles, la création des produits, leur association aux familles et les accréditations clients.</p>
      </div>
      <div className="admin-tabs">
        {onglets
          .filter((o) => !o.superAdministrateur || superAdministrateur)
          .map((o) => (
            <button key={o.cle} className={onglet === o.cle ? 'active' : ''} onClick={() => setOnglet(o.cle)}>
              {o.libelle}
            </button>
          ))}
      </div>
      {onglet === 'configurateurs' && superAdministrateur && <OngletConfigurateurs {...props} />}
      {onglet === 'familles' && <OngletFamilles {...props} />}
      {onglet === 'produits' && <OngletProduits {...props} />}
      {onglet === 'associations' && <OngletAssociations {...props} />}
      {onglet === 'accreditations' && <OngletAccreditations {...props} />}
    </section>
  );
}

interface PropsOnglet {
  familles: FamilleAdmin[];
  produits: ProduitAdmin[];
  configurateurs: ConfigurateurAdmin[];
  accreditations: AccreditationClient[];
  executer: (action: () => Promise<unknown>, message: string) => Promise<boolean>;
}

function Panneau({ titre, description, recherche, surRecherche, placeholder, bouton, surBouton, children }: {
  titre: string;
  description: string;
  recherche: string;
  surRecherche: (v: string) => void;
  placeholder: string;
  bouton: string;
  surBouton: () => void;
  children: ReactNode;
}) {
  return (
    <div className="admin-tab-panel active">
      <div className="card form">
        <div className="admin-section-title">
          <div>
            <h2>{titre}</h2>
            <p className="muted" style={{ margin: 0 }}>
              {description}
            </p>
          </div>
        </div>
        <div className="admin-toolbar">
          <input placeholder={placeholder} value={recherche} onChange={(e) => surRecherche(e.target.value)} />
          <button className="primary" onClick={surBouton}>
            {bouton}
          </button>
        </div>
        <div className="table-wrap">{children}</div>
      </div>
    </div>
  );
}

const imageFamille = (f: FamilleAdmin, produits: ProduitAdmin[]) => {
  const p = produits.find((x) => x.familleId === f.id);
  return imageProduit(p?.nom ?? f.nom, f.nom, f.image ?? p?.image);
};

// ----- Configurateurs (super administrateur) -----

function OngletConfigurateurs({ configurateurs, executer }: PropsOnglet) {
  const [recherche, setRecherche] = useState('');
  const [edition, setEdition] = useState<ConfigurateurAdmin | 'nouveau' | null>(null);
  const q = recherche.trim().toLowerCase();
  const visibles = configurateurs.filter((c) => !q || [c.nom, c.productionSite ?? ''].join(' ').toLowerCase().includes(q));

  return (
    <Panneau
      titre="Configurateurs"
      description="Déclarez les configurateurs du Middleware pour pouvoir y rattacher des produits. Le nom doit être exactement le ConfiguratorName déclaré dans le Middleware."
      recherche={recherche}
      surRecherche={setRecherche}
      placeholder="Rechercher un configurateur..."
      bouton="Ajouter un configurateur"
      surBouton={() => setEdition('nouveau')}
    >
      {visibles.length === 0 ? (
        <div className="muted">Aucun configurateur trouvé.</div>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Configurateur (ConfiguratorName)</th>
              <th>Site de production</th>
              <th>Produits</th>
              <th>Statut</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {visibles.map((c) => (
              <tr key={c.id}>
                <td>
                  <b className="config-id">{c.nom}</b>
                </td>
                <td>{c.productionSite ?? <span className="muted">DEL_BRECE (par défaut)</span>}</td>
                <td>{c.nombreProduits}</td>
                <td>
                  <span className={`pill ${c.actif ? 'green' : 'red'}`}>{c.actif ? 'Actif' : 'Inactif'}</span>
                </td>
                <td>
                  <div className="admin-table-actions">
                    <button className="secondary" onClick={() => void executer(() => admin.basculerConfigurateur(c.id), 'Statut du configurateur mis à jour')}>
                      {c.actif ? 'Désactiver' : 'Activer'}
                    </button>
                    <button className="secondary" onClick={() => setEdition(c)}>
                      Modifier
                    </button>
                    <button
                      className="danger"
                      disabled={c.nombreProduits > 0}
                      title={c.nombreProduits > 0 ? 'Utilisé par des produits : désactivez-le plutôt' : undefined}
                      onClick={() => window.confirm(`Supprimer le configurateur ${c.nom} ?`) && void executer(() => admin.supprimerConfigurateur(c.id), 'Configurateur supprimé')}
                    >
                      Supprimer
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {edition && (
        <EditionConfigurateur
          configurateur={edition === 'nouveau' ? null : edition}
          surFermer={() => setEdition(null)}
          surEnregistrer={async (saisie) => {
            const ok = await executer(
              () => (edition === 'nouveau' ? admin.creerConfigurateur(saisie) : admin.modifierConfigurateur(edition.id, saisie)),
              'Configurateur enregistré',
            );
            if (ok) setEdition(null);
          }}
        />
      )}
    </Panneau>
  );
}

function EditionConfigurateur({
  configurateur,
  surFermer,
  surEnregistrer,
}: {
  configurateur: ConfigurateurAdmin | null;
  surFermer: () => void;
  surEnregistrer: (s: ConfigurateurSaisie) => void;
}) {
  const [nom, setNom] = useState(configurateur?.nom ?? '');
  const [site, setSite] = useState(configurateur?.productionSite ?? '');
  const [actif, setActif] = useState(configurateur?.actif ?? true);
  return (
    <Modale
      ouverte
      titre={configurateur ? 'Modifier le configurateur' : 'Ajouter un configurateur'}
      sousTitre="Le nom est envoyé tel quel au Middleware (ConfiguratorName) et sert de préfixe aux ConfigId."
      largeur={620}
      surFermer={surFermer}
    >
      <div className="form">
        <label>Nom du configurateur *</label>
        <input className="config-id" placeholder="Ex. CHIV_Initial" value={nom} onChange={(e) => setNom(e.target.value)} />
        {configurateur && configurateur.nombreProduits > 0 && nom.trim() !== configurateur.nom && (
          <div className="erreur-ligne">
            Ce configurateur est utilisé par {configurateur.nombreProduits} produit(s) : un nom différent de celui du Middleware empêchera leur lancement.
          </div>
        )}
        <label>Site de production</label>
        <input placeholder="DEL_BRECE si vide" value={site} onChange={(e) => setSite(e.target.value)} />
        <label>Statut</label>
        <select value={actif ? 'active' : 'inactive'} onChange={(e) => setActif(e.target.value === 'active')}>
          <option value="active">Actif</option>
          <option value="inactive">Inactif</option>
        </select>
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 14 }}>
          <button className="secondary" onClick={surFermer}>
            Annuler
          </button>
          <button className="primary" onClick={() => surEnregistrer({ nom: nom.trim(), productionSite: site.trim() || null, actif })}>
            Enregistrer
          </button>
        </div>
      </div>
    </Modale>
  );
}

// ----- Familles -----

function OngletFamilles({ familles, produits, executer }: PropsOnglet) {
  const [recherche, setRecherche] = useState('');
  const [edition, setEdition] = useState<FamilleAdmin | 'nouvelle' | null>(null);
  const visibles = familles.filter((f) => !recherche.trim() || f.nom.toLowerCase().includes(recherche.trim().toLowerCase()));

  return (
    <Panneau
      titre="Familles de produits"
      description="Créez, recherchez, activez, désactivez ou supprimez une famille."
      recherche={recherche}
      surRecherche={setRecherche}
      placeholder="Rechercher une famille..."
      bouton="Créer une famille"
      surBouton={() => setEdition('nouvelle')}
    >
      {visibles.length === 0 ? (
        <div className="muted">Aucune famille trouvée.</div>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Image</th>
              <th>Famille</th>
              <th>Produits associés</th>
              <th>Statut</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {visibles.map((f) => (
              <tr key={f.id}>
                <td>
                  <div className="admin-table-thumb" style={{ backgroundImage: urlCss(imageFamille(f, produits)) }} />
                </td>
                <td>
                  <b>{f.nom}</b>
                </td>
                <td>{f.nombreProduits}</td>
                <td>
                  <span className={`pill ${f.actif ? 'green' : 'red'}`}>{f.actif ? 'Actif' : 'Inactif'}</span>
                </td>
                <td>
                  <div className="admin-table-actions">
                    <button className="secondary" onClick={() => void executer(() => admin.basculerFamille(f.id), 'Statut de la famille mis à jour')}>
                      {f.actif ? 'Désactiver' : 'Activer'}
                    </button>
                    <button className="secondary" onClick={() => setEdition(f)}>
                      Modifier
                    </button>
                    <button
                      className="danger"
                      onClick={() => {
                        const msg = f.nombreProduits
                          ? `Supprimer la famille ${f.nom} et ses ${f.nombreProduits} produit(s) associés ?`
                          : `Supprimer la famille ${f.nom} ?`;
                        if (window.confirm(msg)) void executer(() => admin.supprimerFamille(f.id), 'Famille supprimée');
                      }}
                    >
                      Supprimer
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {edition && (
        <EditionFamille
          famille={edition === 'nouvelle' ? null : edition}
          surFermer={() => setEdition(null)}
          surEnregistrer={async (saisie) => {
            const ok = await executer(
              () => (edition === 'nouvelle' ? admin.creerFamille(saisie) : admin.modifierFamille(edition.id, saisie)),
              'Famille enregistrée',
            );
            if (ok) setEdition(null);
          }}
        />
      )}
    </Panneau>
  );
}

/** Saisie d'une image : URL ou fichier local converti en data URL (comme la maquette). */
function ChampImage({ valeur, surChange }: { valeur: string; surChange: (v: string) => void }) {
  return (
    <>
      <input placeholder="URL d'image (facultatif)" value={valeur.startsWith('data:') ? '' : valeur} onChange={(e) => surChange(e.target.value)} />
      <input
        type="file"
        accept="image/*"
        style={{ marginTop: 8 }}
        onChange={(e) => {
          const fichier = e.target.files?.[0];
          if (!fichier) return;
          const lecteur = new FileReader();
          lecteur.onload = () => surChange(String(lecteur.result));
          lecteur.readAsDataURL(fichier);
        }}
      />
      <div className="image-preview" style={{ backgroundImage: valeur ? urlCss(valeur) : 'none' }} />
    </>
  );
}

function EditionFamille({ famille, surFermer, surEnregistrer }: { famille: FamilleAdmin | null; surFermer: () => void; surEnregistrer: (s: FamilleSaisie) => void }) {
  const [nom, setNom] = useState(famille?.nom ?? '');
  const [actif, setActif] = useState(famille?.actif ?? true);
  const [image, setImage] = useState(famille?.image ?? '');
  return (
    <Modale ouverte titre={famille ? 'Modifier la famille' : 'Créer une famille'} sousTitre="Définissez le nom, l’image et le statut de la famille." largeur={620} surFermer={surFermer}>
      <div className="form">
        <label>Nom de la famille</label>
        <input placeholder="Ex. Volet hors-sol" value={nom} onChange={(e) => setNom(e.target.value)} />
        <label>Statut</label>
        <select value={actif ? 'active' : 'inactive'} onChange={(e) => setActif(e.target.value === 'active')}>
          <option value="active">Actif</option>
          <option value="inactive">Inactif</option>
        </select>
        <label>Image de la famille</label>
        <ChampImage valeur={image} surChange={setImage} />
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 14 }}>
          <button className="secondary" onClick={surFermer}>
            Annuler
          </button>
          <button className="primary" onClick={() => surEnregistrer({ nom, actif, image: image || null })}>
            Enregistrer
          </button>
        </div>
      </div>
    </Modale>
  );
}

// ----- Produits -----

function OngletProduits({ produits, configurateurs, executer }: PropsOnglet) {
  const [recherche, setRecherche] = useState('');
  const [edition, setEdition] = useState<ProduitAdmin | 'nouveau' | null>(null);
  const q = recherche.trim().toLowerCase();
  const visibles = produits.filter((p) => !q || [p.nom, p.configurateur, p.lienConfigurateur ?? ''].join(' ').toLowerCase().includes(q));

  return (
    <Panneau
      titre="Création de produits"
      description="Créez et gérez les produits, leurs images et leur configurateur. L'association à une famille se fait dans l'onglet dédié."
      recherche={recherche}
      surRecherche={setRecherche}
      placeholder="Rechercher un produit ou un configurateur..."
      bouton="Créer un produit"
      surBouton={() => setEdition('nouveau')}
    >
      {visibles.length === 0 ? (
        <div className="muted">Aucun produit trouvé.</div>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Image</th>
              <th>Produit</th>
              <th>Configurateur</th>
              <th>Lien configurateur</th>
              <th>Famille</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {visibles.map((p) => (
              <tr key={p.id}>
                <td>
                  <div className="admin-table-thumb" style={{ backgroundImage: urlCss(imageProduit(p.nom, p.famille ?? '', p.image)) }} />
                </td>
                <td>
                  <b>{p.nom}</b>
                  <div className="form-note">{p.description}</div>
                </td>
                <td>
                  <span className="mini-chip">{p.configurateur}</span>
                </td>
                <td>
                  <div className="admin-table-link" title={p.lienConfigurateur ?? ''}>
                    {p.lienConfigurateur ?? <span className="muted">Non défini</span>}
                  </div>
                </td>
                <td>{p.famille ? <span className="mini-chip">{p.famille}</span> : <span className="pill red">Non associée</span>}</td>
                <td>
                  <div className="admin-table-actions">
                    <button className="secondary" onClick={() => setEdition(p)}>
                      Modifier
                    </button>
                    <button
                      className="danger"
                      onClick={() => window.confirm(`Supprimer le produit ${p.nom} ?`) && void executer(() => admin.supprimerProduit(p.id), 'Produit supprimé')}
                    >
                      Supprimer
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {edition && (
        <EditionProduit
          produit={edition === 'nouveau' ? null : edition}
          configurateurs={configurateurs}
          surFermer={() => setEdition(null)}
          surEnregistrer={async (saisie) => {
            const ok = await executer(
              () => (edition === 'nouveau' ? admin.creerProduit(saisie) : admin.modifierProduit(edition.id, saisie)),
              'Produit enregistré',
            );
            if (ok) setEdition(null);
          }}
        />
      )}
    </Panneau>
  );
}

function EditionProduit({
  produit,
  configurateurs,
  surFermer,
  surEnregistrer,
}: {
  produit: ProduitAdmin | null;
  configurateurs: ConfigurateurAdmin[];
  surFermer: () => void;
  surEnregistrer: (s: ProduitSaisie) => void;
}) {
  const [nom, setNom] = useState(produit?.nom ?? '');
  const [configurateurId, setConfigurateurId] = useState(produit?.configurateurId ?? 0);
  const [lien, setLien] = useState(produit?.lienConfigurateur ?? '');
  const [description, setDescription] = useState(produit?.description ?? '');
  const [image, setImage] = useState(produit?.image ?? '');
  return (
    <Modale
      ouverte
      titre={produit ? 'Modifier le produit' : 'Créer un produit'}
      sousTitre="Définissez le produit, son image et son configurateur. L'association à une famille se fait séparément."
      largeur={760}
      surFermer={surFermer}
    >
      <div className="form">
        <div className="catalog-form-grid">
          <div>
            <label>Nom du produit</label>
            <input placeholder="Ex. Rolleasy2" value={nom} onChange={(e) => setNom(e.target.value)} />
          </div>
          <div>
            <label>Configurateur (Configurator Name)</label>
            <select value={configurateurId} onChange={(e) => setConfigurateurId(Number(e.target.value))}>
              <option value={0}>Choisir un configurateur</option>
              {configurateurs.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.nom}
                  {c.actif ? '' : ' — Inactif'}
                </option>
              ))}
            </select>
          </div>
        </div>
        <label>Lien configurateur</label>
        <input type="url" placeholder="https://... (facultatif, le lancement passe par le Middleware CPQ)" value={lien} onChange={(e) => setLien(e.target.value)} />
        <label>Description</label>
        <input placeholder="Description courte du produit" value={description} onChange={(e) => setDescription(e.target.value)} />
        <label>Image du produit</label>
        <ChampImage valeur={image} surChange={setImage} />
        <div style={{ display: 'flex', gap: 8, marginTop: 14, justifyContent: 'flex-end' }}>
          <button className="secondary" onClick={surFermer}>
            Annuler
          </button>
          <button
            className="primary"
            onClick={() => surEnregistrer({ nom, configurateurId, lienConfigurateur: lien || null, description: description || null, image: image || null })}
          >
            Enregistrer le produit
          </button>
        </div>
      </div>
    </Modale>
  );
}

// ----- Associations famille / produit -----

function OngletAssociations({ familles, produits, executer }: PropsOnglet) {
  const [recherche, setRecherche] = useState('');
  const [edition, setEdition] = useState<{ produitId: number | null } | null>(null);
  const q = recherche.trim().toLowerCase();
  const visibles = produits.filter((p) => !q || [p.nom, p.famille ?? ''].join(' ').toLowerCase().includes(q));

  return (
    <Panneau
      titre="Association famille / produit"
      description="Classez chaque produit dans la famille correspondante."
      recherche={recherche}
      surRecherche={setRecherche}
      placeholder="Rechercher un produit ou une famille..."
      bouton="Lier un produit à une famille"
      surBouton={() => setEdition({ produitId: null })}
    >
      {visibles.length === 0 ? (
        <div className="muted">Aucun produit trouvé.</div>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Image</th>
              <th>Produit</th>
              <th>Famille actuelle</th>
              <th>Configurateur</th>
              <th>Action</th>
            </tr>
          </thead>
          <tbody>
            {visibles.map((p) => (
              <tr key={p.id}>
                <td>
                  <div className="admin-table-thumb" style={{ backgroundImage: urlCss(imageProduit(p.nom, p.famille ?? '', p.image)) }} />
                </td>
                <td>
                  <b>{p.nom}</b>
                </td>
                <td>{p.famille ? <span className="mini-chip">{p.famille}</span> : <span className="pill red">Non associée</span>}</td>
                <td>{p.configurateur}</td>
                <td>
                  <button className="secondary" onClick={() => setEdition({ produitId: p.id })}>
                    {p.famille ? 'Modifier l’association' : 'Associer'}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {edition && (
        <EditionAssociation
          produitInitial={edition.produitId}
          familles={familles}
          produits={produits}
          surFermer={() => setEdition(null)}
          surEnregistrer={async (produitId, familleId) => {
            const ok = await executer(() => admin.associer(produitId, familleId), 'Association enregistrée');
            if (ok) setEdition(null);
          }}
        />
      )}
    </Panneau>
  );
}

function EditionAssociation({
  produitInitial,
  familles,
  produits,
  surFermer,
  surEnregistrer,
}: {
  produitInitial: number | null;
  familles: FamilleAdmin[];
  produits: ProduitAdmin[];
  surFermer: () => void;
  surEnregistrer: (produitId: number, familleId: number | null) => void;
}) {
  const [produitId, setProduitId] = useState<number | null>(produitInitial);
  const [familleId, setFamilleId] = useState<number | null>(produits.find((p) => p.id === produitInitial)?.familleId ?? null);
  const { notifier } = useApp();
  return (
    <Modale ouverte titre="Associer le produit à une famille" sousTitre="Choisissez la famille dans laquelle ranger le produit." largeur={620} surFermer={surFermer}>
      <div className="form">
        <label>Produit</label>
        <select
          value={produitId ?? ''}
          onChange={(e) => {
            const id = e.target.value ? Number(e.target.value) : null;
            setProduitId(id);
            setFamilleId(produits.find((p) => p.id === id)?.familleId ?? null);
          }}
        >
          <option value="">Choisir un produit</option>
          {produits.map((p) => (
            <option key={p.id} value={p.id}>
              {p.nom}
            </option>
          ))}
        </select>
        <label>Famille</label>
        <select value={familleId ?? ''} onChange={(e) => setFamilleId(e.target.value ? Number(e.target.value) : null)}>
          <option value="">Choisir une famille</option>
          {familles.map((f) => (
            <option key={f.id} value={f.id}>
              {f.nom}
              {f.actif ? '' : ' — Inactive'}
            </option>
          ))}
        </select>
        <div style={{ display: 'flex', gap: 8, marginTop: 14, justifyContent: 'flex-end' }}>
          <button className="secondary" onClick={surFermer}>
            Annuler
          </button>
          <button
            className="primary"
            onClick={() => {
              if (produitId === null) return notifier('Choisissez un produit');
              if (familleId === null) return notifier('Choisissez une famille');
              surEnregistrer(produitId, familleId);
            }}
          >
            Enregistrer l'association
          </button>
        </div>
      </div>
    </Modale>
  );
}

// ----- Accréditations clients / produits -----

function Puces({ valeurs }: { valeurs: string[] }) {
  return (
    <div className="rights-summary" style={{ marginTop: 5 }}>
      {valeurs.slice(0, 3).map((v) => (
        <span key={v} className="mini-chip">
          {v}
        </span>
      ))}
      {valeurs.length > 3 && <span className="mini-chip">+{valeurs.length - 3}</span>}
    </div>
  );
}

function OngletAccreditations({ familles, produits, accreditations, executer }: PropsOnglet) {
  const [recherche, setRecherche] = useState('');
  const [client, setClient] = useState<string | null>(null);
  const q = recherche.trim().toLowerCase();
  const visibles = accreditations.filter((a) => !q || `${a.codeClient} ${a.raisonSociale}`.toLowerCase().includes(q));

  return (
    <Panneau
      titre="Accréditation clients / produits"
      description="Définissez les produits accessibles pour chaque client. Les familles accessibles sont déduites automatiquement."
      recherche={recherche}
      surRecherche={setRecherche}
      placeholder="Rechercher un code client ou une raison sociale..."
      bouton="Lier des produits à un client"
      surBouton={() => setClient(accreditations[0]?.codeClient ?? null)}
    >
      {visibles.length === 0 ? (
        <div className="muted">Aucun client trouvé.</div>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Code client</th>
              <th>Raison sociale</th>
              <th>Produits autorisés</th>
              <th>Familles accessibles</th>
              <th>Action</th>
            </tr>
          </thead>
          <tbody>
            {visibles.map((a) => {
              const autorises = produits.filter((p) => a.produits.includes(p.id));
              const fams = [...new Set(autorises.map((p) => p.famille).filter((f): f is string => !!f))];
              return (
                <tr key={a.codeClient}>
                  <td>
                    <b>{a.codeClient}</b>
                  </td>
                  <td>{a.raisonSociale}</td>
                  <td>
                    {autorises.length}
                    <Puces valeurs={autorises.map((p) => p.nom)} />
                  </td>
                  <td>
                    {fams.length}
                    <Puces valeurs={fams} />
                  </td>
                  <td>
                    <button className="secondary" onClick={() => setClient(a.codeClient)}>
                      Gérer les accès
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
      {client && (
        <EditionAccreditations
          codeInitial={client}
          accreditations={accreditations}
          familles={familles}
          produits={produits}
          surFermer={() => setClient(null)}
          surEnregistrer={async (code, ids, nom) => {
            const ok = await executer(() => admin.enregistrerAccreditations(code, ids), `Droits enregistrés pour ${nom}`);
            if (ok) setClient(null);
          }}
        />
      )}
    </Panneau>
  );
}

function EditionAccreditations({
  codeInitial,
  accreditations,
  familles,
  produits,
  surFermer,
  surEnregistrer,
}: {
  codeInitial: string;
  accreditations: AccreditationClient[];
  familles: FamilleAdmin[];
  produits: ProduitAdmin[];
  surFermer: () => void;
  surEnregistrer: (code: string, produits: number[], nom: string) => void;
}) {
  const [code, setCode] = useState(codeInitial);
  const [coches, setCoches] = useState<number[]>(accreditations.find((a) => a.codeClient === codeInitial)?.produits ?? []);
  const client = accreditations.find((a) => a.codeClient === code);

  const basculer = (ids: number[], coche: boolean) => setCoches((c) => (coche ? [...new Set([...c, ...ids])] : c.filter((x) => !ids.includes(x))));

  return (
    <Modale ouverte titre="Accréditation du client" sousTitre="Cochez les produits accessibles à ce client." largeur={820} surFermer={surFermer}>
      <div className="form">
        <label>Client</label>
        <select
          value={code}
          onChange={(e) => {
            setCode(e.target.value);
            setCoches(accreditations.find((a) => a.codeClient === e.target.value)?.produits ?? []);
          }}
        >
          {accreditations.map((a) => (
            <option key={a.codeClient} value={a.codeClient}>
              {a.raisonSociale} — {a.codeClient}
            </option>
          ))}
        </select>
        <div className="permissions">
          {familles.map((f) => {
            const ps = produits.filter((p) => p.familleId === f.id);
            if (!ps.length) return null;
            const toute = ps.every((p) => coches.includes(p.id));
            return (
              <div key={f.id} className="rights-family">
                <div className="rights-family-head">
                  <b>
                    {f.nom}
                    {!f.actif && (
                      <span className="pill red" style={{ marginLeft: 6 }}>
                        Inactif
                      </span>
                    )}
                  </b>
                  <label className="form-note">
                    <input
                      type="checkbox"
                      checked={toute}
                      onChange={(e) =>
                        basculer(
                          ps.map((p) => p.id),
                          e.target.checked,
                        )
                      }
                    />{' '}
                    Toute la famille
                  </label>
                </div>
                <div className="rights-products">
                  {ps.map((p) => (
                    <label key={p.id} className="perm">
                      <input type="checkbox" checked={coches.includes(p.id)} onChange={(e) => basculer([p.id], e.target.checked)} />
                      <span>
                        <b>{p.nom}</b>
                        <br />
                        <small className="muted">{p.configurateur}</small>
                      </span>
                    </label>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
        <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 14 }}>
          <button className="primary" onClick={() => client && surEnregistrer(code, coches, client.raisonSociale)}>
            Enregistrer les droits
          </button>
        </div>
      </div>
    </Modale>
  );
}
