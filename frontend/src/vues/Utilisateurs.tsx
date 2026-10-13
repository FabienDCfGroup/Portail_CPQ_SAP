import { useEffect, useState } from 'react';
import { admin, type Utilisateur } from '../api';
import { useApp } from '../contexte';

export function Utilisateurs() {
  const { notifier } = useApp();
  const [utilisateurs, setUtilisateurs] = useState<Utilisateur[]>([]);

  useEffect(() => {
    admin.utilisateurs().then(setUtilisateurs).catch((e: Error) => notifier(e.message));
  }, [notifier]);

  return (
    <section id="users" className="view active">
      <div className="hero">
        <h1>Utilisateurs & rôles</h1>
        <p>Les administrateurs disposent des écrans de gestion. Le rôle Utilisateur accède uniquement au tableau de bord.</p>
      </div>
      <div className="admin-grid">
        <div className="card">
          <h2>Règles d’accès</h2>
          <div className="permissions">
            <div className="perm">
              <b>Super administrateur</b>
              <span className="muted">Droits de l'administrateur, plus la déclaration des configurateurs du Middleware. Poste de développement uniquement.</span>
            </div>
            <div className="perm">
              <b>Administrateur</b>
              <span className="muted">Accès à tous les onglets, gestion des utilisateurs, familles, produits, droits clients et messages.</span>
            </div>
            <div className="perm">
              <b>Utilisateur</b>
              <span className="muted">Accès uniquement à Création commande/devis.</span>
            </div>
          </div>
        </div>
        <div className="card">
          <h2>Utilisateurs</h2>
          <table>
            <thead>
              <tr>
                <th>Nom</th>
                <th>Rôle</th>
                <th>Statut</th>
              </tr>
            </thead>
            <tbody>
              {utilisateurs.map((u) => (
                <tr key={u.id}>
                  <td>{u.nom}</td>
                  <td>{u.role}</td>
                  <td>
                    <span className="pill green">{u.statut}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}
