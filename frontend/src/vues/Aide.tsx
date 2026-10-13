export function Aide() {
  return (
    <section id="help" className="view active">
      <div className="hero">
        <h1>Aide</h1>
        <p>CF Saisie commandes — saisie des commandes et devis, lancement des configurateurs CPQ.</p>
      </div>
      <div className="card" style={{ marginTop: 18 }}>
        <h3>Parcours principal</h3>
        <p>
          Tableau de bord → Création d'une commande → rechercher le client → renseigner la référence chantier, le ticket Salesforce et la livraison → choisir la
          famille disponible → choisir le produit autorisé. Le configurateur du produit s'ouvre dans une nouvelle fenêtre ; fermez-la après avoir enregistré la
          configuration, puis choisissez la date de fin de production et validez.
        </p>
        <h3>Modifier ou consulter une configuration existante</h3>
        <p>
          Tableau de bord → Ouvrir une configuration existante → choisir Commande ou Devis, Modification ou Consultation → saisir le ConfigId (ex.
          Volet_00000561) et le code client. Le configurateur est déduit du ConfigId et s'ouvre dans une nouvelle fenêtre, sur l'environnement choisi dans la
          barre du haut.
        </p>
        <h3>Rôles</h3>
        <p>
          Administrateur voit tous les onglets, dont Gestion clients & produits. Utilisateur voit uniquement Création commande/devis. Super
          administrateur (poste de développement uniquement, en attendant l'authentification) déclare en plus les configurateurs du Middleware
          (onglet Configurateurs), auxquels les administrateurs rattachent ensuite les produits.
        </p>
        <h3>Mode Production / Test</h3>
        <p>
          Le mode choisi dans la barre du haut détermine le Middleware CPQ utilisé pour les nouveaux lancements (recette en mode Test, production en mode
          Production). Pour rouvrir une configuration existante, choisissez le mode de l'environnement où elle a été créée.
        </p>
      </div>
    </section>
  );
}
