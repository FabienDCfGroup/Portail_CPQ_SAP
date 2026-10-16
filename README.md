# CF Saisie commandes

Portail web de saisie des commandes et devis : choix du client, livraison ou retrait, lancement des configurateurs CPQ via le Middleware, finalisation et envoi à SAP.

## Arborescence

```
backend/                      ASP.NET Core (.NET 8)
├─ src/CfSaisie.Api           API REST consommée par le front
├─ src/CfSaisie.Cpq           Requête de lancement des configurateurs (reprise de l'ancien outil) + client Middleware
├─ src/CfSaisie.Data          Base SQL Server CfSaisieCommandes (EF Core, migrations) ; schéma erp = tables alimentées par l'ERP
├─ sql/                       Scripts SQL des migrations, pour les DBA et l'équipe ERP
├─ src/CfSaisie.Sap           Contrats des données ERP (clients, adresses, agences) + client de l'API Planning CCX
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
2. **Certificat HTTPS de développement** (une seule fois ; le portail est servi en HTTPS, `https://localhost:5173` étant l'adresse de retour déclarée dans Entra ID). Le certificat .NET « localhost » est exporté pour Vite dans `frontend/.certificats/`, exclu de Git :
   ```
   dotnet dev-certs https --trust
   dotnet dev-certs https --export-path frontend/.certificats/localhost.pem --format Pem --no-password
   ```
3. **Installation** (une seule fois, à la racine du dépôt ; installe aussi les paquets du front) :
   ```
   npm install
   ```
