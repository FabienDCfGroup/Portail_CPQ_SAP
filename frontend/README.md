# Frontend — CF Saisie commandes

Application React + TypeScript (Vite) reproduisant la maquette `CF_Saisie_commandes_interactive_v21.html`.

Le frontend ne communique qu'avec l'API du backend (`/api`, relayée vers http://localhost:5075 en développement) : il n'appelle jamais directement le Middleware CPQ ni SAP, et ne manipule aucun identifiant technique.

```
npm install
npm run dev     # http://localhost:5173
npm run build   # vérification TypeScript + build de production dans dist/
```

| Dossier | Contenu |
|---|---|
| `src/api.ts` | Types et appels de l'API |
| `src/contexte.tsx` | Rôle, mode Production/Test, thème, écran courant, notifications |
| `src/vues/` | Écrans : création, assistant commande/devis, suivi, administration, messages, utilisateurs, aide |
| `src/styles/maquette.css` | Style de la maquette, repris tel quel |
| `src/styles/portail.css` | Compléments et corrections |
