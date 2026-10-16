import type { Traductions } from '..';

const textes: Traductions = {
  en: {
    'Aide': 'Help',
    'CF Saisie commandes — saisie des commandes et devis, lancement des configurateurs CPQ.':
      'CF Saisie commandes — entry of orders and quotes, launch of the CPQ configurators.',
    'Parcours principal': 'Main workflow',
    'Tableau de bord → Création d’une commande → rechercher le client → renseigner la référence chantier, le ticket Salesforce et la livraison → choisir la famille disponible → choisir le produit autorisé. Le configurateur du produit s’ouvre dans une nouvelle fenêtre ; fermez-la après avoir enregistré la configuration, puis choisissez la date de fin de production et validez.':
      'Dashboard → Create an order → search for the customer → enter the site reference, the Salesforce ticket and the delivery details → choose an available family → choose an authorised product. The product configurator opens in a new window; close it after saving the configuration, then choose the end-of-production date and confirm.',
    'Rôles': 'Roles',
    'Chaque personne se connecte avec son compte Microsoft ; son rôle dépend de son groupe Entra ID. Administrateur voit tous les onglets, dont Gestion clients & produits et le Journal des connexions. Utilisateur voit Création commande/devis et le Suivi des commandes. Super administrateur déclare en plus les configurateurs du Middleware (onglet Configurateurs), auxquels les administrateurs rattachent ensuite les produits.':
      'Everyone signs in with their Microsoft account; their role depends on their Entra ID group. Administrator sees all tabs, including Customer & product management and the Sign-in log. User sees Order/quote creation and Order tracking. Super administrator can also declare the Middleware configurators (Configurators tab), to which administrators then link the products.',
    'Mode Production / Test': 'Production / Test mode',
    'Reprendre une commande ou un devis': 'Resuming an order or a quote',
    'Suivi des commandes → Ouvrir (dossier en cours) pour reprendre la saisie et la configuration, Consulter pour voir la configuration d’un dossier transmis, Transformer en commande pour un devis envoyé.':
      'Order tracking → Open (file in progress) to resume data entry and configuration, View to see the configuration of a submitted file, Convert to order for a sent quote.',
    'Le mode choisi dans la barre du haut détermine le Middleware CPQ utilisé pour les nouveaux lancements (recette en mode Test, production en mode Production). Un dossier repris depuis le suivi s’ouvre toujours dans l’environnement où il a été créé.':
      'The mode chosen in the top bar determines the CPQ Middleware used for new launches (acceptance in Test mode, production in Production mode). A file resumed from order tracking always opens in the environment where it was created.',
  },
  de: {
    'Aide': 'Hilfe',
    'CF Saisie commandes — saisie des commandes et devis, lancement des configurateurs CPQ.':
      'CF Saisie commandes — Erfassung von Aufträgen und Angeboten, Start der CPQ-Konfiguratoren.',
    'Parcours principal': 'Hauptablauf',
    'Tableau de bord → Création d’une commande → rechercher le client → renseigner la référence chantier, le ticket Salesforce et la livraison → choisir la famille disponible → choisir le produit autorisé. Le configurateur du produit s’ouvre dans une nouvelle fenêtre ; fermez-la après avoir enregistré la configuration, puis choisissez la date de fin de production et validez.':
      'Dashboard → Auftrag erstellen → Kunden suchen → Baustellenreferenz, Salesforce-Ticket und Lieferung angeben → verfügbare Familie wählen → freigegebenes Produkt wählen. Der Produktkonfigurator öffnet sich in einem neuen Fenster; schließen Sie es, nachdem Sie die Konfiguration gespeichert haben, wählen Sie dann das Produktionsende-Datum und bestätigen Sie.',
    'Rôles': 'Rollen',
    'Chaque personne se connecte avec son compte Microsoft ; son rôle dépend de son groupe Entra ID. Administrateur voit tous les onglets, dont Gestion clients & produits et le Journal des connexions. Utilisateur voit Création commande/devis et le Suivi des commandes. Super administrateur déclare en plus les configurateurs du Middleware (onglet Configurateurs), auxquels les administrateurs rattachent ensuite les produits.':
      'Jede Person meldet sich mit ihrem Microsoft-Konto an; ihre Rolle hängt von ihrer Entra-ID-Gruppe ab. Administrator sieht alle Registerkarten, einschließlich Kunden- & Produktverwaltung und Anmeldeprotokoll. Benutzer sieht „Auftrag/Angebot erstellen“ und „Auftragsverfolgung“. Superadministrator legt zusätzlich die Konfiguratoren der Middleware an (Registerkarte Konfiguratoren), denen die Administratoren anschließend die Produkte zuordnen.',
    'Mode Production / Test': 'Modus Produktion / Test',
    'Reprendre une commande ou un devis': 'Einen Auftrag oder ein Angebot fortsetzen',
    'Suivi des commandes → Ouvrir (dossier en cours) pour reprendre la saisie et la configuration, Consulter pour voir la configuration d’un dossier transmis, Transformer en commande pour un devis envoyé.':
      'Auftragsverfolgung → Öffnen (laufender Vorgang), um Erfassung und Konfiguration fortzusetzen, Ansehen, um die Konfiguration eines übermittelten Vorgangs anzuzeigen, In Auftrag umwandeln für ein gesendetes Angebot.',
    'Le mode choisi dans la barre du haut détermine le Middleware CPQ utilisé pour les nouveaux lancements (recette en mode Test, production en mode Production). Un dossier repris depuis le suivi s’ouvre toujours dans l’environnement où il a été créé.':
      'Der in der oberen Leiste gewählte Modus bestimmt die CPQ-Middleware für neue Starts (Abnahme im Testmodus, Produktion im Produktionsmodus). Ein aus der Auftragsverfolgung fortgesetzter Vorgang öffnet sich immer in der Umgebung, in der er erstellt wurde.',
  },
  nl: {
    'Aide': 'Help',
    'CF Saisie commandes — saisie des commandes et devis, lancement des configurateurs CPQ.':
      'CF Saisie commandes — invoer van orders en offertes, starten van de CPQ-configurators.',
    'Parcours principal': 'Hoofdproces',
    'Tableau de bord → Création d’une commande → rechercher le client → renseigner la référence chantier, le ticket Salesforce et la livraison → choisir la famille disponible → choisir le produit autorisé. Le configurateur du produit s’ouvre dans une nouvelle fenêtre ; fermez-la après avoir enregistré la configuration, puis choisissez la date de fin de production et validez.':
      'Dashboard → Een order aanmaken → de klant zoeken → de werfreferentie, het Salesforce-ticket en de levering invullen → een beschikbare familie kiezen → een toegestaan product kiezen. De productconfigurator wordt in een nieuw venster geopend; sluit het nadat u de configuratie hebt opgeslagen, kies vervolgens de einddatum van de productie en bevestig.',
    'Rôles': 'Rollen',
    'Chaque personne se connecte avec son compte Microsoft ; son rôle dépend de son groupe Entra ID. Administrateur voit tous les onglets, dont Gestion clients & produits et le Journal des connexions. Utilisateur voit Création commande/devis et le Suivi des commandes. Super administrateur déclare en plus les configurateurs du Middleware (onglet Configurateurs), auxquels les administrateurs rattachent ensuite les produits.':
      'Iedereen meldt zich aan met zijn Microsoft-account; de rol hangt af van de Entra ID-groep. Beheerder ziet alle tabbladen, waaronder Klant- & productbeheer en het Aanmeldingslogboek. Gebruiker ziet Order/offerte aanmaken en Orderopvolging. Superbeheerder legt daarnaast de configuratoren van de Middleware vast (tabblad Configuratoren), waaraan de beheerders vervolgens de producten koppelen.',
    'Mode Production / Test': 'Modus Productie / Test',
    'Reprendre une commande ou un devis': 'Een order of offerte hervatten',
    'Suivi des commandes → Ouvrir (dossier en cours) pour reprendre la saisie et la configuration, Consulter pour voir la configuration d’un dossier transmis, Transformer en commande pour un devis envoyé.':
      'Orderopvolging → Openen (dossier in behandeling) om de invoer en configuratie te hervatten, Bekijken om de configuratie van een verzonden dossier te zien, Omzetten in order voor een verzonden offerte.',
    'Le mode choisi dans la barre du haut détermine le Middleware CPQ utilisé pour les nouveaux lancements (recette en mode Test, production en mode Production). Un dossier repris depuis le suivi s’ouvre toujours dans l’environnement où il a été créé.':
      'De modus die in de bovenste balk is gekozen, bepaalt de CPQ-Middleware voor nieuwe starts (acceptatie in testmodus, productie in productiemodus). Een dossier dat vanuit de orderopvolging wordt hervat, opent altijd in de omgeving waarin het is aangemaakt.',
  },
};

export default textes;