4. **Lancement de l'API et du front en une commande**, à la racine du dépôt :
   ```
   npm run dev
   ```
   - API : http://localhost:5075 (crée et alimente la base `CfSaisieCommandes` au premier démarrage), lignes préfixées `[api]` ;
   - front : https://localhost:5173, lignes préfixées `[front]`.

   Le portail **s'ouvre automatiquement** dans le navigateur (https://localhost:5173) ; il affiche « Démarrage du serveur… » le temps que l'API soit prête. `Ctrl+C` arrête les deux ; si l'un des deux s'arrête, l'autre est arrêté aussi.

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
| `Authentification:TenantId` / `ClientId` | Locataire et inscription d'application Microsoft Entra ID (GUID) |
| `Authentification:ClientSecret` | **Secret** client de l'inscription : `dotnet user-secrets set "Authentification:ClientSecret" "<secret>" --project backend/src/CfSaisie.Api` |
| `Authentification:Groupes:Utilisateur` / `Administrateur` / `SuperAdministrateur` | Identifiants (GUID) des groupes Entra ID donnant chaque rôle |
| `Authentification:AdresseRetour` | Facultatif : adresse de retour déclarée dans Entra ID si c'est la racine du portail ; la page d'accueil transmet alors le code à `/api/signin-oidc`. Poste de développement : `https://localhost:5173` (`appsettings.Development.json`). Vide : `{portail}/api/signin-oidc` |
| `Export:DossierXml` | Dossier d'envoi à SAP : la validation d'une commande ou d'un devis y crée un répertoire `{ConfigId}` contenant `{ConfigId}_portailSAP.xml` (toutes les données saisies + données ERP du client, de l'adresse et de l'agence) et `{ConfigId}.xml` (XML de fin de configuration lu dans le Middleware par `GetXmlForSite`, recopié tel quel). La validation est refusée si le XML du CPQ ne peut pas être lu. **Différent par environnement** : `C:\CfSaisieCommandeSAP` sur le poste (`appsettings.Development.json`), variable `Export__DossierXml` (fichier `.env` du conteneur) en test et en production. L'API refuse de démarrer si le dossier n'existe pas ou n'est pas accessible en écriture |
| `PlanningCcx:Url` | API Planning CCX (n8n) : disponibilités de fabrication par code CCX (instance de test pour l'instant) |
| `PlanningCcx:CleApi` | En-tête `X-API-Key` — **secret** : `dotnet user-secrets set "PlanningCcx:CleApi" "<clé>" --project backend/src/CfSaisie.Api`. Absente : API simulée (poste de développement uniquement) |
| `PlanningCcx:NbJours` / `SeuilPlacesLimitees` | Horizon du calendrier (90 j) ; jusqu'à 2 places restantes, le jour est « places limitées » |

### Langues

- **Table `Langues`** (`Code`, `Libelle`, `Actif`, `Ordre`) : FR, EN, DE, NL actives ; PT présente mais désactivée (aucun configurateur traduit). Passer `Actif` à 0 retire une langue du sélecteur, sans modifier le code.
- **Portail** : textes traduits dans `frontend/src/i18n/textes/*.ts` (clé = texte français). Un texte sans traduction s'affiche en français et est signalé dans la console en développement.
- **Configurateur** : il s'ouvre dans la langue de l'interface, transmise par l'en-tête `Accept-Language` et vérifiée par l'API (langue inactive ou non gérée par les configurateurs → langue par défaut du configurateur).

### Suivi des commandes

Écran « Suivi des commandes » : les commandes et devis du portail (`dbo.Dossiers`, alimentée à la création et à la validation), complétés par les données ERP de `erp.Commandes` (statut ERP, fin de production, livraisons) rapprochées par numéro SAP, sinon par ConfigId. Le portail lit le schéma `erp` sans jamais y écrire.

**Transformer un devis en commande** (bouton du suivi, sur un devis envoyé) : le portail crée une commande « En cours » liée au devis (`DevisSourceId`), qui reprend son client, sa livraison, ses références et **sa configuration (même ConfigId)** ; la date de fin de production est à rechoisir. Dans l'assistant, « Ouvrir la configuration en commande » relance le configurateur en modification avec `TypeTransaction = Order` : le Middleware fait passer la configuration de devis (D) à commande (C), puis l'utilisateur la valide dans le configurateur. La finalisation n'est possible qu'après ce passage. À la validation, les deux XML du répertoire `{ConfigId}` sont **remplacés** (Type = Commande, `DevisSource` = référence du devis) et le devis affiche « → CF-… ». Une transformation commencée et non validée est reprise (« Reprendre la transformation »), jamais dupliquée. Le devis doit avoir été enregistré dans le configurateur, sinon le Middleware ne le connaît pas (« No config found »).

### Date de fin de production

À la fermeture du configurateur, l'API lit le XML de fin de configuration dans le Middleware (`GetXmlForSite`) et y cherche l'attribut `ccx` (`/Dossier/Attributs/Attribut[@Key="ccx"]`), calculé par le configurateur (aujourd'hui le Liner) :

- **code CCX présent** : calendrier de l'API Planning CCX pour ce code et le site de production ; la date est revérifiée à la validation ;
- **pas de code CCX**, ou pas de planning pour ce code : date de fin de production saisie librement ;
- **XML absent** : la configuration n'a pas encore été enregistrée dans le configurateur.

## Connexion et rôles (Microsoft Entra ID)

Connexion Microsoft (OpenID Connect, code + PKCE) gérée par l'API, avec un cookie de session (`CfSaisie.Session`, 8 h glissantes). Toutes les routes `/api` exigent un rôle, attribué à la connexion selon le groupe Entra ID (le plus élevé l'emporte) :

| Groupe Entra ID | Rôle | Accès |
|---|---|---|
| Utilisateur | Utilisateur | Création commande/devis, suivi des commandes, ouverture d'une configuration existante |
| Administrateur | Administrateur | + familles, produits, accréditations, messages, journal des connexions |
| Super administrateur | Super administrateur | + table `Configurateurs` (onglet « Configurateurs ») |
| aucun | — | Accès refusé (tentative tracée) |

Chaque connexion (réussie, refusée, en échec) est tracée dans la table `JournalConnexions` (date, compte, nom, rôle, IP, navigateur), consultable par les administrateurs (écran « Journal des connexions »). Il n'y a plus de table d'utilisateurs : les comptes et groupes sont gérés dans Entra ID.

**Inscription d'application Entra ID** (à faire configurer par l'équipe Microsoft 365) :
- plateforme **Web**, adresses de redirection `https://<serveur>/api/signin-oidc` (poste de développement : `https://localhost:5173`), URL de déconnexion `https://<serveur>/api/signout-callback-oidc` ;
- un **secret client** (ou certificat) ;
- **Configuration du jeton → revendication de groupes** : « Groupes de sécurité », limitée aux **groupes attribués à l'application** (sinon, au-delà d'environ 200 groupes, Entra ID n'envoie plus la liste et l'accès est refusé) ; attribuer les 3 groupes à l'application.

