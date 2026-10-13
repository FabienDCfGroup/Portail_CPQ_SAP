# Requêtes attendues — lancement des configurateurs

Chaque fichier décrit un cas de lancement :

- `entree` : ce que le portail fournit (profil Middleware, environnement, demande de l'utilisateur) ;
- `requeteAttendue` : les champs de l'`OpenRequest` envoyée au Middleware ;
- `source` : la méthode et les lignes de l'ancien outil (`Lancement configurateur`) dont la règle est tirée.

Les valeurs attendues ont été **déduites de la lecture du code** de `Configurateur.CreeRequete` et de `PanneauPrincipal.ExecuterConfigurateur`, version publiée (16/02/2026).

## Règles de comparaison

1. Les champs listés dans `requeteAttendue` doivent avoir exactement cette valeur.
2. **Tous les autres champs** doivent garder la valeur d'un `new OpenRequest()` de la DLL de l'éditeur : `null`, `0`, `false`, sauf `ShowDiscountPrice = true` et `WebSessionId = ""` (valeurs initialisées par le constructeur de la DLL).
3. `DateValidite` n'est pas listée : elle vaut l'heure de l'appel et fait l'objet d'un test dédié.
4. Le mot de passe vaut `***` dans les tests ; il n'y a aucun secret réel dans ces fichiers.

## Écarts volontaires avec l'ancien outil

| Cas | Ancien outil | Portail | Raison |
|---|---|---|---|
| `07` | `ConfiguratorName` = texte avant le premier `_` du ConfigId → `CHIV` | Configurateur reconnu parmi ceux du portail : le plus long nom suivi de `_` → `CHIV_Initial` (`ConfigIdCpq`, testé dans `ConfigIdCpqTests`) | `CHIV_Initial` et `CHIV_Supergreen` sont des configurateurs distincts |
| `14` | `ProductionSite` toujours `DEL_BRECE` (en dur) | Valeur paramétrée par configurateur en BDD, `DEL_BRECE` par défaut | Décision projet |
| `08`, `09` | Mode ERP déclenché par le login `cfgAxDelBrece@techform.fr` | Mode ERP = réglage du profil | Le profil SAP remplacera `cfgAxDelBrece` |

Les cas Benelux et Hornbach de l'ancien outil ne sont pas repris (décision projet).

## Cas d'erreur (sans fichier)

| Cas | Comportement attendu | Source |
|---|---|---|
| Configurateur absent | Refus de la demande | `PanneauPrincipal.cs` l.313-320 et l.411 |
| ConfigId absent en modification ou consultation | Refus de la demande | `Configurateur.cs` l.34 : sans ConfigId exploitable, `ConfiguratorName` reste vide et l'outil bloque (l.411) |
