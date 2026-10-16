/// <reference types="node" />
import { existsSync, readFileSync } from 'node:fs';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Certificat HTTPS de développement (certificat .NET « localhost », reconnu par Windows), exporté une fois avec :
//   dotnet dev-certs https --export-path frontend/.certificats/localhost.pem --format Pem --no-password
// Portail en HTTPS (https://localhost:5173/, adresse de retour déclarée dans Entra ID) dès que le certificat est présent.
const certificat = new URL('./.certificats/localhost.pem', import.meta.url);
const cle = new URL('./.certificats/localhost.key', import.meta.url);
const https = existsSync(certificat) && existsSync(cle) ? { cert: readFileSync(certificat), key: readFileSync(cle) } : undefined;

// En développement, les appels /api sont relayés vers l'API ASP.NET Core (backend/src/CfSaisie.Api).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    https,
    // Échoue clairement si le port est pris (portail déjà lancé) au lieu de basculer sur un autre port.
    strictPort: true,
    // Ouvre le portail dans le navigateur au lancement de « npm run dev ».
    open: true,
    proxy: {
      // changeOrigin: false : l'API reçoit l'adresse du navigateur (localhost:5173), pour poser le cookie de session
      // sur l'adresse du portail. xfwd : l'API sait que le navigateur est en https (en-têtes X-Forwarded-*).
      '/api': { target: 'http://localhost:5075', changeOrigin: false, xfwd: true },
    },
  },
});
