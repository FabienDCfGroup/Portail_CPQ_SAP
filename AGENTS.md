# Instructions du projet

Contexte du projet

Développer une application web permettant de créer des commandes et des devis, de lancer des configurateurs CPQ existants et d'échanger des données avec SAP.

Technologies

Frontend : React + TypeScript

Backend : ASP.NET Core Web API (C#)

Base de données : SQL Server

Versionnement : Git / GitHub

Application existante

Le projet Ancien_Launcher_CPQ contient le code source de l'application locale actuellement utilisée pour construire les requêtes de lancement des configurateurs.

Ce code constitue la référence fonctionnelle et technique.

Ne jamais modifier les fichiers de l'application existante.

Objectifs de réutilisation

Identifier les données nécessaires au lancement de chaque configurateur.

Retrouver les classes, méthodes et traitements construisant les requêtes.

Documenter les règles métier et les paramètres.

Réutiliser les traitements compatibles avec le nouveau backend.

Réécrire uniquement les parties dépendantes de l'ancienne interface ou de son environnement local.

Conserver la compatibilité des requêtes produites.

Intégration SAP

Isoler les échanges SAP dans des services dédiés.

Ne pas supposer l'existence d'API ou de champs SAP non documentés.

Prévoir des interfaces et des implémentations simulées pendant le développement.

Ne jamais exposer les identifiants SAP dans le frontend.

Règles de travail

Le projet doit être complètement indépendant du Projet source. Aucun lien ne doit être fait.

Avant toute modification importante, proposer un plan.

Ne pas modifier l'ancien projet.

Ne pas ajouter l'ancien code source au nouveau dépôt Git.

Ne pas effectuer de commit ou de push sans validation.

Écrire des tests comparant les requêtes générées avec celles de l'application existante.

Expliquer les choix techniques et les changements réalisés.