# CF Saisie commandes

Portail web de saisie des commandes et devis : choix du client, livraison ou retrait, lancement des configurateurs CPQ via le Middleware, finalisation et envoi à SAP.

## Arborescence

```
backend/                      ASP.NET Core (.NET 8)
├─ src/CfSaisie.Api           API REST consommée par le front
├─ src/CfSaisie.Cpq           Requête de lancement des configurateurs (reprise de l'ancien outil) + client Middleware
├─ src/CfSaisie.Data          Base SQL Server CfSaisieCommandes (EF Core, migrations) ; schéma erp = tables alimentées par l'ERP
├─ sql/                       Scripts SQL des migrations, pour les DBA et l'équipe ERP
├─ src/CfSaisie.Sap           Interfaces SAP + implémentation simulée
├─ tests/CfSaisie.Cpq.Tests   Tests de compatibilité des requêtes avec l'ancien outil
└─ lib/                       SmartDataExchangeDefines.dll (contrats du Middleware, éditeur)
frontend/                     React + TypeScript (Vite)
```

## Démarrage en local

Prérequis : .NET SDK 8, Node.js 20+, accès à `N-BRECE133\SQLEXPRESS01` (authentification Windows).

1. **Mot de passe du profil Middleware** (une seule fois, stocké hors du dépôt) :
   ```
   dotnet user-secrets set "Middleware:Profil:MotDePasse" "<mot de passe>" --project backend/src/CfSaisie.Api
   ```
2. **Installation** (une seule fois, à la racine du dépôt ; installe aussi les paquets du front) :
   ```
   npm install
   ```
3. **Lancement de l'API et du front en une commande**, à la racine du dépôt :
   ```
   npm run dev
   ```
   - API : http://localhost:5075 (crée et alimente la base `CfSaisieCommandes` au premier démarrage), lignes préfixées `[api]` ;
   - front : http://localhost:5173, lignes préfixées `[front]`.

   Le portail **s'ouvre automatiquement** dans le navigateur (http://localhost:5173) ; il affiche « Démarrage du serveur… » le temps que l'API soit prête. `Ctrl+C` arrête les deux ; si l'un des deux s'arrête, l'autre est arrêté aussi.

Pour lancer séparément : `npm run dev:api` ou `npm run dev:front`.

Si le lancement échoue avec « address already in use » (API) ou « Port 5173 is already in use » (front), le portail tourne déjà dans un autre terminal : fermez-le, ou arrêtez les processus `CfSaisie.Api` et `node` correspondants.

## Configuration (`backend/src/CfSaisie.Api/appsettings.json`)

| Clé | Rôle |
|---|---|
| `ConnectionStrings:CfSaisie` | Base SQL Server du portail |
| `Middleware:Profil` | Profil technique d'appel du Middleware (`ModeErp` : comportement « appel ERP ») |
| `Middleware:Environnements` | Serveurs Middleware par mode (`test` = recette, `production`) |
| `Middleware:Environnements:local` | Middleware installé sur le poste de développement (`n-brece133.fija.local`). Déclaré **uniquement** dans `appsettings.Development.json` : absent des déploiements. Profil propre possible via `Middleware:Environnements:local:Profil` |
| `Middleware:Simulation` | `true` : aucun appel réseau, URL fictive |
| `Portail:UtilisateurSimule` | Utilisateur affiché tant que l'authentification réelle n'est pas branchée |

## Rôles (simulés en attendant l'authentification)

| Rôle | Accès |
|---|---|
| Utilisateur | Création commande/devis, ouverture d'une configuration existante |
| Administrateur | + familles, produits, accréditations, messages, utilisateurs |
| Super administrateur | + table `Configurateurs` (onglet « Configurateurs ») ; **poste de développement uniquement** (API en mode Development) |

Ajouter un produit sur un nouveau configurateur : le super administrateur déclare le configurateur (nom = `ConfiguratorName` exact du Middleware), puis un administrateur crée le produit, l'associe à une famille et l'accrédite aux clients.

## Lancement d'un configurateur

Le navigateur n'appelle jamais le Middleware : il passe par l'API, qui construit la requête `Open` et renvoie l'URL du configurateur à ouvrir.

```
Front ──► API ──► base CfSaisieCommandes (quel configurateur ?)
              ──► SAP simulé (client, langue)
              ──► Middleware : POST https://{serveur}/Cfg_Middleware/Service.svc/Open
Front ◄── URL du configurateur + ConfigId ◄──┘
```

### Tables utilisées

| Table | Rôle au lancement |
|---|---|
| `Configurateurs` | `Nom` = `ConfiguratorName` envoyé au Middleware ; `ProductionSite` (`DEL_BRECE` si vide) ; `Actif` |
| `Produits` | Produit choisi par l'utilisateur → son `ConfigurateurId` |
| `Familles` | Le produit doit appartenir à une famille `Actif` |
| `Accreditations` | Couple (`CodeClient`, `ProduitId`) : le produit est autorisé pour ce client |
| `Dossiers` | Commande ou devis du portail ; conserve `ConfigId` et `Environnement` après la création |

### Création (assistant « Création d'une commande / d'un devis »)

1. Client SAP + référence chantier, ticket Salesforce, livraison (contrôlés par l'API).
2. Choix du produit : il doit être accrédité pour le client, dans une famille active, avec un configurateur actif.
3. Requête `Open` : `TypeAppel = Creation`, `TypeTransaction = Order` (commande) ou `Quote` (devis), `AccountId` = code client, `BusinessCode` = référence chantier, `Language` = langue du client SAP.
4. Si le Middleware accepte, le dossier est enregistré dans `Dossiers` (statut « En cours ») avec le `ConfigId` renvoyé (ex. `Volet_00000561`). En cas de refus, rien n'est enregistré.

### Modification / consultation (carte « Ouvrir une configuration existante »)

1. Saisie de Commande ou Devis, Modification ou Consultation, du ConfigId et du code client.
2. Le configurateur est déduit du ConfigId : le plus long `Configurateurs.Nom` suivi de `_` (`CHIV_Initial_00000012` → `CHIV_Initial`).
3. Requête `Open` : `TypeAppel = Modification` ou `Consultation`, `ConfigId`, `TypeTransaction` selon Commande ou Devis.
4. Rien n'est écrit en base.

### Paramètres communs à chaque requête

- **Profil Middleware** : `Middleware:Profil` (identifiant dans `appsettings.json`, mot de passe dans les user-secrets). En mode ERP, `AccountId` et `ApplicationUrl` sont toujours envoyés.
- **Serveur** : environnement choisi dans la barre du haut, parmi `Middleware:Environnements` (`local` uniquement sur le poste de développement).
- **Règles** : reprises de l'ancien outil, vérifiées par les tests de `backend/tests/CfSaisie.Cpq.Tests/Attendu`.

## Tests

```
npm test
```
