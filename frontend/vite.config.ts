import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// En développement, les appels /api sont relayés vers l'API ASP.NET Core (backend/src/CfSaisie.Api).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Échoue clairement si le port est pris (portail déjà lancé) au lieu de basculer sur un autre port.
    strictPort: true,
    // Ouvre le portail dans le navigateur au lancement de « npm run dev ».
    open: true,
    proxy: {
      '/api': 'http://localhost:5075',
    },
  },
});
