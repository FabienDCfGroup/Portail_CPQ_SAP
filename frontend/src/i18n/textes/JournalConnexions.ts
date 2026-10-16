import type { Traductions } from '..';

// Journal des connexions (JournalConnexions.tsx), connexion / déconnexion (App.tsx, main.tsx).
const textes: Traductions = {
  en: {
    'Journal des connexions': 'Sign-in log',
    'Les utilisateurs se connectent avec leur compte Microsoft. Leur rôle dépend du groupe Entra ID auquel ils appartiennent.':
      'Users sign in with their Microsoft account. Their role depends on the Entra ID group they belong to.',
    'Règles d’accès': 'Access rules',
    'Groupe Super administrateur : droits de l’administrateur, plus la déclaration des configurateurs du Middleware.':
      'Super administrator group: administrator rights, plus declaring the Middleware configurators.',
    'Groupe Administrateur : tous les onglets, catalogue, droits clients, messages et journal des connexions.':
      'Administrator group: all tabs, catalogue, customer rights, messages and sign-in log.',
    'Groupe Utilisateur : Création commande/devis et Suivi des commandes.': 'User group: Order/quote creation and Order tracking.',
    'Aucun de ces groupes': 'None of these groups',
    'Accès refusé ; la tentative est tracée dans le journal.': 'Access denied; the attempt is recorded in the log.',
    'Nom ou compte...': 'Name or account...',
    'Tous les résultats': 'All results',
    Du: 'From',
    Au: 'To',
    Date: 'Date',
    Nom: 'Name',
    Compte: 'Account',
    Rôle: 'Role',
    Résultat: 'Result',
    'Adresse IP': 'IP address',
    'Connexion réussie': 'Successful sign-in',
    'Refusée : aucun groupe du portail': 'Denied: no portal group',
    'Échec de l’authentification': 'Authentication failed',
    'Aucune connexion ne correspond aux filtres.': 'No sign-in matches the filters.',
    'Seules les 500 connexions les plus récentes sont affichées : affinez les filtres.':
      'Only the 500 most recent sign-ins are shown: refine the filters.',
    'Se déconnecter': 'Sign out',
    Déconnexion: 'Sign out',
    'Accès refusé': 'Access denied',
    'Connexion impossible': 'Unable to sign in',
    'Votre compte Microsoft n’appartient à aucun groupe autorisé pour CF Saisie commandes. Demandez l’accès à votre administrateur.':
      'Your Microsoft account does not belong to any group authorised for CF Order Entry. Ask your administrator for access.',
    'La connexion Microsoft a échoué. Réessayez ; si le problème persiste, contactez votre administrateur.':
      'The Microsoft sign-in failed. Try again; if the problem persists, contact your administrator.',
    'Se connecter avec un autre compte': 'Sign in with another account',
  },
  de: {
    'Journal des connexions': 'Anmeldeprotokoll',
    'Les utilisateurs se connectent avec leur compte Microsoft. Leur rôle dépend du groupe Entra ID auquel ils appartiennent.':
      'Die Benutzer melden sich mit ihrem Microsoft-Konto an. Ihre Rolle hängt von der Entra-ID-Gruppe ab, der sie angehören.',
    'Règles d’accès': 'Zugriffsregeln',
    'Groupe Super administrateur : droits de l’administrateur, plus la déclaration des configurateurs du Middleware.':
      'Gruppe Superadministrator: Rechte des Administrators, zusätzlich das Anlegen der Konfiguratoren der Middleware.',
    'Groupe Administrateur : tous les onglets, catalogue, droits clients, messages et journal des connexions.':
      'Gruppe Administrator: alle Registerkarten, Katalog, Kundenrechte, Nachrichten und Anmeldeprotokoll.',
    'Groupe Utilisateur : Création commande/devis et Suivi des commandes.':
      'Gruppe Benutzer: „Auftrag/Angebot erstellen“ und „Auftragsverfolgung“.',
    'Aucun de ces groupes': 'Keine dieser Gruppen',
    'Accès refusé ; la tentative est tracée dans le journal.': 'Zugriff verweigert; der Versuch wird im Protokoll erfasst.',
    'Nom ou compte...': 'Name oder Konto...',
    'Tous les résultats': 'Alle Ergebnisse',
    Du: 'Von',
    Au: 'Bis',
    Date: 'Datum',
    Nom: 'Name',
    Compte: 'Konto',
    Rôle: 'Rolle',
    Résultat: 'Ergebnis',
    'Adresse IP': 'IP-Adresse',
    'Connexion réussie': 'Anmeldung erfolgreich',
    'Refusée : aucun groupe du portail': 'Verweigert: keine Portalgruppe',
    'Échec de l’authentification': 'Authentifizierung fehlgeschlagen',
    'Aucune connexion ne correspond aux filtres.': 'Keine Anmeldung entspricht den Filtern.',
    'Seules les 500 connexions les plus récentes sont affichées : affinez les filtres.':
      'Nur die 500 neuesten Anmeldungen werden angezeigt: Filter verfeinern.',
    'Se déconnecter': 'Abmelden',
    Déconnexion: 'Abmelden',
    'Accès refusé': 'Zugriff verweigert',
    'Connexion impossible': 'Anmeldung nicht möglich',
    'Votre compte Microsoft n’appartient à aucun groupe autorisé pour CF Saisie commandes. Demandez l’accès à votre administrateur.':
      'Ihr Microsoft-Konto gehört keiner für CF Auftragserfassung berechtigten Gruppe an. Bitten Sie Ihren Administrator um Zugriff.',
    'La connexion Microsoft a échoué. Réessayez ; si le problème persiste, contactez votre administrateur.':
      'Die Microsoft-Anmeldung ist fehlgeschlagen. Versuchen Sie es erneut; wenn das Problem weiterhin besteht, wenden Sie sich an Ihren Administrator.',
    'Se connecter avec un autre compte': 'Mit einem anderen Konto anmelden',
  },
  nl: {
    'Journal des connexions': 'Aanmeldingslogboek',
    'Les utilisateurs se connectent avec leur compte Microsoft. Leur rôle dépend du groupe Entra ID auquel ils appartiennent.':
      'Gebruikers melden zich aan met hun Microsoft-account. Hun rol hangt af van de Entra ID-groep waartoe ze behoren.',
    'Règles d’accès': 'Toegangsregels',
    'Groupe Super administrateur : droits de l’administrateur, plus la déclaration des configurateurs du Middleware.':
      'Groep Superbeheerder: rechten van de beheerder, plus het vastleggen van de configuratoren van de Middleware.',
    'Groupe Administrateur : tous les onglets, catalogue, droits clients, messages et journal des connexions.':
      'Groep Beheerder: alle tabbladen, catalogus, klantrechten, berichten en aanmeldingslogboek.',
    'Groupe Utilisateur : Création commande/devis et Suivi des commandes.': 'Groep Gebruiker: Order/offerte aanmaken en Orderopvolging.',
    'Aucun de ces groupes': 'Geen van deze groepen',
    'Accès refusé ; la tentative est tracée dans le journal.': 'Toegang geweigerd; de poging wordt in het logboek vastgelegd.',
    'Nom ou compte...': 'Naam of account...',
    'Tous les résultats': 'Alle resultaten',
    Du: 'Van',
    Au: 'Tot',
    Date: 'Datum',
    Nom: 'Naam',
    Compte: 'Account',
    Rôle: 'Rol',
    Résultat: 'Resultaat',
    'Adresse IP': 'IP-adres',
    'Connexion réussie': 'Aanmelding geslaagd',
    'Refusée : aucun groupe du portail': 'Geweigerd: geen portaalgroep',
    'Échec de l’authentification': 'Authenticatie mislukt',
    'Aucune connexion ne correspond aux filtres.': 'Geen aanmelding komt overeen met de filters.',
    'Seules les 500 connexions les plus récentes sont affichées : affinez les filtres.':
      'Alleen de 500 meest recente aanmeldingen worden getoond: verfijn de filters.',
    'Se déconnecter': 'Afmelden',
    Déconnexion: 'Afmelden',
    'Accès refusé': 'Toegang geweigerd',
    'Connexion impossible': 'Aanmelden niet mogelijk',
    'Votre compte Microsoft n’appartient à aucun groupe autorisé pour CF Saisie commandes. Demandez l’accès à votre administrateur.':
      'Uw Microsoft-account behoort tot geen enkele groep die toegang heeft tot CF Orderinvoer. Vraag uw beheerder om toegang.',
    'La connexion Microsoft a échoué. Réessayez ; si le problème persiste, contactez votre administrateur.':
      'De Microsoft-aanmelding is mislukt. Probeer het opnieuw; neem contact op met uw beheerder als het probleem aanhoudt.',
    'Se connecter avec un autre compte': 'Aanmelden met een ander account',
  },
};

export default textes;