Ajouter un produit sur un nouveau configurateur : le super administrateur déclare le configurateur (nom = `ConfiguratorName` exact du Middleware), puis un administrateur crée le produit, l'associe à une famille et l'accrédite aux clients.

## Lancement d'un configurateur

Le navigateur n'appelle jamais le Middleware : il passe par l'API, qui construit la requête `Open` et renvoie l'URL du configurateur à ouvrir.

```
Front ──► API ──► base CfSaisieCommandes (quel configurateur ?)
              ──► tables erp.* (client, langue, adresses)
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

1. Client (table `erp.Clients`, actif) + référence chantier, ticket Salesforce, livraison (contrôlés par l'API).
2. Choix du produit : il doit être accrédité pour le client, dans une famille active, avec un configurateur actif.
3. Requête `Open` : `TypeAppel = Creation`, `TypeTransaction = Order` (commande) ou `Quote` (devis), `AccountId` = code client, `BusinessCode` = référence chantier, `Language` = langue du client (`erp.Clients.Langue`, vide si non renseignée).
4. Si le Middleware accepte, le dossier est enregistré dans `Dossiers` (statut « En cours ») avec le `ConfigId` renvoyé (ex. `Volet_00000561`). En cas de refus, rien n'est enregistré.

### Modification / consultation (depuis le suivi des commandes)

1. **Ouvrir** (dossier « En cours ») : l'assistant rouvre le dossier ; « Modifier la configuration » envoie `TypeAppel = Modification` avec le `ConfigId` du dossier, dans l'environnement où il a été créé.
2. **Consulter** (dossier transmis) : `TypeAppel = Consultation`, configuration en lecture seule.
3. `TypeTransaction` selon Commande ou Devis (Order / Quote).

La saisie libre d'un ConfigId (ancienne carte « Ouvrir une configuration existante ») a été retirée du tableau de bord ; la route `POST /api/lancements` (`ServiceLancements`) reste dans l'API, inutilisée par le front.

### Pendant et après le configurateur

- **Portail bloqué** : tant que la fenêtre du configurateur est ouverte (création, modification, consultation, ouverture par ConfigId), un voile couvre le portail ; il propose de remettre le configurateur au premier plan ou de le fermer (abandon). Quitter ou recharger le portail demande confirmation.
- **Fin de configuration** (`POST /api/dossiers/{id}/fin-configuration`) : à la fermeture, l'API demande au Middleware (`GetXmlForSite`) si la configuration a été enregistrée ou validée. Sinon :
  - nouveau dossier → statut **« Abandonné »** (gardé pour trace, masqué du suivi sauf avec le filtre « Abandonné ») ;
  - changement de produit → le dossier garde son produit et sa configuration précédents (le nouveau produit n'est appliqué qu'une fois sa configuration enregistrée) ;
  - commande issue d'un devis → « Abandonné » si la configuration n'a pas été validée en commande (nature `C` dans `Entete/Nature` du XML).
  - La modification d'une configuration déjà enregistrée ne change rien : le Middleware ne permet pas de distinguer « enregistrer » d'« abandonner » dans ce cas. Middleware injoignable : rien n'est abandonné.
- Portail fermé pendant la configuration : la vérification est faite à la réouverture du dossier (« Ouvrir » dans le suivi).

### Paramètres communs à chaque requête

- **Profil Middleware** : `Middleware:Profil` (identifiant dans `appsettings.json`, mot de passe dans les user-secrets). En mode ERP, `AccountId` et `ApplicationUrl` sont toujours envoyés.
- **Serveur** : environnement choisi dans la barre du haut, parmi `Middleware:Environnements` (`local` uniquement sur le poste de développement).
- **Règles** : reprises de l'ancien outil, vérifiées par les tests de `backend/tests/CfSaisie.Cpq.Tests/Attendu`.

## Tests

```
npm test
```
